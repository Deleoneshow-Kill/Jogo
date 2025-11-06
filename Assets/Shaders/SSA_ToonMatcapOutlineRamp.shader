Shader "SSA/ToonMatcapOutlineRamp"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _RampTex ("Ramp (1xN)", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.25,0.25,0.3,1)

        _MatCapTex ("MatCap", 2D) = "gray" {}
        _MatCapIntensity ("MatCap Intensity", Range(0,2)) = 0.5

        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1,8)) = 2.0

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (world)", Range(0,0.02)) = 0.0025
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        // ---------- OUTLINE PASS ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                float3 nWS = TransformObjectToWorldNormal(v.normal);
                float3 posWS = TransformObjectToWorld(v.vertex.xyz);
                posWS += nWS * _OutlineWidth;
                o.posCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ---------- FORWARD PASS ----------
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_MatCapTex); SAMPLER(sampler_MatCapTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float3 nWS   : TEXCOORD1;
                float2 uv    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _ShadowColor;
                float _MatCapIntensity;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.posWS = TransformObjectToWorld(v.vertex.xyz);
                o.nWS   = TransformObjectToWorldNormal(v.normal);
                o.posCS = TransformWorldToHClip(o.posWS);
                o.uv    = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.posWS));

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(n, -mainLight.direction));

                float rampU = NdotL;
                float ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampU, 0.5)).r;

                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _Color.rgb;

                float3 toon = lerp(_ShadowColor.rgb, albedo, ramp);

                float3 nVS = normalize(mul((float3x3)UNITY_MATRIX_V, n));
                float2 mcUV = nVS.xy * 0.5 + 0.5;
                float3 mc = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, mcUV).rgb;
                toon += mc * _MatCapIntensity;

                float rim = pow(1.0 - saturate(dot(n, V)), _RimPower);
                toon += _RimColor.rgb * rim;

                return half4(toon, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
