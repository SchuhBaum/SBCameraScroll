// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "SBCameraScroll/LevelColor" //Unlit Transparent Vertex Colored Additive 
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		
        // modded:
        // Initialize value.
        _TextureOffsetArraySize("DO NOT CHANGE", Range(0, 0)) = 0

	//	_PalTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		
	//	_NoiseTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		
		//    _RAIN ("Rain", Range (0,1.0)) = 0.5
		//_Color ("Main Color", Color) = (1,0,0,1.5)
		//_BlurAmount ("Blur Amount", Range(0,02)) = 0.0005
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
		GrabPass { }
		GrabPass {"_PreLevelColorGrab"}
		
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
#pragma multi_compile _ RIPPLE 
#pragma multi_compile _ COMBINEDLEVEL
#pragma multi_compile_local _ levelheat 
#pragma multi_compile_local _ levelmelt
// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
#include "_UrbanLife.cginc"
#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

//float4 _Color;
sampler2D _MainTex;

// vanilla:
// uniform float2 _MainTex_TexelSize;

// modded:
// in theory they are the same since this shader is only applied to the level
// texture; but they are not for some reason;
uniform float2 _LevelTex_TexelSize;

sampler2D _PalTex;
sampler2D _NoiseTex;
#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif
sampler2D _LevelTex;
sampler2D _RippleMask;
sampler2D _GameplayRippleMask;
sampler2D _RipplePalTex;
sampler2D _GameplayRipplePalTex;
float _RipplePaletteAmount;
float _RippleTrailPaletteAmount;
uniform float _rippleFogAmount;

//float _BlurAmount;

uniform float _palette;
uniform float _RAIN;
uniform float _light = 0;

// modded:
uniform float2 _textureOffsetArray[30];
uniform int    _textureOffsetArrayLength;

uniform float4 _spriteRect;
uniform float2 _screenOffset;

uniform float4 _lightDirAndPixelSize;
uniform float _fogAmount;
uniform float _waterLevel;
uniform float _Grime;
uniform float _SwarmRoom;
uniform float _WetTerrain;
uniform float _cloudsSpeed;
uniform float _darkness;
uniform float _contrast;
uniform float _saturation;
uniform float _hue;
uniform float _brightness;
uniform half4 _AboveCloudsAtmosphereColor;
uniform fixed _rimFix;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2  uv2 : TEXCOORD1;
    float2  scrPos : TEXCOORD2;
    float4 clr : COLOR;

    // modded:
    float2  pixel_coord : TEXCOORD3;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);

    // modded:
    o.pixel_coord = o.uv / _LevelTex_TexelSize;

    // problem:
    // when using larger level textures and _rimFix is used the texture is shifted to the right
    // and misaligns with other shaders (cloud and background shader for example); see the room
    // OE_Rail01 as an example;
    // 
    // The weird part is that this bug is not consistent; when I quit to the main menu from the
    // room OE_Rail01 then it will not happen after loading again; it happens again if I quit
    // from a room that is not affected;
    // 
    // Unity jank??;

    // vanilla:
    // does not work with larger level textures when _rimFix is used (1f); _MainTex_TexelSize stays
    // constant at float2(1400, 800) (see pixel shader);
    // o.uv2 = o.uv-_MainTex_TexelSize*.5*_rimFix;

    // this works for some reason; WHY??; how does this yield different
    // results??;
    // o.uv2 = o.uv - float2(1/1400, 1/800) * .5 * _rimFix;

    // modded:
    // works as well; this shader is only applied to the level texture so this should in theory be
    // equivalent to vanilla; of course it is not (see pixel shader);
    o.uv2 = o.uv - _LevelTex_TexelSize * .5 * _rimFix;

    o.scrPos = ComputeScreenPos(o.pos).xy;
    o.clr = v.color;
    return o;
}

// modded:
inline float2 getScreenTextureOffset(float2 pixel_coord) {
    if (_textureOffsetArrayLength <= 0) return float2(0,0);
    if (_textureOffsetArrayLength > 30) return float2(0,0);
    float  smallest_distance      = 1e30;
    float2 closest_texture_offset = float2(0,0);

    for (int index = 0; index < _textureOffsetArrayLength; ++index) {
        float2 texture_offset = _textureOffsetArray[index];
        if (pixel_coord.x < texture_offset.x || pixel_coord.y < texture_offset.y) continue;
        if (distance(pixel_coord, texture_offset) >= smallest_distance) continue;

        smallest_distance = distance(pixel_coord, texture_offset);
        closest_texture_offset = texture_offset;
    }
    return closest_texture_offset;
}

