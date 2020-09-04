using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

public class TestGPGPUParticle : MonoBehaviour {

    #region DrawMeshInstancedIndirect_Parameters
    [Header ("DrawMeshInstancedIndirectのパラメータ")]
    [SerializeField]
    private Mesh m_mesh;

    [SerializeField]
    private Material m_material;

    [SerializeField]
    private Bounds m_bounds;

    [SerializeField]
    private ShadowCastingMode m_shadowCastingMode;

    [SerializeField]
    private bool m_receiveShadows;
    #endregion

    #region GPUParticle_Parameters
    [Header ("GPUパーティクルのパラメータ")]
    [Space (20)]
    [SerializeField]
    private int m_instanceCount;

    [SerializeField]
    private int m_emitCount;

    [SerializeField]
    private float m_lifeTime;
    [SerializeField]
    private float m_force;
    [SerializeField]
    private float m_forceAngle;
    [SerializeField]
    private float m_gravity;
    [SerializeField]
    private float m_scale;
    #endregion

    #region ComputeShader_Parameters
    private ComputeBuffer m_argsBuffer;

    [SerializeField]
    private ComputeShader m_gpuParticleCalculator;

    private ComputeBuffer m_particlesBuffer;

    private ComputeBuffer m_particlePoolBuffer;

    private ComputeBuffer m_particlePoolCountBuffer;
    private int[] m_poolCount = { 0, 0, 0, 0 };

    private int m_updateKernel, m_emitKernel;

    private Vector3Int m_updateGroupSize, m_emitGroupSize;
    #endregion

    #region Struct_Particle
    private struct Particle {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 angle;
        public float duration;
        public float scale;
        public bool isActive;
    }
    #endregion

    // Start is called before the first frame update
    void Start () {

        InitializeArgsBuffer ();
        InitializeGPUParticle ();

    }

    // Update is called once per frame
    void Update () {

        m_gpuParticleCalculator.SetFloat ("Time", Time.time);
        m_gpuParticleCalculator.SetFloat ("deltaTime", Time.deltaTime);

        if (Input.GetMouseButton (0)) {
            EmitParticles ();
        }

        UpdateParticles ();

    }

    void LateUpdate () {

        Graphics.DrawMeshInstancedIndirect (
            m_mesh,
            0,
            m_material,
            m_bounds,
            m_argsBuffer,
            0,
            null,
            m_shadowCastingMode,
            m_receiveShadows
        );

    }

    private void EmitParticles () {

        int poolCount = GetParticlePoolCount ();

        // 未使用のパーティクルが一度に生成する個数より低い場合は
        // パーティクルを生成しない
        if (poolCount < m_emitCount)
            return;

        m_gpuParticleCalculator.Dispatch (m_emitKernel, m_emitGroupSize.x, m_emitGroupSize.y, m_emitGroupSize.z);

    }

    private int GetParticlePoolCount () {

        // ComputeShader内のDeadListに入っているデータの個数を取得する
        ComputeBuffer.CopyCount (m_particlePoolBuffer, m_particlePoolCountBuffer, 0);
        m_particlePoolCountBuffer.GetData (m_poolCount);
        int restPool = m_poolCount[0];

        Debug.Log ("restPool = " + restPool);

        return restPool;

    }

    private void UpdateParticles () {

        m_gpuParticleCalculator.Dispatch (m_updateKernel, m_updateGroupSize.x, m_updateGroupSize.y, m_updateGroupSize.z);

    }

    private void InitializeArgsBuffer () {

        Assert.IsNotNull (m_mesh, "メッシュが設定されていません");
        Assert.IsNotNull (m_material, "マテリアルが設定されていません");

        var args = new uint[] { 0, 0, 0, 0, 0 };

        args[0] = m_mesh.GetIndexCount (0);
        args[1] = (uint) m_instanceCount;

        m_argsBuffer = new ComputeBuffer (1, 4 * args.Length, ComputeBufferType.IndirectArguments);

        m_argsBuffer.SetData (args);

    }

