// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "Futile/GlyphProjection" //Unlit Transparent Vertex Colored Additive 
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
//sampler2D _PalTex;
//uniform float _fogAmount;
//uniform float _waterPosition;

//#if defined(SHADER_API_PSSL)
//sampler2D _GrabTexture;
//#else
//sampler2D _GrabTexture : register(s0);
//#endif

uniform float _RAIN;

uniform float4 _spriteRect;
uniform float2 _screenSize;
uniform float2 _gridOffset;

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
// vanilla:
// float2 texSize = float2(1400.0, 800.0);

// modded:
float number_of_screens_x = _spriteRect.z - _spriteRect.x;
float number_of_screens_y = _spriteRect.w - _spriteRect.y;
float2 texSize = float2(number_of_screens_x * _screenSize.x, number_of_screens_y * _screenSize.y);

float2 textCoord = float2(floor(i.scrPos.x*texSize.x)/texSize.x, floor(i.scrPos.y*texSize.y)/texSize.y);


textCoord -= half2(_gridOffset.x / _screenSize.x, _gridOffset.y / _screenSize.y);

textCoord.x -= _spriteRect.x;
textCoord.y -= _spriteRect.y;

textCoord.x /= _spriteRect.z - _spriteRect.x;
textCoord.y /= _spriteRect.w - _spriteRect.y;

float2 sampleCoord = textCoord * half2(texSize.x, texSize.y)/15.0;

sampleCoord.x = floor(sampleCoord.x)/texSize.x;
sampleCoord.y = floor(sampleCoord.y)/texSize.y;
float rand = frac(sin(dot(sampleCoord.x, 12.98232)*sampleCoord.y*0.23532-tex2D(_NoiseTex, half2( lerp(0.265, 0.9455, sampleCoord.y), 0.5)).x) * lerp(43758.5453, 23746.232, i.clr.y));

 
if(frac(sin(dot(sampleCoord.x, 7.62232)*sampleCoord.y*0.19532-tex2D(_NoiseTex, half2( lerp(0.235, 0.973, sampleCoord.y), 0.4)).x) * 102438.543) > i.clr.w)
return half4(0,0,0,0);


float strtX = frac((textCoord.x * texSize.x) / 15.0);
float strtY = frac(textCoord.y * texSize.y / 15.0);

float glyph = floor(rand * 50.0)/50.0;

if ((tex2D(_MainTex, float2(glyph+(strtX/50.0), strtY)).x < 0.5) == (i.clr.x == 0))
return half4(0,0,0,1);
else
return half4(0,0,0,0);//half4(0.5f+0.5*rand,clamp((rand-0.95)*10, 0, 1),0,1);

}
ENDCG
				
				
				
			}
		} 
	}
}
