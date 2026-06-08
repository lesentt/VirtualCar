Shader "VirtualVehicle/VehicleBodyWear"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Paint Albedo", 2D) = "white" {}
        _WearMask ("Wear Mask", 2D) = "black" {}
        _WearMetalTex ("Wear Metal", 2D) = "gray" {}
        _WearMetalNormal ("Wear Normal", 2D) = "bump" {}
        _WearMetalRough ("Wear Roughness", 2D) = "white" {}
        _WearTiling ("Metal Tiling", Float) = 5
        _WearGrime ("Grime", Range(0, 1)) = 0.45
        _WearBlendPower ("Wear Blend Power", Float) = 2.2
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
        sampler2D _WearMetalTex;
        sampler2D _WearMetalNormal;
        sampler2D _WearMetalRough;

        fixed4 _Color;
        half _WearTiling;
        half _WearGrime;
        half _WearBlendPower;
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
            half wearCurve = saturate(pow(wear, 0.85) * _WearBlendPower);

            float2 metalUv = IN.uv_MainTex * _WearTiling;
            fixed4 metal = tex2D(_WearMetalTex, metalUv);
            metal.rgb *= 1.0h - _WearGrime * wearCurve;

            o.Albedo = lerp(paint.rgb, metal.rgb, wearCurve);

            fixed3 paintNormal = fixed3(0, 0, 1);
            fixed3 metalNormal = UnpackNormal(tex2D(_WearMetalNormal, metalUv));
            o.Normal = lerp(paintNormal, metalNormal, wearCurve);

            half rough = tex2D(_WearMetalRough, metalUv).r;
            half metalSmooth = (1.0h - rough) * 0.5h;

            o.Metallic = lerp(_Metallic, 0.95h, wearCurve);
            o.Smoothness = lerp(_Glossiness, metalSmooth, wearCurve);
        }
        ENDCG
    }

    Fallback "Standard"
}