    private void InitializeGPUParticle () {

        Assert.IsFalse (m_instanceCount < m_emitCount, "一度に出すパーティクルの個数がパーティクルの総数以上になっています");

        // インスタンスの個数、一度に出すパーティクルの個数は2の累乗に設定（計算しやすくするため）
        m_instanceCount = Mathf.ClosestPowerOfTwo (m_instanceCount);
        m_emitCount = Mathf.ClosestPowerOfTwo (m_emitCount);

        InitializeComputeBuffers ();

        InitializeParticlePool ();

        m_gpuParticleCalculator.SetFloat ("lifeTime", m_lifeTime);
        m_gpuParticleCalculator.SetFloat ("force", m_force);
        m_gpuParticleCalculator.SetFloat ("forceAngle", m_forceAngle);
        m_gpuParticleCalculator.SetFloat ("scale", m_scale);
        m_gpuParticleCalculator.SetFloat ("gravity", m_gravity);

        m_updateKernel = m_gpuParticleCalculator.FindKernel ("UpdateParticles");

        uint x, y, z;
        m_gpuParticleCalculator.GetKernelThreadGroupSizes (m_updateKernel, out x, out y, out z);
        m_updateGroupSize = new Vector3Int (m_instanceCount / (int) x, (int) y, (int) z);
        m_gpuParticleCalculator.SetBuffer (m_updateKernel, "particles", m_particlesBuffer);
        m_gpuParticleCalculator.SetBuffer (m_updateKernel, "deadList", m_particlePoolBuffer);

        m_emitKernel = m_gpuParticleCalculator.FindKernel ("EmitParticles");

        m_gpuParticleCalculator.GetKernelThreadGroupSizes (m_emitKernel, out x, out y, out z);
        m_emitGroupSize = new Vector3Int (m_emitCount / (int) x, (int) y, (int) z);
        m_gpuParticleCalculator.SetBuffer (m_emitKernel, "particles", m_particlesBuffer);
        m_gpuParticleCalculator.SetBuffer (m_emitKernel, "particlePool", m_particlePoolBuffer);

    }

    private void InitializeComputeBuffers () {

        m_particlesBuffer = new ComputeBuffer (m_instanceCount, Marshal.SizeOf (typeof (Particle)));
        m_material.SetBuffer ("_ParticleBuffer", m_particlesBuffer);

        m_particlePoolBuffer = new ComputeBuffer (m_instanceCount, Marshal.SizeOf (typeof (int)), ComputeBufferType.Append);
        // Append/Consumeの追加削除位置を0に設定する
        m_particlePoolBuffer.SetCounterValue (0);

        // パーティクルの個数を求める際に使用する
        m_particlePoolCountBuffer = new ComputeBuffer (4, Marshal.SizeOf (typeof (int)), ComputeBufferType.IndirectArguments);
        m_particlePoolCountBuffer.SetData (m_poolCount);

    }

    private void InitializeParticlePool () {

        int initializeParticlesKernel = m_gpuParticleCalculator.FindKernel ("InitializeParticles");
        m_gpuParticleCalculator.GetKernelThreadGroupSizes (initializeParticlesKernel, out uint x, out uint y, out uint z);
        m_gpuParticleCalculator.SetBuffer (initializeParticlesKernel, "deadList", m_particlePoolBuffer);
        m_gpuParticleCalculator.SetBuffer (initializeParticlesKernel, "particles", m_particlesBuffer);
        m_gpuParticleCalculator.Dispatch (initializeParticlesKernel, m_instanceCount / (int) x, 1, 1);

    }

    private void OnDisable () {

        m_particlesBuffer?.Release ();
        m_particlePoolBuffer = null;

        m_particlePoolBuffer?.Release ();
        m_particlePoolBuffer = null;

        m_particlePoolCountBuffer?.Release ();
        m_particlePoolCountBuffer = null;

        m_argsBuffer?.Release ();
        m_argsBuffer = null;

    }

}