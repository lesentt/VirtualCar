Shader "Bitgem/BuiltIn Stylised Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.09, 0.29, 0.54, 1)
        _ShallowColor ("Shallow Color", Color) = (0.10, 0.54, 0.66, 0.65)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 1.5
        _WaveScale ("Wave Scale", Range(0.01, 1)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0.1, 8)) = 1.75
        _Glossiness ("Glossiness", Range(0, 1)) = 0.75
        _Metallic ("Metallic", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        LOD 200
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0

        sampler2D _NormalMap;
        fixed4 _DeepColor;
        fixed4 _ShallowColor;
        half _WaveSpeed;
        half _WaveScale;
        half _WaveFrequency;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_NormalMap;
            float3 worldPos;
            float3 viewDir;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_NormalMap = v.texcoord.xy;
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uvScale = _WaveScale * _WaveFrequency;
            float t = _Time.y * _WaveSpeed;
            float2 uv1 = IN.uv_NormalMap * uvScale + float2(t, t * 0.7);
            float2 uv2 = IN.uv_NormalMap * uvScale * 1.3 + float2(-t * 0.8, t * 0.5);

            fixed3 n1 = UnpackNormal(tex2D(_NormalMap, uv1));
            fixed3 n2 = UnpackNormal(tex2D(_NormalMap, uv2));
            fixed3 normal = normalize(fixed3(n1.xy + n2.xy, n1.z * n2.z));

            half fresnel = saturate(1.0 - dot(normalize(IN.viewDir), fixed3(0, 1, 0)));
            fixed4 waterColor = lerp(_DeepColor, _ShallowColor, fresnel);

            o.Albedo = waterColor.rgb;
            o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = waterColor.a;
        }
        ENDCG
    }

    Fallback "Transparent/Diffuse"
}
