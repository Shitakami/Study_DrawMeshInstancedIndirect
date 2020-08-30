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

        float4x4 eulerAnglesToRotationMatrix(float3 angles) {

            float cx = cos(angles.x); float sx = sin(angles.x);
            float cy = cos(angles.z); float sy = sin(angles.z);
            float cz = cos(angles.y); float sz = sin(angles.y);

            return float4x4(
                cz*cy + sz*sx*sy, -cz*sy + sz*sx*cy, sz*cx, 0,
                sy*cx, cy*cx, -sx, 0,
                -sz*cy + cz*sx*sy, sy*sz + cz*sx*cy, cz*cx, 0,
                0, 0, 0, 1);

        }

        float4x4 CalcInverseMatrix(float3 position, float3 angle, float3 scale) {

            float4x4 inversScaleeMatrix = float4x4(
                1/scale.x, 0, 0, -position.x,
                0, 1/scale.y, 0, -position.y,
                0, 0, 1/scale.z, -position.z,
                0, 0, 0, 1);

            float4x4 mat = float4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);

            float4x4 rotMatrix = mul(eulerAnglesToRotationMatrix(angle), mat);

            float4x4 inverseRotMatrix = float4x4(
                rotMatrix._11, rotMatrix._21, rotMatrix._31, 0,
                rotMatrix._12, rotMatrix._22, rotMatrix._32, 0,
                rotMatrix._13, rotMatrix._23, rotMatrix._33, 0,
                0, 0, 0, 1);

            return mul(inversScaleeMatrix, inverseRotMatrix);

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

            // 速度から回転を求める
            float3 angle = float3(
                -asin(velocity.y/(length(velocity.xyz) + 1e-8)), // 0除算防止
                atan2(velocity.x, velocity.z),
                0);

            // 回転
            unity_ObjectToWorld = mul(eulerAnglesToRotationMatrix(angle), unity_ObjectToWorld);

            // 座標
            unity_ObjectToWorld._14_24_34_44 = float4(position, 1);

            // モデル行列を求める（間違っているかも. . .）
            // 参考:https://qiita.com/yuji_yasuhara/items/8d63455d1d277af4c270
            // 参考:http://gamemakerlearning.blog.fc2.com/blog-entry-196.html
            unity_WorldToObject = CalcInverseMatrix(position, angle, float3(_ScaleX, _ScaleY, _ScaleZ));


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
