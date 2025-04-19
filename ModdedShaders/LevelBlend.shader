Shader "SBCameraScroll/LevelBlend"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="False" "RenderType"="Transparent"}
		// Blend SrcAlpha OneMinusSrcAlpha 
		//Alphatest Greater 0
		Blend Off
		Lighting Off
		Cull Off 
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
            #pragma multi_compile _ GAMEPLAYRIPPLETEXTURE
			#include "UnityCG.cginc"
			#include "_ShaderFix.cginc"
            #include "_Functions.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float2 uv2 : TEXCOORD1;
				float4 clr : COLOR;
				float2 grainCoord : TEXCOORD2;
			};
			sampler2D _MainTex;
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			sampler2D _LevelTex;
			float2 _screenSize;
			float4 _spriteRect;
            float2 _SlowFollowCreatureScreenPos;
			float _RAIN;
            sampler2D _RippleMask;
            sampler2D _GameplayRippleMask;
            sampler2D _RippleMask_TexelSize;
            sampler2D _GameplayRippleTexture;
            sampler2D _PlayerCamoMaskSaved;
            sampler2D _UniNoise;

            float2 scale_from(float2 uv, float2 p, float scale) {
                float2 offsetUV = uv - p;
                offsetUV *= scale;
                return offsetUV + p - uv;
            }

			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);

                // This is from zero to one over the full room texture.
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.clr = v.color;

                // This is more like a fullscreen effect. It is zero to one over
                // the screen size. (For example in x, at the left edge of the
                // screen the value is always <= 0 and at the right side it is
                // always >= 1. Negative values are treated like zero and values
                // >= 1 are like one. Moving the character (scrolling) changes
                // the values but the bounds don't change.)
				o.uv2 = lerp(_spriteRect.xy,_spriteRect.zw,o.uv);

                o.grainCoord = o.uv*fixed2(6,4);
				return o;
			}

            fixed step3(fixed3 a, fixed3 b){
                return saturate(step(a.x,b.x)+step(a.y,b.y)+step(a.z,b.z));
            }

			fixed4 frag (v2f i) : SV_Target
            {
                // modded:
                // We need to scale the distortion effect when using full room
                // textures. Otherwise the distortion scales with the number of
                // screens. Without using camera scroll is this roughtly one.
                //
                // There is still a small mismatch. In WRSA_L01 you should stand
                // normally on top of the pillar structure instead of slightly
                // clipping into the ground.
                float2 distortion_scale = _spriteRect.zw - _spriteRect.xy;

                fixed3 shiftTex = tex2D(_RippleMask,i.uv2);
                fixed playerMask = step3(tex2D(_PlayerCamoMaskSaved,i.uv2).xyz,.00001);
                fixed gameplayShiftTex = tex2D(_GameplayRippleMask,i.uv2).x;

                _SlowFollowCreatureScreenPos = saturate(_SlowFollowCreatureScreenPos);
                fixed gameplayShiftMask = lerp(gameplayShiftTex,shiftTex.y*.2,shiftTex.y*1.1);

                float2 tailDistortion =scale_from(i.uv.xy,fixed2(.5,.5),1-shiftTex.y*.1+saturate(shiftTex.y-.8)*.8);
                tailDistortion *=playerMask; 
#if GAMEPLAYRIPPLETEXTURE
                float2 sourceDistortion =scale_from(i.uv.xy,_SlowFollowCreatureScreenPos,1-max(shiftTex.x,gameplayShiftTex)*.5);
#else
                float2 sourceDistortion =scale_from(i.uv.xy,_SlowFollowCreatureScreenPos,1-max(shiftTex.x,smoothstep(.2,-.0,abs(gameplayShiftTex-.25))*.5-saturate(gameplayShiftTex-.2))*.5);
#endif
                float2 destDistortion =scale_from(i.uv.xy,_SlowFollowCreatureScreenPos,1-saturate(smoothstep(0.3,0,lerp(shiftTex.x,gameplayShiftTex*.25+saturate(gameplayShiftTex-.3),gameplayShiftTex>.2)))*.25);

                // vanilla:
                // fixed4 source = tex2D(_LevelTex,i.uv+sourceDistortion+tailDistortion);
                // fixed4 destination = tex2D(_MainTex,i.uv+destDistortion+tailDistortion);

                // modded:
                fixed4 source = tex2D(_LevelTex,i.uv+(sourceDistortion+tailDistortion)/distortion_scale);
                fixed4 destination = tex2D(_MainTex,i.uv+(destDistortion+tailDistortion)/distortion_scale);

#if GAMEPLAYRIPPLETEXTURE
                // vanilla:
                // fixed4 gameplayDest = tex2D(_GameplayRippleTexture,i.uv+destDistortion+tailDistortion);

                // modded:
                fixed4 gameplayDest = tex2D(_GameplayRippleTexture,i.uv+(destDistortion+tailDistortion)/distortion_scale);

                if (gameplayShiftMask>.2){
                    if (shiftTex.z>0.5&&get_depth(gameplayDest).x<5) return (.029);
                    return gameplayDest;
                }
#endif
                float grain = abs(fmod(tex2D(_UniNoise,i.grainCoord*fixed2(6,4)).x+_RAIN*4,1)-.5)*2;//grain to fuzzy out any other visible hard edges 
                if (shiftTex.x>.10+grain*.03){
                    float destDepth = get_depth(destination);
                    if (shiftTex.z>0.5&&destDepth<5) return (.029);
                    float mix = smoothstep(.1,.20-grain*.01,shiftTex.x);
                    destination.x +=-destDepth/255+trunc(lerp(get_depth(source),destDepth,mix))/255;// Interpolate depth to smooth out the transition
                    return destination;
                }
                return source;
            }
            ENDCG
        }
    }
}
