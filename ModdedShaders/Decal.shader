// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

//Unlit Transparent Vertex Colored Additive 
Shader "Futile/Decal" {
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


                uniform float _palette;
                uniform float _RAIN;
                uniform float _light = 0;
                uniform float4 _spriteRect;

                uniform float4 _lightDirAndPixelSize;
                uniform float _fogAmount;
                uniform float _waterLevel;
                uniform float _Grime;
                uniform float _SwarmRoom;
                uniform float _WetTerrain;
                uniform float _cloudsSpeed;

#if defined(SHADER_API_PSSL)
                sampler2D _GrabTexture;
#else
                sampler2D _GrabTexture : register(s0);
#endif
                sampler2D _MainTex;
                sampler2D _NoiseTex;
                sampler2D _NoiseTex2;
                sampler2D _LevelTex;
                sampler2D _PalTex;

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


                half3 Multiply(half3 cA, half3 cB){
                    return half3(cA.x*cB.x, cA.y*cB.y, cA.z*cB.z);
                }
                half3 Screen(half3 cA, half3 cB){
                    return half3(1.0-(1.0-cA.x)*(1.0-cB.x), 1.0-(1.0-cA.y)*(1.0-cB.y), 1.0-(1.0-cA.z)*(1.0-cB.z));
                }
                half3 Overlay(half3 cA, half3 cB){
                    return half3(
                        cB.x <= 0.5 ? cA.x*cB.x : 1.0-(1.0-cA.x)*(1.0-cB.x), 
                        cB.y <= 0.5 ? cA.y*cB.y : 1.0-(1.0-cA.y)*(1.0-cB.y), 
                        cB.z <= 0.5 ? cA.z*cB.z : 1.0-(1.0-cA.z)*(1.0-cB.z)
                    );
                }

                half4 frag (v2f i) : SV_Target {
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

                    textCoord.x -= _spriteRect.x;
                    textCoord.y -= _spriteRect.y;

                    textCoord.x /= _spriteRect.z - _spriteRect.x;
                    textCoord.y /= _spriteRect.w - _spriteRect.y;


                    float ugh = fmod(fmod(   round(tex2D(_MainTex, float2(textCoord.x, textCoord.y)).x*255)   , 90)-1, 30)/300.0;
                    float displace = tex2D(_NoiseTex, float2((textCoord.x * 1.5) - ugh + (_RAIN*0.01), (textCoord.y*0.25) - ugh + _RAIN * 0.05)   ).x;
                    displace = clamp((sin((displace + textCoord.x + textCoord.y + _RAIN*0.1) * 3 * 3.14)-0.95)*20, 0, 1);



                    //half2 screenPos = half2(lerp(_spriteRect.x, _spriteRect.z, textCoord.x), lerp(_spriteRect.y, _spriteRect.w, textCoord.y));

                    if (_WetTerrain < 0.5 || 1-i.scrPos.y > _waterLevel) displace = 0;

                    half4 texcol = tex2D(_LevelTex, float2(textCoord.x, textCoord.y+displace*0.001));


                    if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0){
                        return half4(0,0,0,0);
                    }else{
                        int red = round(texcol.x * 255);

                        // vanilla:
                        // half shadow = tex2D(_NoiseTex, float2((textCoord.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0)), 1-(textCoord.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0)))).x;
                        // shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-textCoord.y, 1)*3.14*2)*0.5;

                        // modded:
                        // The clouds light mask can be stretched. The mask is
                        // applied to the full level texture. Merged level
                        // textures contain multiple screens.
                        float number_of_screens_x = _spriteRect.z - _spriteRect.x;
                        float number_of_screens_y = _spriteRect.w - _spriteRect.y;
                        half shadow = tex2D(_NoiseTex, float2((number_of_screens_x*textCoord.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0)), 1-(number_of_screens_y*textCoord.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0)))).x;
                        shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-number_of_screens_y*textCoord.y, 1)*3.14*2)*0.5;

                        // vanilla:
                        shadow = clamp(((shadow - 0.5)*6)+0.5-(_light*4), 0,1);

                        if (red > 90) red -= 90;
                        else  shadow = 1.0;


                        int paletteColor = clamp(floor((red-1)/30.0), 0, 2);//some distant objects want to get palette color 3, so we clamp it


                        red = fmod(red-1, 30.0);

                        if(red / 30.0 < i.clr.x || red / 30.0 > i.clr.y) return half4(0,0,0,0);


                        if (shadow != 1 && red >= 5) {
                            half2 grabPos = float2(i.scrPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(red-5), i.scrPos.y + _lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(red-5));
                            grabPos = ((grabPos-half2(0.5, 0.3))*(1 + (red-5.0)/460.0))+half2(0.5, 0.3);
                            float4 grabTexCol2 = tex2D(_GrabTexture, grabPos);
                            if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0){
                                shadow = 1;
                            }
                        }

                        half4  terrainCol = lerp(tex2D(_PalTex, float2((red)/32.0, (paletteColor + 3 + 0.5)/8.0)), tex2D(_PalTex, float2((red)/32.0, (paletteColor + 0.5)/8.0)), shadow);

                        terrainCol = lerp(terrainCol, tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)), clamp(red*_fogAmount/30.0, 0, 1));


                        if (red >= 5){
                            float4 grabTexCol = tex2D(_GrabTexture, float2(i.scrPos.x, i.scrPos.y));
                            if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0)
                                return half4(0,0,0,0);
                        }

                        half2 grabPos = i.uv;
                        grabPos -= half2(0.5, 0.5);
                        grabPos *= lerp(1, 1.5, red/30.0);
                        grabPos += half2(0.5, 0.5);

                        half h = textCoord.x;
                        h = lerp(floor(h*700.0)/700.0, h, 0.5);

                        grabPos.y += pow(max(0,tex2D(_NoiseTex2, half2(h*4, textCoord.y*0.01 + red/100.0)).x-0.5)*2, 3-i.clr.z)*0.3*i.clr.z;

                        half4 myCol = tex2D(_MainTex, grabPos);
                        half3 lightCol = lerp(terrainCol.xyz, myCol.xyz, 0.5);
                        lightCol = lerp(lightCol, Screen(terrainCol.xyz, myCol.xyz), (1.0-shadow)*(paletteColor == 2 ? 0.9 : 0.25));
                        return half4(lerp(lightCol, Multiply(terrainCol.xyz, myCol.xyz), lerp(0.2, 0.3, shadow)), myCol.w*lerp(1, 0.25+0.25*i.clr.w, shadow)*i.clr.w);
                        // return half4(lerp(Overlay(terrainCol.xyz, myCol.xyz), Multiply(terrainCol.xyz, myCol.xyz), lerp(0.2, 0.8, shadow)), myCol.w*lerp(1, 0.3, shadow)*i.clr.w);

                        // return half4(lerp(lerp(terrainCol.xyz, myCol.xyz, 0.5), lerp(Screen(terrainCol.xyz, myCol.xyz), Multiply(terrainCol.xyz, myCol.xyz), lerp(0.2, 0.8, shadow)), 0.5), myCol.w*lerp(1, 0.3, shadow)*i.clr.w);
                        //return half4(Overlay(terrainCol.xyz, myCol.xyz), myCol.w*i.clr.w);

                    }
                }
                ENDCG
            }
        } 
    }
}
