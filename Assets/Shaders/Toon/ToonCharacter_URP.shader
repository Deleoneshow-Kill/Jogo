Shader "Toon/CharacterURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Base Map", 2D) = "white" {}
        _RampTex ("Light Ramp", 2D) = "gray" {}
        _RampBias ("Ramp Bias", Range(-0.5,0.5)) = 0
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0,2)) = 1
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecIntensity ("Specular Intensity", Range(0,2)) = 0.6
        _SpecSize ("Specular Size", Range(0.01,1)) = 0.2
        _SpecSteps ("Specular Steps", Range(1,6)) = 3
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimIntensity ("Rim Intensity", Range(0,2)) = 0.3
        _RimPower ("Rim Power", Range(0.1,8)) = 2
        _MatcapTex ("MatCap", 2D) = "gray" {}
        _MatcapIntensity ("MatCap Intensity", Range(0,2)) = 0.4
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionMask ("Emission Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampTex);        SAMPLER(sampler_RampTex);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MatcapTex);      SAMPLER(sampler_MatcapTex);
            TEXTURE2D(_EmissionMask);   SAMPLER(sampler_EmissionMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpecColor;
                float4 _RimColor;
                float4 _EmissionColor;
                float _RampBias;
                float _ShadowThreshold;
                float _NormalScale;
                float _SpecIntensity;
                float _SpecSize;
                float _SpecSteps;
                float _RimIntensity;
                float _RimPower;
                float _MatcapIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                half3 tangentWS   : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv         : TEXCOORD4;
                float2 lightmapUV : TEXCOORD5;
                half3 viewDirWS   : TEXCOORD6;
                float4 shadowCoord: TEXCOORD7;
                UNITY_FOG_COORDS(8)
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS  = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
                OUT.uv = IN.uv;
                OUT.lightmapUV = IN.lightmapUV;
                OUT.viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                UNITY_TRANSFER_FOG(OUT, OUT.positionCS);
                return OUT;
            }

            half3 SampleNormal(Varyings IN)
            {
                half3 normalWS = normalize(IN.normalWS);
                #ifdef _NORMALMAP
                    half3 nrmTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalScale);
                    half3x3 TBN = half3x3(normalize(IN.tangentWS), normalize(IN.bitangentWS), normalWS);
                    normalWS = normalize(mul(nrmTS, TBN));
                #endif
                return normalWS;
            }

            half3 EvaluateRamp(float nl)
            {
                float rampU = saturate(nl + _RampBias);
                return SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampU, 0.5)).rgb;
            }

            half3 EvaluateSpecular(half3 normalWS, half3 viewDirWS, half3 lightDirWS)
            {
                half3 halfDir = normalize(lightDirWS + viewDirWS);
                half nh = saturate(dot(normalWS, halfDir));
                half spec = pow(nh, max(_SpecSize, 0.0001));
                float stepCount = max(_SpecSteps, 1.0);
                spec = floor(spec * stepCount) / stepCount;
                return spec * _SpecColor.rgb * _SpecIntensity;
            }

            half3 EvaluateRim(half3 normalWS, half3 viewDirWS)
            {
                half rim = pow(saturate(1.0 - dot(normalWS, normalize(viewDirWS))), _RimPower);
                return rim * _RimColor.rgb * _RimIntensity;
            }

            half3 EvaluateMatcap(half3 normalWS, half3 viewDirWS)
            {
                half3x3 viewMatrix = (half3x3)UNITY_MATRIX_V;
                half3 normalVS = mul(viewMatrix, normalWS);
                half2 uv = normalVS.xy * 0.5 + 0.5;
                half3 matcap = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, uv).rgb;
                return matcap * _MatcapIntensity;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 viewDirWS = normalize(IN.viewDirWS);
                half3 normalWS = SampleNormal(IN);

                half4 albedoSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;
                half3 albedo = albedoSample.rgb;

                // Lighting
                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, normalWS, true);
                half nl = saturate(dot(normalWS, mainLight.direction));
                half3 ramp = EvaluateRamp(nl);

                half shadowMask = (nl < _ShadowThreshold) ? 0.0 : 1.0;
                half3 direct = albedo * ramp * mainLight.color.rgb * shadowMask;

                // Specular & rim
                half3 spec = EvaluateSpecular(normalWS, viewDirWS, mainLight.direction);
                half3 rim = EvaluateRim(normalWS, viewDirWS);
                half3 matcap = EvaluateMatcap(normalWS, viewDirWS);

                half3 color = direct + spec + rim + matcap;

                // Additional lights (add diffuse only to keep toon look subtle)
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS, 1.0);
                    half ndotl = saturate(dot(normalWS, light.direction));
                    half3 addRamp = EvaluateRamp(ndotl);
                    color += albedo * addRamp * light.color.rgb;
                }
                #endif

                // Global illumination
                half3 bakedGI = SampleSH(normalWS);
                color += albedo * bakedGI;

                // Emission
                half3 emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, IN.uv).rgb;
                color += emissionMask * _EmissionColor.rgb;

                // Fog
                half4 finalColor = half4(color, albedoSample.a);
                UNITY_APPLY_FOG(IN.fogCoord, finalColor);
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
