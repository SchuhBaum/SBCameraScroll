// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Edit of the Spores.shader for snow

// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

//Unlit Transparent Vertex Colored Additive 
Shader "Futile/SporesSnow" {
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

        SubShader   {
            Pass {
                //SetTexture [_MainTex] 
                //{
                //	Combine texture * primary
                //}



                CGPROGRAM
                #pragma target 4.0
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile __ HR
                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"

                //#pragma profileoption NumTemps=64
                //#pragma profileoption NumInstructionSlots=2048

                //float4 _Color;
                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _NoiseTex;
                sampler2D _PalTex;
                uniform float _fogAmount;
                float4 _lightDirAndPixelSize;
                float _cloudsSpeed;
                float _light = 0;
                sampler2D _SnowTex;
                //uniform float _waterPosition;

                //#if defined(SHADER_API_PSSL)
                //sampler2D _GrabTexture;
                //#else
                //sampler2D _GrabTexture : register(s0);
                //#endif

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
                    o.clr = fixed4(v.color.xyz,v.color.w*tex2Dlod(_SnowTex,float4(v.color.xy,0,0)).y);
                    return o;
                }

                float GetShadows (float a) {
                    // if (a==1.0) return 1;
                    a*=255;
                    return (step(round(a),90)*-1+1);
                }

                half4 frag (v2f i) : SV_Target {
                    float2 textCoord2 = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y + _RAIN)/_screenSize.y);
                    textCoord2.x -= _spriteRect.x;
                    textCoord2.y -= _spriteRect.y;
                    textCoord2.x /= _spriteRect.z - _spriteRect.x;
                    textCoord2.y /= _spriteRect.w - _spriteRect.y;
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y + _RAIN*153.2)/_screenSize.y);

                    //textCoord.y += _RAIN*0.02;

                    textCoord.x -= _spriteRect.x;
                    textCoord.y -= _spriteRect.y;

                    textCoord.x /= _spriteRect.z - _spriteRect.x;
                    textCoord.y /= _spriteRect.w - _spriteRect.y;

                    textCoord.y += 0.04;

                    float dist = clamp(1-distance(i.uv.xy, half2(0.5, 0.5))*2, 0, 1);



                    half h = (sin((1.77 * _RAIN + tex2D(_NoiseTex, float2(textCoord.x*5.2, _RAIN * - 0.1 + textCoord.y*2.6) ).x * 3) * 3.14 * 2)*0.5)+0.5;
                    h *= (sin((3.5 * _RAIN + tex2D(_NoiseTex, float2(textCoord.x*12.2, _RAIN * - 0.25 + textCoord.y*6.6) ).x * 3) * 3.14 * 2)*0.5)+0.5;


                    h *= 0.5 + 0.5 * sin((tex2D(_NoiseTex, i.uv.xy).x + _RAIN)*6.28*3);
                    // return half4(h, h, h, 1);


                    h = lerp(h*dist, lerp(h, 1, lerp(0.3,0.8,i.clr.w)), dist);

                    //half2 randomCoord = half2(textCoord.x, textCoord.y-_RAIN);

                    float rand = frac(sin(dot(textCoord.x, 12.98232)+textCoord.y-tex2D(_NoiseTex, textCoord).x) * 43758.5453);

                    h -= rand*lerp(0.7, 0.3, i.clr.w);
                    float shadows = GetShadows(tex2D(_LevelTex,textCoord2+ float2(_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(6*h),-_lightDirAndPixelSize.y*_lightDirAndPixelSize.z*(8*h))).x);
                    //h*=dist;


                    //////////////RAINWORLD SHADOWS
                    // vanilla:
                    // half shadow = tex2D(_NoiseTex, float2((textCoord2.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*6*h), 1-(textCoord2.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*6*h))).x;
                    // shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-textCoord2.y, 1)*3.14*2)*0.5;

                    // modded:
                    // The clouds light mask can be stretched. The mask is
                    // applied to the full level texture. Merged level
                    // textures contain multiple screens.
                    float number_of_screens_x = _spriteRect.z - _spriteRect.x;
                    float number_of_screens_y = _spriteRect.w - _spriteRect.y;
                    half shadow = tex2D(_NoiseTex, float2((number_of_screens_x*textCoord2.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*6*h), 1-(number_of_screens_y*textCoord2.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*6*h))).x;
                    shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-number_of_screens_y*textCoord2.y, 1)*3.14*2)*0.5;

                    // vanilla:
                    shadow = clamp(((shadow - 0.5)*6)+0.5-(_light*4), 0,1);
                    //////////////CONTINUE


                    fixed4 fog = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));

                    fixed4 snow = tex2D(_PalTex,float2(6*0.03333*0.9375,0.125+0.125));
                    fixed4 snowLight = tex2D(_PalTex,float2(6*0.03333*0.9375,0.57+0.125));
#if HR
                    snow = lerp(snow,snowLight,(-shadow+1)*shadows);
                    snow+=.05;
                    snow = lerp(snow,fog,_fogAmount*(6/30));
#else
                    snow = lerp(snow,snowLight,(-shadow+1)*shadows);
                    snow+=.3;
                    snow = lerp(snow,fog,_fogAmount*(6/30));
#endif


                    // return shadows;
                    // return float4(textCoord2.xy,0,1);
                    // return i.clr+fixed4(0,0,0,1);	

                    if(h * i.clr.w < 0.35)
                        return float4(0, 0, 0, 0);

                    if(h * i.clr.w > 0.5)
                        return half4(snow.xyz*0.7+.2,1);

                    return half4(snow.xyz,1);//rand, rand, rand, 1);

                }
                ENDCG
            }
        } 
    }
}
