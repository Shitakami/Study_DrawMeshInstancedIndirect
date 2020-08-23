Shader "Custom/TestInstancedIndirectSurf"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    StructuredBuffer<float4> positionBuffer;
    StructuredBuffer<float3> eulerAngleBuffer;
#endif

        #define Deg2Rad 0.0174532924

        float4x4 eulerAnglesToRottationMatrix(float3 angles) {

            float cx = cos(angles.x * Deg2Rad); float sx = sin(angles.x * Deg2Rad);
            float cy = cos(angles.z * Deg2Rad); float sy = sin(angles.z * Deg2Rad);
            float cz = cos(angles.y * Deg2Rad); float sz = sin(angles.y * Deg2Rad);

            return float4x4(
                cz*cy + sz*sx*sy, -cz*sy + sz*sx*cy, sz*cx, 0,
                sy*cx, cy*cx, -sx, 0,
                -sz*cy + cz*sx*sy, sy*sz + cz*sx*cy, cz*cx, 0,
                0, 0, 0, 1);


        }


        void setup() {

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4 data = positionBuffer[unity_InstanceID];
            float3 angles = eulerAngleBuffer[unity_InstanceID];

            // スケーリング
            unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);

            // 回転
            unity_ObjectToWorld = mul(eulerAnglesToRottationMatrix(angles), unity_ObjectToWorld);

            // 座標
            unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);

            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
        #endif

        }


        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 cy = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = cy.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = cy.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
