Shader "VirtualVehicle/VehicleBodyWear"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Paint Albedo", 2D) = "white" {}
        _WearMask ("Wear Mask", 2D) = "black" {}
        _MetalLightTex ("Light Metal", 2D) = "gray" {}
        _MetalHeavyTex ("Heavy Metal", 2D) = "gray" {}
        _MetalLightNormal ("Light Normal", 2D) = "bump" {}
        _MetalHeavyNormal ("Heavy Normal", 2D) = "bump" {}
        _MetalLightRough ("Light Roughness", 2D) = "white" {}
        _MetalHeavyRough ("Heavy Roughness", 2D) = "white" {}
        _WearTiling ("Metal Tiling", Float) = 4
        _WearGrime ("Grime", Range(0, 1)) = 0.35
        _Glossiness ("Paint Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Paint Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _WearMask;
        sampler2D _MetalLightTex;
        sampler2D _MetalHeavyTex;
        sampler2D _MetalLightNormal;
        sampler2D _MetalHeavyNormal;
        sampler2D _MetalLightRough;
        sampler2D _MetalHeavyRough;

        fixed4 _Color;
        half _WearTiling;
        half _WearGrime;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 paint = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half wear = tex2D(_WearMask, IN.uv_MainTex).r;
            half wearCurve = saturate(wear * 1.15);

            float2 metalUv = IN.uv_MainTex * _WearTiling;
            fixed4 metalLight = tex2D(_MetalLightTex, metalUv);
            fixed4 metalHeavy = tex2D(_MetalHeavyTex, metalUv);
            fixed4 metal = lerp(metalLight, metalHeavy, wearCurve);
            metal.rgb *= 1.0h - _WearGrime * wearCurve * wearCurve;

            o.Albedo = lerp(paint.rgb, metal.rgb, wearCurve);

            fixed3 paintNormal = fixed3(0, 0, 1);
            fixed3 nLight = UnpackNormal(tex2D(_MetalLightNormal, metalUv));
            fixed3 nHeavy = UnpackNormal(tex2D(_MetalHeavyNormal, metalUv));
            fixed3 metalNormal = normalize(lerp(nLight, nHeavy, wearCurve));
            o.Normal = lerp(paintNormal, metalNormal, wearCurve);

            half roughLight = tex2D(_MetalLightRough, metalUv).r;
            half roughHeavy = tex2D(_MetalHeavyRough, metalUv).r;
            half metalSmooth = 1.0h - lerp(roughLight, roughHeavy, wearCurve);

            o.Metallic = lerp(_Metallic, 0.92h, wearCurve);
            o.Smoothness = lerp(_Glossiness, metalSmooth * 0.55h, wearCurve);
        }
        ENDCG
    }

    Fallback "Standard"
}
