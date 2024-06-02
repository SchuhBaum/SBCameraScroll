// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'
// from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

// Unlit Transparent Vertex Colored Additive 
Shader "SBCameraScroll/UnderWaterLight" {
Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha 
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels {
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}

		SubShader {
		    //GrabPass { }
			Pass {
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
                uniform float _waterPosition;

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

                v2f vert (appdata_full v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos (v.vertex);
                    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                half4 frag (v2f i) : SV_Target {
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

                    textCoord.x -= _spriteRect.x;
                    textCoord.y -= _spriteRect.y;
                    
                    textCoord.x /= _spriteRect.z - _spriteRect.x;
                    textCoord.y /= _spriteRect.w - _spriteRect.y;

                    half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(textCoord.x*1.2, textCoord.y*1.2) ).x * 3) + 0/12.0) * 3.14 * 2)*0.5)+0.5;

                    // vanilla:
                    // float2 distortion = float2(lerp(-0.002, 0.002, rbcol)*lerp(1, 20, pow(i.uv.y, 200)), -0.02 * pow(i.uv.y, 8));

                    // modded:
                    // reduces the magnitude of the distortion effect; this can get out of hand 
                    // in larger rooms otherwise; copy&paste from DeepWater shader;
                    // I probably can just use _LevelTex_TexelSize instead of 1/level_texture_size.
                    float2 level_texture_size = float2((_spriteRect.z - _spriteRect.x) * _screenSize.x, (_spriteRect.w - _spriteRect.y) * _screenSize.y);
                    float2 distortion = float2(lerp(-0.002, 0.002, rbcol) * lerp(1, 20, pow(i.uv.y, 200)) * 1400 / level_texture_size.x, -0.02 * pow(i.uv.y, 8) * 800 / level_texture_size.y);

                    // vanilla:
                    // distortion.x = floor(distortion.x*_screenSize.x)/_screenSize.x;
                    // distortion.y = floor(distortion.y*_screenSize.y)/_screenSize.y;

                    // modded:
                    // makes the distortion less pixelated;
                    distortion.x = floor(distortion.x * level_texture_size.x) / level_texture_size.x;
                    distortion.y = floor(distortion.y * level_texture_size.y) / level_texture_size.y;

                    half4 texcol = tex2D(_LevelTex, textCoord+distortion);

                    //int paletteColor = floor(((int)(texcol.x * 255) % 90 )/30.0);
                    //if (texcol.y >= 16.0/255.0) paletteColor = 3;

                    half dist = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
                    if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) {
                        dist = 1.0;
                    }

                    if (dist > 6.0/30.0) {
                        half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
                        if ( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                            dist = 6.0/30.0;
                        }
                    }


                    half2 dir = normalize(i.uv.xy - half2(0.5, 0.5)); 
                    half centerDist = clamp(distance(i.uv.xy, half2(0.5, 0.5))*2, 0, 1);
                    textCoord += centerDist*dir*0.02;
                    half d = dist;

                    if (dist < 0.2) {
                        dist = pow(1.0-(dist * 5.0), 0.35);
                    } else {
                        dist = clamp((dist - 0.2) * 1.3, 0, 1);
                    }

                    dist = 1.0-dist;
                    dist *= pow(pow((1-pow(min(centerDist + d*0.5, 1), 2)), 3.5), 1);//lerp(0.5, 1.5, d));

                    half whatToSine = (_RAIN*6) + (tex2D(_NoiseTex, float2((d/10)+lerp(textCoord.x, 0.5, d/3)*2.1,  (_RAIN*0.1)+(d/5)+lerp(textCoord.y, 0.5, d/3)*2.1) ).x * 7);
                    half col = (sin(whatToSine * 3.14 * 2)*0.5)+0.5;
                    textCoord += dir*0.15*pow(1-centerDist, 1.7);
                    whatToSine = (_RAIN*2.7) + (tex2D(_NoiseTex, float2((d/7)+lerp(textCoord.x, 0.5, d/5)*1.3,  (_RAIN*-0.21)+(d/8)+lerp(textCoord.y, 0.5, d/6)*1.3) ).x * 6.33);
                    half col2 = (sin(whatToSine * 3.14 * 2)*0.5)+0.5;
                    
                    if (pow(max(col, col2), 47) >= 0.8) {
                        dist = pow(dist, 0.6+0.4*centerDist);
                    }

                    dist *= lerp(1, max(col, col2), pow(centerDist, 1.5));
                    return half4(i.clr.xyz, dist * i.clr.w * lerp(0.75, 0.5, centerDist));
                }
                ENDCG
			}
		} 
	}
}
