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
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float4 positionVS:TEXCOORD3; UNITY_FOG_COORDS(4) };
            Varyings vert(Attributes v){
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(v.normalOS);
                o.positionHCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=NormalizeNormalPerPixel(n.normalWS); o.uv=v.uv;
                o.positionVS=mul(UNITY_MATRIX_V, float4(o.positionWS,1)); UNITY_TRANSFER_FOG(o,o.positionHCS); return o;
            }
            float3 sampleRamp(float x){ float2 uv=float2(saturate(x),0.5); return SAMPLE_TEXTURE2D(_RampTex,sampler_RampTex,uv).rgb; }
            float3 sampleMatcap(float3 nWS,float4 posVS){ float3 nVS=normalize(mul((float3x3)UNITY_MATRIX_V,nWS)); float2 uv=nVS.xy*0.5+0.5; float3 mc=SAMPLE_TEXTURE2D(_MatCapTex,sampler_MatCapTex,float2(uv.x,1-uv.y)).rgb; return mc; }
            half4 frag(Varyings i):SV_Target{
                float3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                Light Lm=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 N=normalize(i.normalWS); float3 L=normalize(Lm.direction); float3 V=normalize(_WorldSpaceCameraPos-i.positionWS);
                float rampX=0.5*dot(N,-L)+0.5; float3 ramp=sampleRamp(rampX);
                float3 lit=lerp(_ShadowColor.rgb, albedo, ramp.r);
                float3 matcap=sampleMatcap(N,i.positionVS); lit=lerp(lit, lit*matcap, _MatCapIntensity);
                float rim=pow(saturate(1-dot(N,V)), _RimPower); lit+=_RimColor.rgb*rim*0.35; lit*=Lm.color.rgb;
                half4 col=half4(lit, _BaseColor.a); UNITY_APPLY_FOG(i.fogCoord,col); return col;
            }
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _OutlineColor; float _OutlineWidth; CBUFFER_END
            struct A{float4 positionOS:POSITION; float3 normalOS:NORMAL;};
            struct V{float4 positionHCS:SV_POSITION;};
            V vert(A v){
                V o; float3 nWS=TransformObjectToWorldNormal(v.normalOS);
                float3 posWS=TransformObjectToWorld(v.positionOS.xyz);
                float3 posVS=TransformWorldToView(posWS);
                float3 nVS=normalize(mul((float3x3)UNITY_MATRIX_V,nWS));
                posVS.xy += nVS.xy * _OutlineWidth * max(1, abs(posVS.z)*0.5);
                o.positionHCS = TransformWViewToHClip(float4(posVS,1)); return o;
            }
            half4 frag(V i):SV_Target{ return _OutlineColor; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