inline float3 applyHue(float3 aColor, float aHue)
{
	float angle = radians(aHue);
	float3 k = float3(0.57735, 0.57735, 0.57735);
	float cosAngle = cos(angle);
	//Rodrigues' rotation formula
	return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
}

half4 frag (v2f i) : SV_Target
{
#if RIPPLE
fixed4 rippleMask = tex2D(_RippleMask, i.scrPos);//x = normal ripple mask; y = watcher trail
fixed gameplayRippleMask = tex2D(_GameplayRippleMask, i.scrPos).x;
gameplayRippleMask = lerp(gameplayRippleMask.x, rippleMask.y*.5, rippleMask.y);
fixed shiftWave = smoothstep(.2,-.0, abs(rippleMask.x-.2))*-.02;
fixed gameplayShiftWave = smoothstep(.2,-.0, abs(gameplayRippleMask-.2))*-.02;
shiftWave = min(lerp(shiftWave, 0, gameplayRippleMask>.2), gameplayShiftWave); //combine cosmetic ripple color rim with gameplay ripple
shiftWave = min(shiftWave, -smoothstep(.25, -.0, abs(rippleMask.y-.8))*.02); //add color rim to the trail
fixed4 rippleColor = 0;
fixed4 gameplayRippleColor = 0;
float gameplayPaletteAmount = lerp(gameplayRippleMask, _RippleTrailPaletteAmount, saturate(saturate(rippleMask.y-.5)*2));
#endif // RIPPLE

half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
bool checkMaskOut = false;

sampler2D levelTexture = _MainTex;
#if COMBINEDLEVEL
          levelTexture = _LevelTex;
#endif //COMBINEDLEVEL

float ugh = fmod(fmod(   round(tex2D(levelTexture, float2(i.uv.x, i.uv.y)).x*255)   , 90)-1, 30)/300.0;

float displace = tex2D(_NoiseTex, float2((i.uv.x * 1.5)  - ugh + _RAIN * 0.01,
                                         (i.uv.y * 0.25) - ugh + _RAIN * 0.05)   ).x;
displace = saturate((sin((displace + i.uv.x + i.uv.y + _RAIN*0.1) * 3 * 3.14)-0.95)*20);

half2 screenPos = half2(lerp(_spriteRect.x+_screenOffset.x, _spriteRect.z+_screenOffset.x, i.uv.x),
                        lerp(_spriteRect.y+_screenOffset.y, _spriteRect.w+_screenOffset.y, i.uv.y));
#if UNITY_UV_STARTS_AT_TOP
  screenPos.y = 1 - screenPos.y;
#endif

if (_WetTerrain < 0.5 || screenPos.y > _waterLevel) 
	displace = 0;
#if levelmelt
    displace = 0;
#endif
half4 texcol = tex2D(levelTexture, float2(i.uv2.x, i.uv2.y+displace*0.001));
   
int red = round(texcol.x * 255);
int green = round(texcol.y * 255);

float effectCol = 0;

float heatEffectCol = 0;
float sky = false;
bool melt = false;

#if levelmelt
melt = true;
#endif //levelmelt
   
if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0){ //sky stuff
	setColor = tex2D(_PalTex, float2(0.5/32.0, 7.5/8));
#if levelmelt
    red = 31;
#endif //levelmelt
#if RIPPLE
	rippleColor = tex2D(_RipplePalTex, float2(0.5/32.0, 7.5/8));
	gameplayRippleColor = tex2D(_GameplayRipplePalTex, float2(0.5/32.0, 7.5/8));
	setColor = lerp(setColor, rippleColor, smoothstep(.1, .4,rippleMask.x)*_RipplePaletteAmount);
	setColor = lerp(setColor, gameplayRippleColor, smoothstep(.1, .4, gameplayPaletteAmount));
	if(_rimFix>.5 && gameplayRippleMask<.2){
		setColor = _AboveCloudsAtmosphereColor;
	}
#else
	if(_rimFix>.5){
		setColor = _AboveCloudsAtmosphereColor;
	}
#endif //RIPPLE
	checkMaskOut = true;
    sky = true;
} 
else 
{ 
	half notFloorDark = 1;
	if(green >= 16) {
		notFloorDark = 0;
		green -= 16;
	}
	if(green >= 8) {
		effectCol = 100;
		green -= 8;
	} else
		effectCol = green;

    // vanilla:
	// half shadow = tex2D(_NoiseTex, float2((i.uv.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0)),
                                        // 1-(i.uv.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0))
                                        // )).x;
	// shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-i.uv.y, 1)*3.14*2)*0.5;

    // modded:
    // The clouds light mask can be stretched. The mask is
    // applied to the full level texture. Merged level
    // textures contain multiple screens.
    float number_of_screens_x = _spriteRect.z - _spriteRect.x;
    float number_of_screens_y = _spriteRect.w - _spriteRect.y;
    half shadow = tex2D(_NoiseTex, float2(((number_of_screens_x*i.uv.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0))), (1-(number_of_screens_y*i.uv.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0))))).x;
    shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-number_of_screens_y*i.uv.y, 1)*3.14*2)*0.5;

    // vanilla:
	shadow = saturate(((shadow - 0.5)*6)+0.5-(_light*4));

	if (red > 90)
		red -= 90;
	else
		shadow = 1.0;
   
	int paletteColor = clamp(floor((red-1)/30.0), 0, 2); //some distant objects want to get palette color 3, so we clamp it
   
	red = fmod(red-1, 30.0);//depth

