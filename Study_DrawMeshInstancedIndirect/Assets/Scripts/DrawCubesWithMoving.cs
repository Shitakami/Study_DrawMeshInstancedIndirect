using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;   // Marshal.Sizeofの呼び出しに必要

public class DrawCubesWithMoving : MonoBehaviour
{

    [Header("DrawMeshInstancedIndirectのパラメータ")]
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

    [Space(20)]
    [SerializeField]
    private int m_instanceCount;

    [Space(20)]
    [SerializeField]
    private ComputeShader m_computeShader;

    [SerializeField]
    private float m_moveSpeed;
    [SerializeField]
    private float m_rotateSpeed;

    [SerializeField]
    private float m_moveHeight;

    private int m_kernelID;

    private Vector3Int m_groupSize;

    private ComputeBuffer m_argsBuffer;

    private ComputeBuffer m_cubeParamBuffer;

    struct CubeParameter {
        public Vector3 position;
        public Vector3 angle;
        public float scale;
        public float randTime;
        public float baseHeight;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 描画するメッシュの個数を最も近い2の累乗の値する
        m_instanceCount = Mathf.ClosestPowerOfTwo(m_instanceCount);

        InitializeArgsBuffer();
        InitializeCubeParamBuffer();
        InitializeComputeShader();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePositionAndAngle();
    }

    void LateUpdate() {

        Graphics.DrawMeshInstancedIndirect(
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

    private void InitializeArgsBuffer() {

        uint[] args = { 0, 0, 0, 0, 0};

        args[0] = (m_mesh != null) ? m_mesh.GetIndexCount(0) : 0;
        args[1] = (uint)m_instanceCount;

        m_argsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);

    }

    private void InitializeComputeShader() {

        // カーネルIDの取得
        m_kernelID = m_computeShader.FindKernel("ChangeCubeParameter");

        // グループサイズを求める
        m_computeShader.GetKernelThreadGroupSizes(m_kernelID, out uint x, out uint y, out uint z);
        m_groupSize = new Vector3Int((int)x, (int)y, (int)z);
        m_groupSize.x = m_instanceCount / m_groupSize.x;

        // パラメータをComputeShaderに設定
        m_computeShader.SetBuffer(m_kernelID, "cubeParamBuffer", m_cubeParamBuffer);
        m_computeShader.SetFloat("moveSpeed", m_moveSpeed);
        m_computeShader.SetFloat("rotateSpeed", m_rotateSpeed);
        m_computeShader.SetFloat("moveHeight", m_moveHeight);

    }

    private void InitializeCubeParamBuffer() {

        CubeParameter[] cubeParameters = new CubeParameter[m_instanceCount];

        for(int i = 0; i < m_instanceCount; ++i) {

            cubeParameters[i].position = new Vector3(Random.Range(-100f, 100f), Random.Range(-100f, 100f), Random.Range(-100f, 100f));
            cubeParameters[i].angle = new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f));
            cubeParameters[i].scale = Random.Range(0.1f, 1f);
            cubeParameters[i].randTime = Random.Range(-Mathf.PI * 2, Mathf.PI * 2);
            cubeParameters[i].baseHeight = cubeParameters[i].position.y;

        }

        // Marshal.SizeOfで構造体CubeParameterのサイズを取得する
        m_cubeParamBuffer = new ComputeBuffer(m_instanceCount, Marshal.SizeOf(typeof(CubeParameter)));

        m_cubeParamBuffer.SetData(cubeParameters);

        m_material.SetBuffer("cubeParamBuffer", m_cubeParamBuffer);
    }

    readonly private int m_TimeId = Shader.PropertyToID("_Time");

    private void UpdatePositionAndAngle() {

        m_computeShader.SetFloat(m_TimeId, Time.deltaTime);
        m_computeShader.Dispatch(m_kernelID, m_groupSize.x, m_groupSize.y, m_groupSize.z);

    }


    private void OnDisable() {

        if(m_argsBuffer != null)
            m_argsBuffer.Release();

        m_argsBuffer = null;

        if(m_cubeParamBuffer != null)
            m_cubeParamBuffer.Release();

        m_cubeParamBuffer = null;

    }

}
