// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// This edit adds a shader variant for correct rendering with snow.
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

//Unlit Transparent Vertex Colored Additive 
Shader "SBCameraScroll/Fog" {
Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
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
	        //	GrabPass { }
			Pass {
				//SetTexture [_MainTex] 
				//{
				//	Combine texture * primary
				//}
				
                CGPROGRAM
                #pragma target 4.0
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile SNOW_ON SNOW_OFF
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
                uniform float _waterLevel;

                #if defined(SHADER_API_PSSL)
                    sampler2D _GrabTexture;
                #else
                    sampler2D _GrabTexture : register(s0);
                #endif

                uniform float _RAIN;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                sampler2D _SnowTex;

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
                    #if SNOW_ON
                        // vanilla:
                        float2 textCoord = i.scrPos;
                        
                        // modded:
                        // not sure why you need make this in pixel size steps; this is done in some shaders but for some 
                        // reason not in fullscreen effect shaders => leave it as is;
                        // float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
                        
                        textCoord.x -= _spriteRect.x;
                        textCoord.y -= _spriteRect.y;
                        
                        // modded:
                        // I need the non-normalized version for the noise texture;
                        float2 textCoord2 = textCoord;
                        
                        textCoord.x /= _spriteRect.z - _spriteRect.x;
                        textCoord.y /= _spriteRect.w - _spriteRect.y;

                        // vanilla:
                        // half2 screenPos = half2(i.scrPos.x, 1-i.scrPos.y); // not used;
                        // half amount = clamp((i.scrPos.y - ((1-_waterLevel) - 0.11))*3, 0, 1);
                        
                        // modded:
                        // if I don't account for the size of the texture in y then the amount might
                        // be too low at lower levels;
                        // half amount = clamp((textCoord.y - ((1-_waterLevel) - 0.11))*3, 0, 1);
                        half amount = clamp((textCoord.y - ((1-_waterLevel) - 0.11) / (_spriteRect.w - _spriteRect.y))*3, 0, 1);

                        // vanilla:
                        // half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.44 - _RAIN*0.113, textCoord.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + i.uv.y)*6.28);
                        // half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.6 + _RAIN*0.08, textCoord.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + i.uv.x)*6.28);

                        // modded:
                        // I need to wrap the noise texture; otherwise the fog clouds are way bigger
                        // than intended;
                        // half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.44 - _RAIN*0.113, textCoord.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + textCoord.y)*6.28);
                        // half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.6 + _RAIN*0.08, textCoord.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + textCoord.x)*6.28);
                        half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*0.44 - _RAIN*0.113, textCoord2.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + textCoord2.y)*6.28);
                        half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*0.6 + _RAIN*0.08, textCoord2.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + textCoord2.x)*6.28);
                            
                        //  displace = pow(displace * displace2, 0.5);//lerp(displace, displace2, 0.5);
                        fog1 = lerp(fog1, fog2, 0.5);
                        half4 snowCol =tex2D(_SnowTex, textCoord); 
                        half snowmask = snowCol.y;
                        snowCol = half4(snowCol.x,0,1,1);   
                        half4 texcol = tex2D(_LevelTex, textCoord);
                        texcol = lerp(texcol,snowCol,snowmask);
                        half dp = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
                        
                        if (texcol.x == 1 && texcol.y == 1 && texcol.z == 1) {
                            dp = 1;
                        }

                        if (dp > 6.0/30.0) {
                            half4 grabTexCol = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
                            if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                                dp = 6.0/30.0;
                            }
                        }

                        if (dp == 1) {
                            // vanilla:
                            // fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(i.uv.x*1.7 + _RAIN*0.113, i.uv.y*2.82)).x + _RAIN*0.14 - i.uv.x)*6.28);
                            // fog2 *= clamp(1-distance(i.uv, half2(0,0.9)), 0, 1);
                            
                            // modded:
                            // fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*1.7 + _RAIN*0.113, textCoord.y*2.82)).x + _RAIN*0.14 - textCoord.x)*6.28);
                            fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*1.7 + _RAIN*0.113, textCoord2.y*2.82)).x + _RAIN*0.14 - textCoord2.x)*6.28);
                            fog2 *= clamp(1-distance(textCoord, half2(0,0.9)), 0, 1);
                            
                            fog2 = pow(fog2, 0.2);
                            fog2 *= amount;
                            fog2 *= 1 - pow(fog1, 1.5);
                            fog2 *= i.clr.w;
                            
                            if (fog2 > 0.5) {
                                return lerp(tex2D(_PalTex, float2(0, 7.0/8.0)), half4(1,1,1,1), fog2 > 0.6 ? 0.25 : 0.1);
                            }
                        }

                        fog1 = pow(fog1, 3);
                        fog1 *= i.clr.w;
                        //fog1 *= min(dp+0.1, 1);
                        fog1 = pow(fog1, 1 + (1-pow(dp, 0.1))*30);
                        fog1 = max(0, fog1 - (1-amount));
                        fog1 = pow(fog1, 0.2);

                        //return half4(fog1, fog1, 0, 1);

                        if (fog1 > 0.1) return half4(lerp(tex2D(_PalTex, float2(0, 2.0/8.0)), tex2D(_PalTex, float2(0, 7.0/8.0)), 0.5+0.5*dp).xyz, fog1 > 0.5 ? 0.6 : 0.2);
                        return half4(0,0,0,0);
                        //return lerp(tex2D(_GrabTexture, float2(screenPos.x, screenPos.y)), half4(displace,0,0,1), 1);//amount*0.75);
                    #elif SNOW_OFF
                        float2 textCoord = i.scrPos;
                        textCoord.x -= _spriteRect.x;
                        textCoord.y -= _spriteRect.y;

                        // modded:
                        float2 textCoord2 = textCoord;
                        
                        textCoord.x /= _spriteRect.z - _spriteRect.x;
                        textCoord.y /= _spriteRect.w - _spriteRect.y;

                        // vanilla:
                        // half2 screenPos = half2(i.scrPos.x, 1-i.scrPos.y); // not used
                        // half amount = clamp((i.scrPos.y - ((1-_waterLevel) - 0.11))*3, 0, 1);
                        
                        // modded:
                        // half amount = clamp((textCoord.y - ((1-_waterLevel) - 0.11))*3, 0, 1);
                        half amount = clamp((textCoord.y - ((1-_waterLevel) - 0.11) / (_spriteRect.w - _spriteRect.y))*3, 0, 1);

                        // vanilla:
                        // half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.44 - _RAIN*0.113, textCoord.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + i.uv.y)*6.28);
                        // half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.6 + _RAIN*0.08, textCoord.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + i.uv.x)*6.28);
                        
                        // modded:
                        // half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.44 - _RAIN*0.113, textCoord.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + textCoord.y)*6.28);
                        // half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*0.6 + _RAIN*0.08, textCoord.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + textCoord.x)*6.28);
                        half fog1 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*0.44 - _RAIN*0.113, textCoord2.y*1.2 - _RAIN*0.0032)).x + _RAIN*0.05 + textCoord2.y)*6.28);
                        half fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*0.6 + _RAIN*0.08, textCoord2.y*1 - _RAIN*0.01)).x + _RAIN*0.04 + textCoord2.x)*6.28);

                        //  displace = pow(displace * displace2, 0.5);//lerp(displace, displace2, 0.5);
                        fog1 = lerp(fog1, fog2, 0.5);
                        half4 texcol = tex2D(_LevelTex, textCoord);
                        half dp = fmod(round(texcol.x * 255)-1, 30.0)/30.0;

                        if (texcol.x == 1 && texcol.y == 1 && texcol.z == 1) {
                            dp = 1;
                        }

                        if (dp > 6.0/30.0) {
                            half4 grabTexCol = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
                            if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                                dp = 6.0/30.0;
                            }
                        }

                        if (dp == 1) {
                            // vanilla:
                            // fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(i.uv.x*1.7 + _RAIN*0.113, i.uv.y*2.82)).x + _RAIN*0.14 - i.uv.x)*6.28);
                            // fog2 *= clamp(1-distance(i.uv, half2(0,0.9)), 0, 1);
                            
                            // modded:
                            // fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord.x*1.7 + _RAIN*0.113, textCoord.y*2.82)).x + _RAIN*0.14 - textCoord.x)*6.28);
                            fog2 = 0.5 + 0.5f*sin((tex2D(_NoiseTex, half2(textCoord2.x*1.7 + _RAIN*0.113, textCoord2.y*2.82)).x + _RAIN*0.14 - textCoord2.x)*6.28);
                            fog2 *= clamp(1-distance(textCoord, half2(0,0.9)), 0, 1);
                            
                            fog2 = pow(fog2, 0.2);
                            fog2 *= amount;
                            fog2 *= 1 - pow(fog1, 1.5);
                            fog2 *= i.clr.w;
                            
                            if (fog2 > 0.5) {
                                return lerp(tex2D(_PalTex, float2(0, 7.0/8.0)), half4(1,1,1,1), fog2 > 0.6 ? 0.25 : 0.1);
                            }
                        }

                        fog1 = pow(fog1, 3);
                        fog1 *= i.clr.w;
                        //fog1 *= min(dp+0.1, 1);
                        fog1 = pow(fog1, 1 + (1-pow(dp, 0.1))*30);
                        fog1 = max(0, fog1 - (1-amount));
                        fog1 = pow(fog1, 0.2);

                        //return half4(fog1, fog1, 0, 1);
                        if (fog1 > 0.1) return half4(lerp(tex2D(_PalTex, float2(0, 2.0/8.0)), tex2D(_PalTex, float2(0, 7.0/8.0)), 0.5+0.5*dp).xyz, fog1 > 0.5 ? 0.6 : 0.2);
                        return half4(0,0,0,0);
                    #endif
                }
                ENDCG
			}
		} 
	}
}