#if URBANLIFE
        shadow = max(UrbanLifeShadows(i.scrPos,red),shadow);
#endif
  
	if (shadow != 1 && red >= 5) {//casting shadows from objects
        float4 shadowFix =FixEdgeShadowStretch(screenPos, false);
		half2 grabPos = float2(screenPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(red-5)*shadowFix.x,
                             1-screenPos.y +  _lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(red-5)*shadowFix.y);
		grabPos = lerp(grabPos,((grabPos-half2(0.5, 0.3))*(1 + (red-5.0)/460.0))+half2(0.5, 0.3),shadowFix.zw);
		float4 grabTexCol2 = tex2D(_GrabTexture, grabPos);
 		if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0) {
     			shadow = 1;
  		}
	}

#if levelheat
    if (red >= 5) {
        float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1 - screenPos.y));
        if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
            red = 5;
        }
    }

fixed reverse =1;
#if levelmelt
    reverse = -1;
    sky = false;
#endif //levelmelt

    heatEffectCol = tex2D(_NoiseTex, half2(i.uv.x, i.uv.y*0.5 - _RAIN*0.123*reverse));
    heatEffectCol = 0.5 + 0.5 * sin(heatEffectCol*3.14 * 8 + i.uv.y*36.231 - _RAIN*2.63*reverse + (red / 30.0)*8.12);
    heatEffectCol *= 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2((1.0 - i.uv.x) * 2, i.uv.y*0.5 - _RAIN*0.1862*reverse))*3.14 * 8 
                                           + i.uv.y*50.231 - _RAIN*4.75442*reverse + (red / 30.0)*8.12);
    heatEffectCol = 1 - heatEffectCol;
    heatEffectCol = pow(heatEffectCol, 30);

    half4 heatTexCol = tex2D(levelTexture, float2(i.uv.x, i.uv.y + lerp(-0.01, 0.02, heatEffectCol)*i.clr.w));
#if RIPPLE
    heatTexCol= lerp(heatTexCol,texcol,step(.2,gameplayRippleMask));
#endif // ripple
    texcol = heatTexCol;

#if levelmelt
    screenPos.y -=lerp(-0.01, 0.02, heatEffectCol)*i.clr.w;
#endif //levelmelt

    red = round(texcol.x * 255);
    red = fmod(red - 1, 30.0);

    heatEffectCol = 1 - abs(heatEffectCol - 0.5) * 2;
    heatEffectCol = pow(max(0,heatEffectCol - (1.0 - i.clr.w)), lerp(10.0, 1.0, i.clr.w));
#endif // levelheat

    half4 fogCol = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));
	half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(i.uv.x*2, i.uv.y*2) ).x * 4) + red/12.0) * 3.14 * 2)*0.5)+0.5;
