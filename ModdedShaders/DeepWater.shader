// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance
Shader "SBCameraScroll/DeepWater" {
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
                //    Combine texture * primary
                //}
                
                CGPROGRAM

                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #pragma exclude_renderers OpenGL

                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"

                #pragma multi_compile __ Gutter

                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _NoiseTex;
                sampler2D _PalTex;

                #if defined(SHADER_API_PSSL)
                    sampler2D _GrabTexture;
                #else
                    sampler2D _GrabTexture : register(s0);
                #endif

                uniform float _RAIN;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                uniform float _waterDepth;
                float _waterLevel;

                struct v2f {
                    float4  pos : SV_POSITION;
                    float2  uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float4 clr : COLOR;

                    #if Gutter
                        float2 data[4] : TEXCOORD2;
                    #endif
                };

                float4 _MainTex_ST;
                float pulse(float pos, float width, float x) {
                    x = smoothstep(1.-width,1.,1.-abs((x-pos)));
                    return x;
                }

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos (v.vertex);
                    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.clr = v.color;

                    #if Gutter
                        float2 textCoord = float2(floor(o.scrPos.x*_screenSize.x)/_screenSize.x, floor(o.scrPos.y*_screenSize.y)/_screenSize.y);

                        textCoord.x -= _spriteRect.x;
                        textCoord.y -= _spriteRect.y;

                        textCoord.x /= _spriteRect.z - _spriteRect.x;
                        textCoord.y /= _spriteRect.w - _spriteRect.y;
                        textCoord*=fixed2(6,5);
                        o.data[0] = textCoord+float2(0,_RAIN*.5);
                        o.data[1] = -textCoord*float2(1.5,1.5)+float2(0,_RAIN*.25);
                        o.data[2] = textCoord*.25+float2(_RAIN*.25,0);
                        o.data[3] = -textCoord*.2+float2(_RAIN*.25,0);
                    #endif
                    return o;
                }

                float ShEaseOutCubic(float t) {
                    return (t = t - 1.0) * t * t + 1.0;
                }

                half4 frag (v2f i) : SV_Target
                {
                    //return half4(i.uv, 0, 1);

                    // modded:
                    // _spriteRect is the level texture divided into screens of size _screenSize;
                    // I want to scale it with how many cameras are combined into one big level texture;
                    // maybe I need (1400f, 800f) (camera size) instead of the (1366f, 768f) from _screenSize;
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

                    textCoord.x -= _spriteRect.x;
                    textCoord.y -= _spriteRect.y;

                    textCoord.x /= _spriteRect.z - _spriteRect.x;
                    textCoord.y /= _spriteRect.w - _spriteRect.y;

                    half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(textCoord.x*1.2, textCoord.y*1.2) ).x * 3) + 0/12.0) * 3.14 * 2)*0.5)+0.5;

                    // vanilla:
                    // float2 distortion = float2(lerp(-0.002, 0.002, rbcol) * lerp(1, 20, pow(i.uv.y, 200)), -0.02 * pow(i.uv.y, 8));

                    // modded:
                    // reduces the magnitude of the distortion; this could get out of hand in larger rooms;
                    float2 distortion = float2(lerp(-0.002, 0.002, rbcol) * lerp(1, 20, pow(i.uv.y, 200)) / (_spriteRect.z - _spriteRect.x), -0.02 * pow(i.uv.y, 8) / (_spriteRect.w - _spriteRect.y));
                    
                    // vanilla:
                    // distortion.x = floor(distortion.x*_screenSize.x)/_screenSize.x;
                    // distortion.y = floor(distortion.y*_screenSize.y)/_screenSize.y;

                    // modded:
                    // makes the distortion less blocky;
                    float2 level_texture_size = float2((_spriteRect.z - _spriteRect.x) * _screenSize.x, (_spriteRect.w - _spriteRect.y) * _screenSize.y);
                    distortion.x = floor(distortion.x * level_texture_size.x) / level_texture_size.x;
                    distortion.y = floor(distortion.y * level_texture_size.y) / level_texture_size.y;

                    half4 texcol = tex2D(_LevelTex, textCoord+distortion);

                    half plrLightDst = clamp(distance(half2(0,0),  half2((i.scrPos.x - i.clr.x)*(_screenSize.x/_screenSize.y), i.scrPos.y - i.clr.y))*lerp(21, 8, i.clr.z), 0, 1);


                    half grad = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
                    half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x+distortion.x, i.scrPos.y+distortion.y));

                    #if Gutter
                        fixed creaturemask = step(max(max(grabColor.x,grabColor.y),grabColor.z),0.0039);
                        // return creaturemask;
                        float noise1 = tex2D(_NoiseTex,i.data[0]+distortion).x;
                        float noise2 = tex2D(_NoiseTex,i.data[1]).x;
                        float noise3 = tex2D(_NoiseTex,i.data[2]).x;
                        float noise4 = tex2D(_NoiseTex,i.data[3]).x;

                        float noisemix = (noise1-noise2-noise3-noise4+1.)*.25;
                        float garbagemask = clamp(pulse(.4,pulse(1.,0.4,i.uv.y),noise2*noise4-noise1*noise3)*pulse(1.,0.3,i.uv.y),0,1);
                        // return noise;
                        // grad = lerp(grad,noise*.5,grad);
                        // grad = grad + noisemix;
                        // return garbagemask;
                        grad = grad - noisemix*creaturemask*ShEaseOutCubic(grad)+(garbagemask*.3)*creaturemask;
                        // return grad;
                    #endif  

                    plrLightDst = clamp(plrLightDst + pow(1.0-grad, 3), 0, 1);

                    if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) {
                        grad = 1;
                    }

                    #if Gutter
                        if (grabColor.x == 0.0 && grabColor.y == 2.0/255.0 && grabColor.z == 0.0) {
                            grad = 1;
                        } else if (grad > 6.0/30.0) {
                            if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                                if (grabColor.x == 0.0 && grabColor.y == 1.0/255.0 && grabColor.z == 0.0) {
                                    grad = 1;
                                    grabColor = half4(0,0,0,0);
                                } else {
                                    grad = 6.0/30.0;
                                    #if Gutter
                                    // grad += pulse(0.7,.3,1-noisemix)+garbagemask*.4;
                                    #endif
                                    grabColor *= lerp(tex2D(_PalTex, float2(5.5/32.0, 7.5/8.0)), half4(1,1,1,1), 0.75);
                                    plrLightDst = 1;
                                }
                            } else {
                                grabColor = half4(0,0,0,1);
                            }
                        }
                    #else
                        if (grabColor.x == 0.0 && grabColor.y == 2.0/255.0 && grabColor.z == 0.0) {
                            grad = 1;
                        } else if (grad > 6.0/30.0) {
                            if (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                                if (grabColor.x == 0.0 && grabColor.y == 1.0/255.0 && grabColor.z == 0.0) {
                                    grad = 1;
                                    grabColor = half4(0,0,0,0);
                                } else {
                                    grad = 6.0/30.0;
                                    grabColor *= lerp(tex2D(_PalTex, float2(5.5/32.0, 7.5/8.0)), half4(1,1,1,1), 0.75);
                                    plrLightDst = 1;
                                }
                            } else {
                                grabColor = half4(0,0,0,1);
                            }
                        }
                    #endif

                    if(fmod(round(tex2D(_LevelTex, textCoord).x*255)-1, 30.0)<_waterDepth*31.0) return float4(0, 0, 0, 0);
                    
                    grad = pow(grad, clamp(1-pow(i.uv.y, 10), 0.5, 1)) * i.uv.y;
                    // modded:
                    // does not work; makes thing just darker;
                    // grad = pow(grad, clamp(1-pow(i.uv.y, 10), 0.5, 1)) * i.uv.y / (_spriteRect.z - _spriteRect.x);

                    half whatToSine = (_RAIN*6) + (tex2D(_NoiseTex, float2((grad/10)+lerp(textCoord.x, 0.5, grad/3)*2.1 + distortion.x,  (_RAIN*0.1)+(grad/5)+lerp(textCoord.y, 0.5, grad/3)*2.1+distortion.y)).x * 7);
                    // modded:
                    // does not seem to do much; maybe it is faster;
                    // half whatToSine = (_RAIN*6) + (tex2D(_NoiseTex, float2((grad/10) + number_of_screens * lerp(textCoord.x, 0.5, grad/3)*2.1 + distortion.x,  (_RAIN*0.1)+(grad/5)+lerp(textCoord.y, 0.5, grad/3)*2.1+distortion.y)).x * 7);
                    half col = ((sin(whatToSine * 3.14 * 2)*0.5)+0.5);

                    whatToSine = (_RAIN*2.7) + (tex2D(_NoiseTex, float2((grad/7) + lerp(textCoord.x, 0.5, grad/5)*1.3 + distortion.x,  (_RAIN*-0.21)+(grad/8)+lerp(textCoord.y, 0.5, grad/6)*1.3+distortion.y)).x * 6.33);
                    // modded:
                    // same as above;
                    // whatToSine = (_RAIN*2.7) + (tex2D(_NoiseTex, float2((grad/7) + number_of_screens * lerp(textCoord.x, 0.5, grad/5)*1.3 + distortion.x,  (_RAIN*-0.21)+(grad/8)+lerp(textCoord.y, 0.5, grad/6)*1.3+distortion.y)).x * 6.33);
                    half col2 = (sin(whatToSine * 3.14 * 2)*0.5)+0.5;

                    col = pow(max(col, col2), 47);// * i.uv.y;
                    if (col >= 0.8) {
                        grad = min(grad + 0.1*(1.0-abs(0.5-grad)*2.0) * i.uv.y, 1);
                    }

                    plrLightDst = pow(plrLightDst, 3);
                    grad = lerp(grad, pow(grad, lerp(0.2, 1, plrLightDst)), i.clr.z);

                    //clamp((1-pow(i.uv.y, 1.5))*2, 0, 1)
                    texcol = lerp(tex2D(_PalTex, float2(5.5/32.0, 7.5/8.0)), tex2D(_PalTex, float2(4.5/32.0, 7.5/8.0)), grad);
                    #if Gutter
                        // texcol +=garbagemask*.1;
                        if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                            return lerp(tex2D(_PalTex, float2(5.5/32.0, 7.5/8.0)), tex2D(_PalTex, float2(4.5/32.0, 7.5/8.0)), grad+pulse(0.7,.3,1-noisemix)+garbagemask*.2);
                        } else {
                            return texcol;
                        }
                    #endif

                    if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                        return lerp(texcol, grabColor, pow(clamp((i.uv.y-0.9)*10, 0, 1), 3));
                    } else {
                        return texcol;
                    }
                }
                ENDCG
            }
        } 
    }
}