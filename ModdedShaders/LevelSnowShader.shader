// not used yet; not needed for making the snow less blocky; I missed the snow texture
// in the class RoomCamera; it needs to fit the level texture;
// 
// what I can do here probably is to adjust the radius rad in x and y separately; TODO

Shader "SBCameraScroll/LevelSnowShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
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

        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag        
            #pragma profileoption NumInstructionSlots=4096
            #pragma profileoption NumMathInstructionSlots=4096
            #pragma exclude_renderers OpenGL
            #include "UnityCG.cginc"
            #include "_ShaderFix.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 scrPos : TEXCOORD1;
                float4 clr : COLOR;
            };

            #if defined(SHADER_API_PSSL)
                sampler2D _GrabTexture;
            #else
                sampler2D _GrabTexture : register(s0);
            #endif

            sampler2D _PalTex;
            float _light = 0;
            sampler2D _MainTex;
            float2 _MainTex_TexelSize;
            float4 _MainTex_ST;
            sampler2D _LevelTex;
            float2 _LevelTex_TexelSize;
            float4 _lightDirAndPixelSize;
            float2 _screenSize;
            float4 _spriteRect;
            float4 _EffectColor;
            float _WetTerrain;
            float _waterLevel;
            float _RAIN;
            float _cloudsSpeed;
            float _fogAmount;
            sampler2D _NoiseTex;
            sampler2D _NoiseTex2;

            // this is a 7x7 texture called snow light; I guess scaling this does not make sense;
            sampler2D _SnowSources;
            float2 _SnowSources_TexelSize;

            v2f vert (appdata_full v) {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.clr = v.color;
                o.scrPos = ComputeScreenPos(o.pos);
                return o;
            }

            float GetDepth (float a) {
                a = clamp(a,0,1);
                if (a==1.0) return 255;
                a=round(a*255);
                float shadows = (step(a,90)*-1+1)*90;
                return fmod(a-shadows-1, 30);
            }

            float3 hsv2rgb(float3 c) {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float GetShadows (float a) {
                // if (a==1.0) return 1;
                a*=255;
                return (step(round(a),90)*-1+1);
            }

            float cubicPulse( float c, float w, float x ) {
                x = abs(x - c);
                if( x>w ) return 0.0;
                x /= w;
                return 1.0 - x*x*(3.0-2.0*x);
            }

            float easeInExpo(float t) {
                return (t == 0.0) ? 0.0 : pow(2.0, 10.0 * (t - 1.0));
            }

            float easeOutExpo(float t) {
                return (t == 1.0) ? 1.0 : -pow(2.0, -10.0 * t) + 1.0;
            }

            float easeOutCubic(float t) {
                return (t = t - 1.0) * t * t + 1.0;
            }

            float impulse( float k, float x )
            {
                const float h = k*x;
                return h*exp(1.0-h);
            }

            float easeOutQuad(float t) {
                return -1.0 * t * (t - 2.0);
            }

            float easeInQuad(float t) {
                return t * t;
            }

            float easeInOutExpo(float t) {
                if (t == 0.0 || t == 1.0) {
                    return t;
                }
                if ((t *= 2.0) < 1.0) {
                    return 0.5 * pow(2.0, 10.0 * (t - 1.0));
                } else {
                    return 0.5 * (-pow(2.0, -10.0 * (t - 1.0)) + 2.0);
                }
            }

            float fract(float x) {
                return x - floor(x);
            }

            float rand(float x) {
                return fract(sin(x)*1000000.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int iter = 20;
                int invIter = 10;

                // seems to only increase snow;
                // int iter = 20 * 10;

                // modded:
                // still way too skewed;
                // _LevelTex_TexelSize = float2((_spriteRect.z - _spriteRect.x) / _screenSize.x, (_spriteRect.w - _spriteRect.y) / _screenSize.y);
                // streches way too much in y-direction;
                // _LevelTex_TexelSize = float2(_screenSize.x / (_spriteRect.z - _spriteRect.x), _screenSize.y / (_spriteRect.w - _spriteRect.y));

                // modded:
                // float2 updated_LevelTex_TexelSize = _LevelTex_TexelSize;

                // this seems to mess with it to much; skewed and stuff; but it should be accurate information about the texel size; :/
                float2 updated_LevelTex_TexelSize = float2(1.0 / ((_spriteRect.z - _spriteRect.x) * _screenSize.x), 1.0 / ((_spriteRect.w - _spriteRect.y) * _screenSize.y));

                // float2 updated_LevelTex_TexelSize = float2(1/1400, 1/800);

                // does not seem to help:
                // updated_LevelTex_TexelSize.y = updated_LevelTex_TexelSize.y * (_spriteRect.w - _spriteRect.y);
                
                // increases the snow amount; does not seem to make it less pixelated;
                // float2 updated_SnowSources_TexelSize = _SnowSources_TexelSize / 1000.0;
                // float2 updated_SnowSources_TexelSize = _SnowSources_TexelSize * 30.0;

                // removes the snow;
                // float2 updated_SnowSources_TexelSize = _SnowSources_TexelSize * 1000.0;

                float2 updated_SnowSources_TexelSize = _SnowSources_TexelSize;
                // updated_SnowSources_TexelSize.y = updated_SnowSources_TexelSize.y / (_spriteRect.w - _spriteRect.y);

                float ratio = updated_LevelTex_TexelSize.y/updated_LevelTex_TexelSize.x;
                float ratio2 = updated_LevelTex_TexelSize.x/updated_LevelTex_TexelSize.y;

                // modded:
                // does not seem to do much;
                // ratio = ratio * (_spriteRect.w - _spriteRect.y) / (_spriteRect.z - _spriteRect.x);
                // ratio2 = ratio2 * (_spriteRect.z - _spriteRect.x) / (_spriteRect.w - _spriteRect.y);
                
                /////////////////////   TEXTURE COORDINATE////////////////////

                // modded:
                // does not seem to do much; it is overriden anyways and the exact uv coordinates are used;
                // float2 level_texture_size = float2((_spriteRect.z - _spriteRect.x) * _screenSize.x, (_spriteRect.w - _spriteRect.y) * _screenSize.y);

                // float2 textCoord = float2(floor(i.scrPos.x*level_texture_size.x)/level_texture_size.x, floor(i.scrPos.y*level_texture_size.y)/level_texture_size.y);
                // textCoord.x -= _spriteRect.x;
                // textCoord.y -= _spriteRect.y;

                // textCoord.x /= _spriteRect.z - _spriteRect.x;
                // textCoord.y /= _spriteRect.w - _spriteRect.y;
                ///////////////////////////////////////////////////////////////

                /////////////////////////////////////////

                // not using this makes the snow misalign; does not make it less blocky;
                float2 textCoord = i.uv;



                float input = 1;
                // float input = i.clr.w*dist;
                // float input = fmod(_Time.x*.2,1);
                float input2 = 1;
                float input3 = 1;

                ////////// SNOW HEATMAP //////////////
                float snowIntensity = 1;
                float snowNoise = 1;
                float circle = 1;
                float noiseX = tex2D(_NoiseTex2,float2(textCoord.x,textCoord.y)*1).x;
                float noise0 = tex2D(_NoiseTex,float2(textCoord.x,textCoord.y)*2).x+noiseX*.3;

                // 20 is the maximum number of snow sources that are allowed per room;
                for(int o = 0; o < 20; o++)
                {
                    int hor = o%7; // 0-6 // 0 1 2 3 4 5 6 0 .. 3 4 5
                    int hor2 = (o+20)%7; // = (hor + 6) % 7? // 6 0 1 2 3 4 5 6 .. 3 4
                    int hor3 = ((int)(o/4)+40)%7; // = (floor(o/4) + 5) % 7 // 5 5 5 5 6 6 6 6 0 0 0 0 .. 2 2

                    int ver = o/7; // 0 0 0 0 0 0 0 1 1 1 .. 2 2
                    int ver2 = (o+20)/7; // 2 3 3 3 .. 5 5
                    int ver3 = ((int)(o/4)+40)/7; // = floor(o/4) + 40/7 // 40/7 .. 4+40/7

                    int count = o%4; // 0 1 2 3 0 1 2 3 .. 0 1 2 3 // index for xyzw;

                    // snow sources is a 7x7 texture;

                    // line by line; (0, 0) until (5, 2);
                    fixed4 snow = tex2D(_SnowSources,float2(updated_SnowSources_TexelSize.x * .5 + updated_SnowSources_TexelSize.x * hor, updated_SnowSources_TexelSize.y * .5 + updated_SnowSources_TexelSize.y * ver));

                    // line by line starting with an offset; (6, 2) until (4, 5); (4+1) + 5 * number_of_elements_per_row = 5 + 5 * 7 = 40; 
                    // 9 elements of the snow sources texture are not sampled in snow or snow2;
                    fixed4 snow2 = tex2D(_SnowSources,float2(updated_SnowSources_TexelSize.x*.5+updated_SnowSources_TexelSize.x*hor2,updated_SnowSources_TexelSize.y*.5+updated_SnowSources_TexelSize.y*ver2));

                    int snow3 = (tex2D(_SnowSources,float2(updated_SnowSources_TexelSize.x*.5+updated_SnowSources_TexelSize.x*hor3,updated_SnowSources_TexelSize.y*.5+updated_SnowSources_TexelSize.y*ver3))[count]) * 5 + .4;
                    
                    // EncodeFloatRG approximates a float between 0 and 1 with a float that has step size 1/255;
                    // it saves the approximation and remainder in a float2 vector;
                    // the remainder is multiplied by 255 for some reason; DecodeFloatRG does the opposite;
                    // why do I encode this?; is snow.x and snow.z used somewhere?;
                    float2 coord = (float2(DecodeFloatRG(snow.xy),DecodeFloatRG(snow.zw))-.3)*3.33333;
                    float rad = DecodeFloatRG(snow2.xy)*(4);
                    float2 snowcoord = textCoord/rad;
                    coord = coord/rad;

                    // square
                    circle = smoothstep(.5,.4,abs((snowcoord.x*ratio-coord.x*ratio)));
                    circle = -clamp(circle*smoothstep(.5,.4,abs((snowcoord.y-coord.y))),0,1)+1;
                    if (snow3 >= 1&&snow3<4)
                    {    
                        // Radial
                        circle = clamp(easeOutCubic(length(float2(snowcoord.x*ratio-coord.x*ratio,snowcoord.y-coord.y))),0,1);
                    }

                    if (snow3 == 2) {
                        // Stripe
                        circle = clamp(circle + smoothstep(updated_LevelTex_TexelSize.y*(iter+invIter)*.1,updated_LevelTex_TexelSize.y*(iter+invIter),abs(textCoord.y-coord.y*rad)),0,1);
                    }

                    if (snow3 == 3) {
                        // Column
                        circle = clamp(circle + smoothstep(updated_LevelTex_TexelSize.x*(iter+invIter)*.1,updated_LevelTex_TexelSize.x*(iter+invIter),abs(textCoord.x-coord.x*rad)),0,1);
                    }    

                    if(snow3 == 4) {
                        snowIntensity = clamp(snowIntensity+ -lerp(1-snow2.z,1,circle)+1,0,1);
                        snowNoise = clamp(snowNoise+ -lerp(1-snow2.w,1,circle)+1,0,1);
                    }
                    else {
                        snowIntensity *= clamp(lerp(1-snow2.z,1,circle),0,1);
                        snowNoise *= clamp(lerp(1-snow2.w,1,circle),0,1);
                    }
                }

                input = -snowIntensity+1;
                input2 = -snowNoise+1;

                ////////////////////////////////////////
                //////////////////////////////////     CREATURE MASK
                half mask = 100;
                half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));

                if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) {
                    mask = 5;
                }

                ///////////////////////////////////////////////////////
                // sample the texture
                float2 distUV=float2(textCoord.x,textCoord.y);

                fixed4 color = tex2D(_LevelTex, distUV);
                float col = GetDepth(color.x);
                float thicOffset = 2+trunc((invIter-2)*clamp(input*1.5*noise0*.7,0,1));

                for(int b = 0;b<invIter;b++)
                {
                    if (b<thicOffset){
                        // col = max(col,GetDepth(tex2D(_LevelTex, float2(distUV.x,distUV.y+updated_LevelTex_TexelSize.y*b)).x));
                    }
                }

                float col2 = 0;
                float col3 = col;
                float col4 = col;
                float col5 = col;
                float noise =tex2D(_NoiseTex,float2(distUV.x,distUV.y+col*.03)*4).x;
                float noise1 =tex2D(_NoiseTex,float2(distUV.x,distUV.y)*5).x;
                float noisemix = noise1-lerp(.25,.3,noise);
                float shadows = GetShadows(color);
                float height = clamp(lerp(input,input-noisemix-.15*(1-input),(.5-input*.5)+input2)*iter,0,iter);
                float snowMask = 1;
                float tex2 = 1;
                float tex3 = 1;
                float tsg = 0;
                float sg = 0;
                float tex = 0;
                for (int b = 0;b<iter;b++)
                {
                    if (b<height){
                        float tex = tex2D(_LevelTex, float2(distUV.x, distUV.y-updated_LevelTex_TexelSize.y*b)).x;
                        col2 = GetDepth(tex);
                        /////////////////////EXPERIMENTAL SHIT
                        tex2 = tex2D(_LevelTex, float2(distUV.x-updated_LevelTex_TexelSize.x*trunc(b*.35), distUV.y-updated_LevelTex_TexelSize.y*b)).x;
                        tex3 = tex2D(_LevelTex, float2(distUV.x+updated_LevelTex_TexelSize.x*trunc(b*.35), distUV.y-updated_LevelTex_TexelSize.y*b)).x;
                        col4 = min(col4,GetDepth(tex2));
                        col5 = min(col5,GetDepth(tex3));
                        col2 = max(col5,col2);
                        col2 = max(col4,col2);
                        ////////////////////////END
                        shadows = max(shadows,GetShadows(tex));
                        col3 = min(col3,col2);
                        sg=max(sg,(-step(tsg-col3,0.001)+1)*b);
                        tsg=col3;
                    }
                }
                col3 = trunc(col3);
                col = trunc(col);
                half shadowGradient = clamp(trunc((sg/height)*3)/3*0.3529,0,1);
                // half shadowGradient = clamp((trunc((sg/height)*3)-1)/765*30,0,1);
                // half shadowGradient = clamp(trunc(sg/(height*.3))*.4,0,1);
                // half shadowGradient = 0;
                if(col==col3) snowMask = 0;
                // if(col3>mask) snowMask = 0;
                // return (float4)shadowGradient+float4(0,0,0,1);


                return float4(clamp((col3+1)/255 + shadows*90/255+shadowGradient,0,1), snowMask, 0, 0) + float4(0,0,0,1);

                // if (rand(textCoord.x + textCoord.y) > 0.5) {
                //     return float4(1,1,0,1);
                // }
                // return float4(1,0,0,1);
            }
            ENDCG
        }
    }
}
