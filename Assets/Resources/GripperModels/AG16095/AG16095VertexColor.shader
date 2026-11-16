Shader "ARISE/Gripper Vertex Color"
{
    Properties
    {
        _Color ("Tint / ghost opacity", Color) = (1,1,1,1)
        _Mode ("Rendering mode", Float) = 0
        [Toggle] _GhostEffect ("Future state surface", Float) = 0
        _GhostTint ("Future state tint", Color) = (0.15,0.75,1,1)
        _GhostTintStrength ("Tint strength", Range(0,1)) = 0.65
        _GhostPreserveColor ("Preserve soft finger color", Range(0,1)) = 0.85
        _GhostInteriorOpacity ("Interior opacity multiplier", Range(0,1)) = 0.22
        _GhostRimPower ("Edge highlight power", Range(0.5,6)) = 2.4
        _GhostRimStrength ("Edge highlight brightness", Range(0,3)) = 1.1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        [Toggle] _ZWrite ("Depth write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            half _GhostEffect, _GhostTintStrength, _GhostPreserveColor;
            half _GhostInteriorOpacity, _GhostRimPower, _GhostRimStrength;
            fixed4 _GhostTint;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 world : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normal);
                float3 l = normalize(UnityWorldSpaceLightDir(i.world));
                float3 v = normalize(UnityWorldSpaceViewDir(i.world));
                float3 h = normalize(l + v);
                half metal = i.color.a;
                half diffuse = max(0, dot(n, l));
                half specular = pow(max(0, dot(n, h)), lerp(24, 90, metal));
                half3 albedo = i.color.rgb;
                // Palette values are authored from the photographs in sRGB.
                #ifndef UNITY_COLORSPACE_GAMMA
                    albedo = GammaToLinearSpace(albedo);
                #endif
                albedo *= _Color.rgb;
                half3 ambient = max(half3(0.10,0.10,0.10), ShadeSH9(half4(n,1)));
                half3 lit = albedo * (ambient + _LightColor0.rgb * diffuse * 0.85);
                lit += _LightColor0.rgb * specular * lerp(0.12,0.65,metal);
                half rim = pow(1 - saturate(abs(dot(n,v))), _GhostRimPower);
                half saturation = max(albedo.r,max(albedo.g,albedo.b)) - min(albedo.r,min(albedo.g,albedo.b));
                half tint = _GhostTintStrength * (1 - saturation * _GhostPreserveColor);
                half3 ghost = lerp(albedo, _GhostTint.rgb, tint) * (0.85 + 0.15 * diffuse);
                // Saturated silicone keeps its orange edge; neutral metal receives cyan.
                half3 edgeTint = lerp(_GhostTint.rgb, albedo, saturation * _GhostPreserveColor);
                ghost += edgeTint * rim * _GhostRimStrength;
                lit = lerp(lit, ghost, _GhostEffect);
                half opacity = _Color.a * lerp(1, lerp(_GhostInteriorOpacity, 1, rim), _GhostEffect);
                fixed4 color = fixed4(lit, opacity);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