#if levelmelt
    setColor =  tex2D(_PalTex, float2((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0));
    if (green > 0 && green < 3)
        heatEffectCol = max(heatEffectCol, texcol.z);
	if (effectCol == 100) { //colored props
        // vanilla:
		// half4 decalCol = tex2D(_MainTex, float2((255.5-round(texcol.z*255.0))/1400.0, 799.5/800.0));

        // modded:
        // There are some pixels on every screen texture that
        // contain color information. Here, the offset to these
        // pixels is calculated and used.
        float2 texture_offset = getScreenTextureOffset(i.pixel_coord);
        half4 decalCol = tex2D(_MainTex, float2((texture_offset.x + (255.5-round(texcol.z*255.0))) * _LevelTex_TexelSize.x, (texture_offset.y + 799.5) * _LevelTex_TexelSize.y));

		if(paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
		decalCol = lerp(decalCol, fogCol, red/60.0);
		setColor = lerp(lerp(setColor, decalCol, 0.7), setColor*decalCol*1.5,  lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
	}
     setColor = lerp(setColor, fogCol, saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_fogAmount/30.0));
#else //levelmelt

	setColor = lerp(tex2D(_PalTex, float2((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0)),
                    tex2D(_PalTex, float2((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0)),
                    shadow);

	setColor = lerp(setColor, tex2D(_PalTex, float2((5.5 + rbcol*25)/32.0, 6.5 / 8.0) ), (green >= 4 ? 0.2 : 0.0) * _Grime);
   
	if (effectCol == 100) { //colored props
        // vanilla:
        // half4 decalCol = tex2D(_MainTex, float2((255.5-round(texcol.z*255.0))/1400.0, 799.5/800.0));

        // modded:
        float2 texture_offset = getScreenTextureOffset(i.pixel_coord);
        half4 decalCol = tex2D(_MainTex, float2((texture_offset.x + (255.5-round(texcol.z*255.0))) * _LevelTex_TexelSize.x, (texture_offset.y + 799.5) * _LevelTex_TexelSize.y));

		if(paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
		decalCol = lerp(decalCol, fogCol, red/60.0);
		setColor = lerp(lerp(setColor, decalCol, 0.7), setColor*decalCol*1.5,  lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
	}
	else if (green > 0 && green < 3) {//effect colors
		setColor = lerp(setColor,
                        lerp(lerp(tex2D(_PalTex, float2(30.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                                  tex2D(_PalTex, float2(31.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                                  shadow),
                             lerp(tex2D(_PalTex, float2(30.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                                  tex2D(_PalTex, float2(31.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                                  shadow),
                             red/30.0),
                        texcol.z);

	} else if (green == 3) {//batfly nests?
		setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
	}
   
	setColor = lerp(setColor, fogCol, saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_fogAmount/30.0));
#endif // levelmelt

#if RIPPLE // copypasta of the original palette code with minor adjustments
    half4 rippleFogCol =tex2D(_RipplePalTex, float2(1.5/32.0, 7.5/8.0));
    half4 gameplayRippleFogCol = tex2D(_GameplayRipplePalTex, float2(1.5/32.0, 7.5/8.0));

    rippleColor = lerp(tex2D(_RipplePalTex, float2((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0)),
                       tex2D(_RipplePalTex, float2((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0)),
                       shadow);
	rippleColor = lerp(rippleColor, 
                       tex2D(_RipplePalTex, float2((5.5 + rbcol*25)/32.0, 6.5 / 8.0) ),
                       (green >= 4 ? 0.2 : 0.0) * _Grime);
    gameplayRippleColor =
          lerp(tex2D(_GameplayRipplePalTex, float2((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0)),
               tex2D(_GameplayRipplePalTex, float2((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0)),
               shadow);
	gameplayRippleColor = lerp(gameplayRippleColor,
                               tex2D(_GameplayRipplePalTex, float2((5.5 + rbcol*25)/32.0, 6.5 / 8.0) ),
                               (green >= 4 ? 0.2 : 0.0) * _Grime);

	if (effectCol == 100)
    {
        // vanilla:
        // half4 decalCol = tex2D(_MainTex, float2((255.5-round(texcol.z*255.0))/1400.0, 799.5/800.0));

        // modded:
        float2 texture_offset = getScreenTextureOffset(i.pixel_coord);
        half4 decalCol = tex2D(_MainTex, float2((texture_offset.x + (255.5-round(texcol.z*255.0))) * _LevelTex_TexelSize.x, (texture_offset.y + 799.5) * _LevelTex_TexelSize.y));

		if(paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
		half4 gameplayDecalCol = lerp(decalCol, gameplayRippleFogCol, red/60.0);
		decalCol = lerp(decalCol, rippleFogCol, red/60.0);
		rippleColor = lerp(lerp(rippleColor, decalCol, 0.7),
                           rippleColor*decalCol*1.5,
                           lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
		gameplayRippleColor = lerp(lerp(gameplayRippleColor, gameplayDecalCol, 0.7),
                                   gameplayRippleColor*gameplayDecalCol*1.5,
                                   lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
	}
	else if (green > 0 && green < 3) {
		rippleColor = 
        lerp(rippleColor, 
             lerp(lerp(tex2D(_RipplePalTex, float2(30.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                       tex2D(_RipplePalTex, float2(31.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                       shadow),
                  lerp(tex2D(_RipplePalTex, float2(30.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                       tex2D(_RipplePalTex, float2(31.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                       shadow),
                  red/30.0),
             texcol.z);

		gameplayRippleColor =
        lerp(gameplayRippleColor,
             lerp(lerp(tex2D(_GameplayRipplePalTex, float2(30.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                       tex2D(_GameplayRipplePalTex, float2(31.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                       shadow),
                  lerp(tex2D(_GameplayRipplePalTex, float2(30.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                       tex2D(_GameplayRipplePalTex, float2(31.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                       shadow),
                  red/30.0),
             texcol.z);
	} else if (green == 3) {
		rippleColor = lerp(rippleColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
		gameplayRippleColor = lerp(gameplayRippleColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
	}
	
	rippleColor =
        lerp(rippleColor,
             rippleFogCol,
             saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_rippleFogAmount/30.0));
	gameplayRippleColor =
        lerp(gameplayRippleColor,
             gameplayRippleFogCol,
             saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*.033));

	setColor = lerp(setColor,rippleColor,smoothstep(.1,.4,rippleMask.x)*_RipplePaletteAmount);
	setColor = lerp(setColor,gameplayRippleColor,smoothstep(.1,.4,gameplayPaletteAmount));
#endif // RIPPLE

	if (red >= 5) {
		checkMaskOut = true; 	
	}
}
  
if (checkMaskOut) {
	float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1-screenPos.y));
 	if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0){
        setColor.w = 0;
#if levelmelt || levelheat
        setColor = grabTexCol;
        red = 5;
        heatEffectCol = 0;
#endif
   	}
}

#if levelheat
    if (sky && melt) {
        heatEffectCol = pow(1 - i.uv.y, 3) * 0.5 * i.clr.w;
    }
    if ((!sky && (green == 0 || green >= 3))||melt) {
			heatEffectCol = lerp(heatEffectCol, 1, (red / 30.0)*0.1*i.clr.w);

			if (heatEffectCol < 0.75)
				setColor =
                lerp(setColor,
                     lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)),
                          tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)),
                          red / 30.0),
                     heatEffectCol / 0.75);
			else
                setColor =
                lerp(lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)),
                          tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)),
                          red / 30.0),
                      half4(1, 1, 1, 1),
                      heatEffectCol - 0.75);
		}
#endif //levelheat

#if !levelmelt
// Color Adjustment params
setColor.rgb *= _darkness;
setColor.rgb = ((setColor.rgb - 0.5) * _contrast) + 0.5;
float greyscale = dot(setColor.rgb, float3(.222, .707, .071));  // Convert to greyscale numbers with magic luminance numbers
setColor.rgb = lerp(float3(greyscale, greyscale, greyscale), setColor.rgb, _saturation);
setColor.rgb = applyHue(setColor.rgb, _hue);
setColor.rgb += _brightness;
#endif //levelmelt

#if RIPPLE
setColor = fixed4(lerp(setColor.xyz, setColor.xyz*fixed3(.85,.89,1.4), shiftWave*-80),setColor.w);
#endif

return setColor;
}
ENDCG
				
			}
		} 
	}
}
