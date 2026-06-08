Shader "VirtualVehicle/VehicleBodyWear"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Paint Albedo", 2D) = "white" {}
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
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        #define MAX_WEAR_STAMPS 32

        sampler2D _MainTex;
        sampler2D _WearMetalTex;
        sampler2D _WearMetalNormal;
        sampler2D _WearMetalRough;

        float4 _WearStamps[MAX_WEAR_STAMPS];
        float _WearStrengths[MAX_WEAR_STAMPS];

        fixed4 _Color;
        half _WearTiling;
        half _WearGrime;
        half _WearBlendPower;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float3 localPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPos = v.vertex.xyz;
        }

        half ComputeLocalWear(float3 localPos)
        {
            half wear = 0;

            for (int i = 0; i < MAX_WEAR_STAMPS; i++)
            {
                float4 stamp = _WearStamps[i];
                if (stamp.w <= 0.0001)
                    continue;

                float dist = distance(localPos, stamp.xyz);
                half falloff = 1.0h - saturate(dist / stamp.w);
                wear = max(wear, falloff * falloff * _WearStrengths[i]);
            }

            return wear;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 paint = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half wear = ComputeLocalWear(IN.localPos);
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
