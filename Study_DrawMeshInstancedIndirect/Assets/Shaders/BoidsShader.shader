Shader "Custom/BoidsShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _ScaleX("ScaleX", float) = 1
        _ScaleY("ScaleY", float) = 1
        _ScaleZ("ScaleZ", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows // 影を描画するためにはaddshadowが必要
        #pragma multi_compile_instancing    // GPU Instancingを可能にする
        #pragma instancing_options procedural:setup // setup関数を呼び出す


        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        struct BoidsData {
            float3 position;
            float3 velocity;
        };

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<BoidsData> boidsDataBuffer;
        #endif

        float4x4 eulerAnglesToRottationMatrix(float3 angles) {

            float cx = cos(angles.x); float sx = sin(angles.x);
            float cy = cos(angles.z); float sy = sin(angles.z);
            float cz = cos(angles.y); float sz = sin(angles.y);

            return float4x4(
                cz*cy + sz*sx*sy, -cz*sy + sz*sx*cy, sz*cx, 0,
                sy*cx, cy*cx, -sx, 0,
                -sz*cy + cz*sx*sy, sy*sz + cz*sx*cy, cz*cx, 0,
                0, 0, 0, 1);

        }

        fixed _ScaleX;
        fixed _ScaleY;
        fixed _ScaleZ;

        void setup() {

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float3 position = boidsDataBuffer[unity_InstanceID].position;
            float3 velocity = boidsDataBuffer[unity_InstanceID].velocity;

            // スケーリング
            unity_ObjectToWorld._11_21_31_41 = float4(_ScaleX, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, _ScaleY, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, _ScaleZ, 0);

            float3 angle = float3(
                -asin(velocity.y/length(velocity.xyz)),
                atan2(velocity.x, velocity.z),
                0);

            // 回転
            unity_ObjectToWorld = mul(eulerAnglesToRottationMatrix(angle), unity_ObjectToWorld);

            // 座標
            unity_ObjectToWorld._14_24_34_44 = float4(position, 1);

            // モデル行列を求める（間違っているかも. . .）
            // 参考:https://qiita.com/yuji_yasuhara/items/8d63455d1d277af4c270
            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_12_13 = unity_ObjectToWorld._11_21_31;
            unity_WorldToObject._21_22_23 = unity_ObjectToWorld._12_22_32;
            unity_WorldToObject._31_32_33 = unity_ObjectToWorld._13_23_33;
            unity_WorldToObject._11_12_13 = normalize(unity_WorldToObject._11_12_13);
            unity_WorldToObject._21_22_23 = normalize(unity_WorldToObject._21_22_23);
            unity_WorldToObject._31_32_33 = normalize(unity_WorldToObject._31_32_33);
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
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
