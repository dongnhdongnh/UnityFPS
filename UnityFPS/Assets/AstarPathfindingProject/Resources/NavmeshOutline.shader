Shader "Hidden/AstarPathfindingProject/Navmesh Outline" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,0.5)
		_FadeColor ("Fade Color", Color) = (1,1,1,0.3)
		_PixelWidth ("Width (px)", Float) = 4
        _LengthPadding ("Length Padding (px)", Float) = 0
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Offset -3, -50
        Tags { "IgnoreProjector"="True" "RenderType"="Overlay" }
        
        // Render behind objects
        Pass {
            ZTest Greater
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Navmesh.cginc"

            float4 _Color;
            float4 _FadeColor;
            float _PixelWidth;
            float _LengthPadding;
            
            static const float FalloffTextureScreenPixels = 2;
            
            line_v2f vert (appdata_color v, out float4 outpos : SV_POSITION) {
                line_v2f o = line_vert(v, _PixelWidth, _LengthPadding, outpos);
                o.col = v.color * _Color * _FadeColor;
                if (!IsGammaSpaceCompatibility()) {
                    o.col.rgb = GammaToLinearSpace(o.col.rgb);
                }
                return o;
            }

            half4 frag (line_v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : COLOR {
                float2 p = (i.screenPos.xy/i.screenPos.w) - (i.originScreenPos.xy / i.originScreenPos.w);
                // Handle DirectX properly. See https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                p.y *= _ProjectionParams.x;
                float dist = dot(p*_ScreenParams.xy, i.normal) / _PixelWidth;
                float FalloffFractionOfWidth = FalloffTextureScreenPixels/(_PixelWidth*0.5);
                float a = lineAA((abs(dist*4) - (1 - FalloffFractionOfWidth))/FalloffFractionOfWidth);
                return i.col * float4(1,1,1,a);
            }
            ENDCG
        }

        // First pass writes to the Z buffer
        // where the lines have a pretty high opacity
        Pass {
            ZTest LEqual
            ZWrite On
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Navmesh.cginc"
            
            float _PixelWidth;
            float _LengthPadding;
            
            // Number of screen pixels that the _Falloff texture corresponds to
            static const float FalloffTextureScreenPixels = 2;
            
            line_v2f vert (appdata_color v, out float4 outpos : SV_POSITION) {
                line_v2f o = line_vert(v, _PixelWidth, _LengthPadding, outpos);
                return o;
            }

            half4 frag (line_v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : COLOR {
                float2 p = (i.screenPos.xy/i.screenPos.w) - (i.originScreenPos.xy / i.originScreenPos.w);
                // Handle DirectX properly. See https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                p.y *= _ProjectionParams.x;
                float dist = dot(p*_ScreenParams.xy, i.normal) / _PixelWidth;
                float FalloffFractionOfWidth = FalloffTextureScreenPixels/(_PixelWidth*0.5);
                float a = lineAA((abs(dist*4) - (1 - FalloffFractionOfWidth))/FalloffFractionOfWidth);
                if (a < 0.7) discard;
                return float4(1,1,1,a);
            }
            ENDCG
        }
        
        // Render in front of objects
		Pass {
			ZTest LEqual
            
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Navmesh.cginc"
			
			float4 _Color;
			float _PixelWidth;
			float _LengthPadding;
            
			// Number of screen pixels that the _Falloff texture corresponds to
			static const float FalloffTextureScreenPixels = 2;
			
			line_v2f vert (appdata_color v, out float4 outpos : SV_POSITION) {
				line_v2f o = line_vert(v, _PixelWidth, _LengthPadding, outpos);
				o.col = v.color * _Color;
				if (!IsGammaSpaceCompatibility()) {
					o.col.rgb = GammaToLinearSpace(o.col.rgb);
				}
				return o;
			}

			half4 frag (line_v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : COLOR {
				float2 p = (i.screenPos.xy/i.screenPos.w) - (i.originScreenPos.xy / i.originScreenPos.w);
				// Handle DirectX properly. See https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				p.y *= _ProjectionParams.x;
				float dist = dot(p*_ScreenParams.xy, i.normal) / _PixelWidth;
				float FalloffFractionOfWidth = FalloffTextureScreenPixels/(_PixelWidth*0.5);
				float a = lineAA((abs(dist*4) - (1 - FalloffFractionOfWidth))/FalloffFractionOfWidth);
				return i.col * float4(1,1,1,a);
			}
			ENDCG
		}
	}
	Fallback Off
}
