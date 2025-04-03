// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "SBCameraScroll/DeepProcessing" //Unlit Transparent Vertex Colored Additive 
{
Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha 
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}

		SubShader   
		{
				Pass 
			{
				//SetTexture [_MainTex] 
				//{
				//	Combine texture * primary
				//}
				
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "_ShaderFix.cginc"

//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

//float4 _Color;
sampler2D _MainTex;
sampler2D _LevelTex;
sampler2D _NoiseTex;
sampler2D _PalTex;
//uniform float _fogAmount;
//uniform float _waterPosition;

#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif

uniform float _RAIN;

uniform float4 _spriteRect;
uniform float2 _screenSize;


struct v2f {
    float4  pos : SV_POSITION;
   float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;
    return o;
}



half4 frag (v2f i) : SV_Target
{

float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

textCoord.x -= _spriteRect.x;
textCoord.y -= _spriteRect.y;

textCoord.x /= _spriteRect.z - _spriteRect.x;
textCoord.y /= _spriteRect.w - _spriteRect.y;

float4 grabTexCol;

half dist = 1.0-clamp(distance(i.uv.xy, half2(0.5, 0.5))*2, 0, 1);
//dist = pow(pow((1-pow(dist , 2)), 3.5), 1);
 
half4 texcol = tex2D(_LevelTex, textCoord);
half dp = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) dp = 1;


if(dp < i.clr.x) return half4(0,0,0,0);
else if(dp > i.clr.y) return half4(0,0,0,0);
else if(dp > 6.0/30.0){
  grabTexCol = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
  if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) return half4(0,0,0,0);
}

half2 dsplace = (half2(i.scrPos.x, 1.0-i.scrPos.y) - half2(0.5, 0.5)) * 0.06 * dp;

// vanilla:
// half gridSize = lerp(10, 6, dp);

// modded:
// Is this shader used in non-square rooms?
//
// The variable sizeModifier is resolution dependent. It is the number of
// screen widths that fit into the merged level texture. But honestly it looks
// better if it is not too small. There are two cases,
//     1) you can zoom out by increasing the resolution and
//     2) you can fit the content on a larger monitor by increasing the
//     resolution and countering the zoom by changing the camera_zoom as well.
// In case 2, I expect this to be too large. But in case 1, you get the same
// size as before, i.e. the grid gets larger but you also zoom out. I leave it
// as is. The resolution is intended to be used to zoom out and not to support
// larger monitors.
float sizeModifier = _spriteRect.z - _spriteRect.x;
half gridSize = lerp(10, 6, dp) / sizeModifier;


half2 sampleCoord = textCoord;
sampleCoord *= half2(_screenSize.x, _screenSize.y);
sampleCoord /= gridSize;

sampleCoord.x = floor(sampleCoord.x)/_screenSize.x;
sampleCoord.y = floor(sampleCoord.y)/_screenSize.y;
sampleCoord *= gridSize;

sampleCoord += dsplace;

// vanilla:
// float rand = frac(sin(dot(sampleCoord.x, 12.98232)*sampleCoord.y*0.23532+_RAIN-tex2D(_NoiseTex, half2(lerp(sampleCoord.y, lerp(0.265, 0.9455, sampleCoord.x), 0.5), _RAIN)).x) * 43758.5453);
// half h2 = (sin((dp * 1.2 + 0.75 * _RAIN * lerp(0.5, 1.5, i.clr.w) + tex2D(_NoiseTex, float2(sampleCoord.x*1.2, sampleCoord.y*0.6) ).x * lerp(1, 1, pow(dist, 0.5))) * 3.14 * 2)*0.5)+0.5;
// half h = (sin((dp * lerp(0.12, 2.2, h2) + 1.8 * _RAIN * lerp(0.5, 1.5, i.clr.w) + tex2D(_NoiseTex, float2(sampleCoord.x*8.2, sampleCoord.y*4.2) ).x * lerp(1,4,pow(h2,3))) * 3.14 * 2)*0.5)+0.5;

// modded:
// Increase the density for the randomness. It smears the outlines for the grid
// blocks too much otherwise.
float rand = frac(sin(dot(sampleCoord.x, 12.98232)*sampleCoord.y*0.23532+_RAIN-tex2D(_NoiseTex, half2(lerp(sampleCoord.y * sizeModifier, lerp(0.265, 0.9455, sampleCoord.x * sizeModifier), 0.5), _RAIN)).x) * 43758.5453);
half h2 = (sin((dp * 1.2 + 0.75 * _RAIN * lerp(0.5, 1.5, i.clr.w) + tex2D(_NoiseTex, float2(sampleCoord.x * sizeModifier *1.2, sampleCoord.y * sizeModifier *0.6) ).x * lerp(1, 1, pow(dist, 0.5))) * 3.14 * 2)*0.5)+0.5;
half h = (sin((dp * lerp(0.12, 2.2, h2) + 1.8 * _RAIN * lerp(0.5, 1.5, i.clr.w) + tex2D(_NoiseTex, float2(sampleCoord.x * sizeModifier *8.2, sampleCoord.y * sizeModifier *4.2) ).x * lerp(1,4,pow(h2,3))) * 3.14 * 2)*0.5)+0.5;

if(lerp(h, 0, 1.0-i.clr.z) < rand * (1.0-i.clr.z)) return half4(0,0,0,0); 

h *= 1.0-distance(textCoord + dsplace, sampleCoord + half2(gridSize*0.5/_screenSize.x, gridSize*0.5/_screenSize.y)) * (_screenSize.x + _screenSize.y) / (gridSize*2);

//return half4(h, h, h, 1);

dist = pow(dist, lerp(1.2, lerp(0.199, 0.001, i.clr.w), h2));

h += lerp(-1, 1, i.clr.w)*0.2;

if(h*dist < 0.33) return half4(0,0,0,0);
else if (h*lerp(dist,1,0.5) < 0.66) return half4(0,0,0.5,0.2);

return half4(0,0,1,0.4);//tex2D(_PalTex, half2(31.5/32.0, 4.5/8.0));

// return half4(dist, dist, dist, 1);

}
ENDCG
				
				
				
			}
		} 
	}
}
