Shader "SSA/ToonMatcapOutlineRamp"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _RampTex ("Ramp", 2D) = "gray" {}
        _ShadowColor ("Shadow Color", Color) = (0.28,0.32,0.45,1)
        _MatCapTex ("MatCap", 2D) = "white" {}
        _MatCapIntensity ("MatCap Intensity", Range(0,1)) = 0.55
        _RimColor ("Rim Color", Color) = (0.75,0.85,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2.2
        _OutlineWidth ("Outline Width (world)", Range(0,0.02)) = 0.0018
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_MatCapTex); SAMPLER(sampler_MatCapTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _RimColor;
                float _MatCapIntensity;
                float _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 positionVS   : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nInputs = GetVertexNormalInputs(v.normalOS);
                o.positionHCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = NormalizeNormalPerPixel(nInputs.normalWS);
                o.uv = v.uv;
                float4 positionVS = mul(UNITY_MATRIX_V, float4(o.positionWS,1));
                o.positionVS = positionVS;
                UNITY_TRANSFER_FOG(o,o.positionHCS);
                return o;
            }

            float3 sampleRamp(float x)
            {
                float2 uv = float2(saturate(x), 0.5);
                float4 c = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, uv);
                return c.rgb;
            }

            float3 sampleMatcap(float3 normalWS, float4 positionVS)
            {
                // view-space normal for matcap lookup
                float3 nVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                float2 uv = nVS.xy * 0.5 + 0.5;
                float3 mc = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, float2(uv.x, 1-uv.y)).rgb;
                return mc;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                // Main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);

                // toon ramp
                float NdotL = dot(N, -L); // main light dir points from light to surface
                float rampX = 0.5 * NdotL + 0.5;
                float3 ramp = sampleRamp(rampX);
                float3 lit = lerp(_ShadowColor.rgb, albedo, ramp.r); // use ramp as mask between shadowcolor and albedo

                // matcap
                float3 matcap = sampleMatcap(N, i.positionVS);
                lit = lerp(lit, lit * matcap, _MatCapIntensity);

                // rim
                float rim = pow(saturate(1 - dot(N, V)), _RimPower);
                lit += _RimColor.rgb * rim * 0.35;

                // apply main light color & intensity
                lit *= mainLight.color.rgb;

                half4 col = half4(lit, _BaseColor.a * baseTex.a);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDHLSL
        }

        // Outline pass (front-face culled, normal extrusion in view space)
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                // approximate width scaling by distance (view space)
                float3 posVS = TransformWorldToView(posWS);
                float3 nVS = normalize(mul((float3x3)UNITY_MATRIX_V, nWS));
                posVS.xy += nVS.xy * _OutlineWidth * max(1, abs(posVS.z)*0.5);
                float4 posCS = TransformWViewToHClip(float4(posVS,1));
                o.positionHCS = posCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
