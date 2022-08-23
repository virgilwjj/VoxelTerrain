Shader "Custom/Triplanar"
{
    Properties
    {
        _ZY_Tex("ZY_Texture", 2D) = "white" {}
        _XZ_Tex("XZ_Texture", 2D) = "white" {}
        _XY_Tex("XY_Texture", 2D) = "white" {}
        _BlendOffset("BlendOffset", Range(0.0,0.5)) = 0.25
        _BlendExponent("Blend Exponent", Range(1.0, 8.0)) = 2.0
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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float4 worldpos : TEXCOORD2;
            };

            fixed _BlendOffset;
            half _BlendExponent;
            sampler2D _ZY_Tex, _XZ_Tex, _XY_Tex;
            float4 _ZY_Tex_ST, _XZ_Tex_ST, _XY_Tex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 colx = tex2D(_ZY_Tex, i.worldpos.zy * _ZY_Tex_ST.xy + _ZY_Tex_ST.zw);
                fixed4 coly = tex2D(_XZ_Tex, i.worldpos.xz * _XZ_Tex_ST.xy + _XZ_Tex_ST.zw);
                fixed4 colz = tex2D(_XY_Tex, i.worldpos.xy * _XY_Tex_ST.xy + _XY_Tex_ST.zw);

                fixed3 weights = abs(i.normal);
                weights = saturate(weights - _BlendOffset);
                weights = pow(weights, _BlendExponent);
                weights /= (weights.x + weights.y + weights.z);

                fixed4 tricol = colx * weights.x + coly * weights.y + colz * weights.z;
                return tricol;
            }
            ENDCG
        }
    }
}