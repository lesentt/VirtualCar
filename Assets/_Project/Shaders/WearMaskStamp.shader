Shader "Hidden/VirtualVehicle/WearMaskStamp"
{
    Properties
    {
        _MainTex ("Wear Mask", 2D) = "black" {}
        _StampUv ("Stamp UV", Vector) = (0.5, 0.5, 0, 0)
        _StampRadius ("Stamp Radius", Float) = 0.12
        _Strength ("Strength", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _StampUv;
            float _StampRadius;
            float _Strength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed existing = tex2D(_MainTex, i.uv).r;

                float2 delta = i.uv - _StampUv.xy;
                delta.x *= _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0);
                float dist = length(delta);
                float falloff = 1.0 - saturate(dist / max(_StampRadius, 0.0001));
                falloff = falloff * falloff;

                fixed stamp = saturate(_Strength * falloff);
                return fixed4(max(existing, stamp), existing, existing, 1);
            }
            ENDCG
        }
    }
}
