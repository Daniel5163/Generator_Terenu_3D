Shader "Custom/HeightColorShader"
{
    Properties
    {
        _LowColor ("Low Color", Color) = (0.5, 0.2, 0.1, 1)   
        _MidColor ("Mid Color", Color) = (0.3, 0.7, 0.3, 1)  
        _HighColor ("High Color", Color) = (0.9, 0.9, 0.9, 1)  

        _MinHeight ("Min Height", Float) = 0                    
        _MaxHeight ("Max Height", Float) = 50                   

        _LightDirection ("Light Direction", Vector) = (1, 1, 1) 
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            float4 _LowColor;
            float4 _MidColor;
            float4 _HighColor;
            float _MinHeight;
            float _MaxHeight;
            float3 _LightDirection;

            // Oblicza pozycję i wysokość wierzchołka
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float normalizedHeight = (v.vertex.y - _MinHeight) / (_MaxHeight - _MinHeight);
                o.height = saturate(normalizedHeight);
                o.normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                return o;
            }

            // Dobiera kolor w zależności od wysokości i oświetlenia
            fixed4 frag (v2f i) : SV_Target
            {
                float4 baseColor;

                if (i.height < 0.33)
                    baseColor = _LowColor;
                else if (i.height < 0.66)
                    baseColor = _MidColor;
                else
                    baseColor = _HighColor;

                float lightIntensity = max(0, dot(normalize(i.normal), normalize(_LightDirection)));
                return baseColor * lightIntensity;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
