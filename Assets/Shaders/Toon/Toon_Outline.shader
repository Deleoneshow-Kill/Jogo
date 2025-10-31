Shader "Toon/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Thickness ("Thickness", Range(0,0.02)) = 0.005
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float _Thickness;
            float4 _Color;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 normalOS = normalize(IN.normalOS);
                float3 position = IN.positionOS.xyz + normalOS * _Thickness;
                OUT.positionCS = TransformObjectToHClip(float4(position, 1.0));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
