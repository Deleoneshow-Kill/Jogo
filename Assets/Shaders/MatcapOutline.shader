
Shader "CleanRPG/MatcapOutline"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MatCap("MatCap", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0,0.05)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                float3 pos = v.vertex.xyz + n * _OutlineWidth * max(0.25, length(v.vertex.xyz)*0.0);
                o.pos = UnityObjectToClipPos(float4(pos,1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Matcap pass
        Pass
        {
            Name "MATCAP"
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MatCap;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewNormal : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                o.viewNormal = n;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.viewNormal.xy * 0.5 + 0.5;
                fixed4 mc = tex2D(_MatCap, uv);
                return mc * _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
