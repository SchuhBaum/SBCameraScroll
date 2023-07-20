
//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

//Unlit Transparent Vertex Colored Additive 
Shader "SBCameraScroll/LevelHeat" {
    Properties {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}

        //_PalTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        //_NoiseTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        //_RAIN ("Rain", Range (0,1.0)) = 0.5
        //_Color ("Main Color", Color) = (1,0,0,1.5)
        //_BlurAmount ("Blur Amount", Range(0,02)) = 0.0005
    }

    Category {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
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
            GrabPass { }
            Pass {
                //SetTexture [_MainTex] {
                //    Combine texture * primary
                //}

                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

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

                //float _BlurAmount;

                uniform float _palette;
                uniform float _RAIN;
                uniform float _light = 0;
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
                    float4 clr : COLOR;
                };

                float4 _MainTex_ST;

                v2f vert(appdata_full v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos (v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                    // vanilla:
                    // o.uv2 = o.uv-_MainTex_TexelSize*.5*_rimFix;

                    // modded:
                    o.uv2 = o.uv - _LevelTex_TexelSize * .5 * _rimFix;
                    
                    o.clr = v.color;
                    return o;
                }

                inline float3 applyHue(float3 aColor, float aHue) {
                    float angle = radians(aHue);
                    float3 k = float3(0.57735, 0.57735, 0.57735);
                    float cosAngle = cos(angle);
                    //Rodrigues' rotation formula
                    return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
                }

                half4 frag(v2f i) : SV_Target {
                    half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
                    bool checkMaskOut = false;

                    float ugh = fmod(fmod(round(tex2D(_MainTex, float2(i.uv.x, i.uv.y)).x * 255), 90) - 1, 30) / 300.0;
                    float displace = tex2D(_NoiseTex, float2((i.uv.x * 1.5) - ugh + (_RAIN*0.01), (i.uv.y*0.25) - ugh + _RAIN * 0.05)).x;
                    displace = clamp((sin((displace + i.uv.x + i.uv.y + _RAIN*0.1) * 3 * 3.14) - 0.95) * 20, 0, 1);

                    half2 screenPos = half2(lerp(_spriteRect.x+_screenOffset.x, _spriteRect.z+_screenOffset.x, i.uv.x), lerp(_spriteRect.y+_screenOffset.y, _spriteRect.w+_screenOffset.y, i.uv.y));
                    #if UNITY_UV_STARTS_AT_TOP
                        screenPos.y = 1 - screenPos.y;
                    #endif

                    if (_WetTerrain < 0.5 || 1 - screenPos.y > _waterLevel) {
                        displace = 0;
                    }

                    half4 texcol = tex2D(_MainTex, float2(i.uv2.x, i.uv2.y+displace*0.001));
                    int red = round(texcol.x * 255);
                    int green = round(texcol.y * 255);
                    float effectCol = 0;

                    if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) {
                        setColor = tex2D(_PalTex, float2(0.5 / 32.0, 7.5 / 8));
                        if(_rimFix>.5) {
                            setColor = _AboveCloudsAtmosphereColor;
                        }
                        checkMaskOut = true;
                    } else {
                        half notFloorDark = 1;
                        if (green >= 16) {
                            notFloorDark = 0;
                            green -= 16;
                        }

                        if (green >= 8) {
                            green -= 8;
                        }

                        half shadow = tex2D(_NoiseTex, float2((i.uv.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0)), 1 - (i.uv.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0)))).x;
                        shadow = 0.5 + sin(fmod(shadow + (_RAIN*0.1*_cloudsSpeed) - i.uv.y, 1)*3.14 * 2)*0.5;
                        shadow = clamp(((shadow - 0.5) * 6) + 0.5 - (_light * 4), 0, 1);

                        if (red > 90) {
                            red -= 90;
                        } else {
                            shadow = 1.0;
                        }

                        int paletteColor = clamp(floor((red - 1) / 30.0), 0, 2); //some distant objects want to get palette color 3, so we clamp it
                        red = fmod(red - 1, 30.0);

                        if (shadow != 1 && red >= 5) {
                            half2 grabPos = float2(screenPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(red - 5), 1 - screenPos.y + _lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(red - 5));
                            grabPos = ((grabPos - half2(0.5, 0.3))*(1 + (red - 5.0) / 460.0)) + half2(0.5, 0.3);
                            
                            float4 grabTexCol = tex2D(_GrabTexture, grabPos);
                            if (grabTexCol.x != 0.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                                //red = 5;
                                shadow = 1;
                            }
                        }

                        // ============
                        if (red >= 5) {
                            float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1 - screenPos.y));
                            if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                                red = 5;
                            }
                        }

                        effectCol = tex2D(_NoiseTex, half2(i.uv.x, i.uv.y*0.5 - _RAIN*0.123));
                        effectCol = 0.5 + 0.5 * sin(effectCol*3.14 * 8 + i.uv.y*36.231 - _RAIN*2.63 + (red / 30.0)*8.12);
                        effectCol *= 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2((1.0 - i.uv.x) * 2, i.uv.y*0.5 - _RAIN*0.1862))*3.14 * 8 + i.uv.y*50.231 - _RAIN*4.75442 + (red / 30.0)*8.12);
                        effectCol = 1 - effectCol;

                        effectCol = pow(effectCol, 30);
                        texcol = tex2D(_MainTex, float2(i.uv.x, i.uv.y + lerp(-0.01, 0.02, effectCol)*i.clr.w));

                        //screenPos = half2(lerp(_spriteRect.x+_screenOffset.x, _spriteRect.z+_screenOffset.x, i.uv.x), lerp(_spriteRect.y+_screenOffset.y, _spriteRect.w+_screenOffset.y, i.uv.y));// + lerp(-0.01, 0.02, effectCol)*i.clr.w));
                        //#if UNITY_UV_STARTS_AT_TOP
                        //  screenPos.y = 1 - screenPos.y;
                        //#endif
                        //screenPos.x = (floor(screenPos.x * 1366) + 0.5) / 1366;
                        //screenPos.y = (floor(screenPos.y * 768) + 0.5) / 768;

                        red = round(texcol.x * 255);
                        red = fmod(red - 1, 30.0);

                        effectCol = 1 - abs(effectCol - 0.5) * 2;
                        effectCol = pow(max(0,effectCol - (1.0 - i.clr.w)), lerp(10.0, 1.0, i.clr.w));

                        ////setColor = tex2D(_PalTex, float2((red*notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0));
                        setColor = lerp(tex2D(_PalTex, float2((red*notFloorDark) / 32.0, (paletteColor + 3 + 0.5) / 8.0)), tex2D(_PalTex, float2((red*notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0)), shadow);

                        if (green >= 8) {
                            effectCol = 100;
                        } else if (green > 0 && green < 3) {
                            effectCol = green;
                        }
                        // ==============

                        half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(i.uv.x * 2, i.uv.y * 2)).x * 4) + red / 12.0) * 3.14 * 2)*0.5) + 0.5;
                        setColor = lerp(setColor, tex2D(_PalTex, float2((5.5 + rbcol * 25) / 32.0, 6.5 / 8.0)), (green >= 4 ? 0.2 : 0.0) * _Grime);

                        if (effectCol == 100) {
                            half4 decalCol = tex2D(_MainTex, float2((255.5 - round(texcol.z*255.0)) / 1400.0, 799.5 / 800.0));
                            if (paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
                            decalCol = lerp(decalCol, tex2D(_PalTex, float2(1.5 / 32.0, 7.5 / 8.0)), red / 60.0);
                            setColor = lerp(lerp(setColor, decalCol, 0.7), setColor*decalCol*1.5, lerp(0.9, 0.3 + 0.4*shadow, clamp((red - 3.5)*0.3, 0, 1)));
                        } else if (green > 0 && green < 3) {
                            setColor = lerp(setColor, lerp(lerp(tex2D(_PalTex, float2(30.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_PalTex, float2(31.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), shadow), lerp(tex2D(_PalTex, float2(30.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_PalTex, float2(31.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), shadow), red / 30.0), texcol.z);
                        } else if (green == 3) {
                            setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
                        }
                        setColor = lerp(setColor, tex2D(_PalTex, float2(1.5 / 32.0, 7.5 / 8.0)), clamp(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_fogAmount / 30.0, 0, 1));

                        //if (red >= 5) {
                        //    checkMaskOut = true;
                        //}
                    }

                    if (red >= 5) {
                        float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1-screenPos.y));
                        if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                            setColor = grabTexCol;
                            setColor.w = 0;
                            red = 5;
                            effectCol = 0;
                        }
                    }

                    if (checkMaskOut) {
                        float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1-screenPos.y));
                        if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
                            setColor.w = 0;
                        }
                    } else if (green == 0 || green >= 3) {
                        if (setColor.x == -1) {
                            setColor = tex2D(_PalTex, float2(0.5 / 32.0, 7.5 / 8));
                            effectCol = pow(1 - i.uv.y, 3) * 0.5 * i.clr.w;
                        }

                        effectCol = lerp(effectCol, 1, (red / 30.0)*0.1*i.clr.w);
                        if (effectCol < 0.75) {
                            setColor = lerp(setColor, lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)), tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)), red / 30.0), effectCol / 0.75);
                        } else {
                            setColor = lerp(lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)), tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)), red / 30.0), half4(1, 1, 1, 1), effectCol - 0.75);
                        }
                    }

                    // Color Adjustment params
                    setColor.rgb *= _darkness;
                    setColor.rgb = ((setColor.rgb - 0.5) * _contrast) + 0.5;
                    float greyscale = dot(setColor.rgb, float3(.222, .707, .071));  // Convert to greyscale numbers with magic luminance numbers
                    setColor.rgb = lerp(float3(greyscale, greyscale, greyscale), setColor.rgb, _saturation);
                    setColor.rgb = applyHue(setColor.rgb, _hue);
                    setColor.rgb += _brightness;
                    return setColor;
                }
            ENDCG
            }
        }
    }
}