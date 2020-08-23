using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestDrawMesh : MonoBehaviour
{

    [Header("DrawMeshInstancedIndirectのパラメータ")]
    [SerializeField]
    private Mesh m_mesh;

    [SerializeField]
    private Material m_instanceMaterial;

    [SerializeField]
    private Bounds m_bounds;

    [SerializeField]
    private ShadowCastingMode m_shadowCastingMode;

    [SerializeField]
    private bool m_receiveShadows;

    [SerializeField]
    private string m_layerName;

    [SerializeField]
    private Camera m_camera;


    [Space(20)]
    [SerializeField]
    private int m_instanceCount;

    private ComputeBuffer m_argsBuffer;

    private ComputeBuffer m_positionBuffer;

    private ComputeBuffer m_eulerAngleBuffer;

    private int m_layer;

    // Start is called before the first frame update
    void Start()
    {
        InitializeArgsBuffer();
        InitializePositionBuffer();
        InitializeEulerAngleBuffer();

        m_layer = LayerMask.NameToLayer(m_layerName);
    }

    // Update is called once per frame
    void Update()
    {

        Graphics.DrawMeshInstancedIndirect(
            m_mesh,
            0,
            m_instanceMaterial,
            m_bounds,
            m_argsBuffer,
            0,
            null,
            m_shadowCastingMode,
            m_receiveShadows,
            m_layer,
            m_camera,
            LightProbeUsage.BlendProbes,
            null
        );

    }

    private void InitializeArgsBuffer() {

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        uint numIndices = (m_mesh != null) ? (uint) m_mesh.GetIndexCount(0) : 0;

        args[0] = numIndices;
        args[1] = (uint)m_instanceCount;
        
        m_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);

    }

    private void InitializePositionBuffer() {

        // xyz:座標   w:スケール
        Vector4[] positions = new Vector4[m_instanceCount];

        for (int i = 0; i < m_instanceCount; ++i)
        {

            positions[i].x = Random.Range(-100.0f, 100.0f);
            positions[i].y = Random.Range(-100.0f, 100.0f);
            positions[i].z = Random.Range(-100.0f, 100.0f);

            positions[i].w = Random.Range(0.1f, 1f);

        }

        m_positionBuffer = new ComputeBuffer(m_instanceCount, 4 * 4); // 4 * 4 = (floatのbyte）* (要素数)
        m_positionBuffer.SetData(positions);

        m_instanceMaterial.SetBuffer("positionBuffer", m_positionBuffer);

    }

    private void InitializeEulerAngleBuffer() {

        Vector3[] angles = new Vector3[m_instanceCount];

        for(int i = 0; i < m_instanceCount; ++i) {
            angles[i].x = Random.Range(-180.0f, 180.0f);
            angles[i].y = Random.Range(-180.0f, 180.0f);
            angles[i].z = Random.Range(-180.0f, 180.0f);
        }

        m_eulerAngleBuffer = new ComputeBuffer(m_instanceCount, 4 * 3);
        m_eulerAngleBuffer.SetData(angles);

        m_instanceMaterial.SetBuffer("eulerAngleBuffer", m_eulerAngleBuffer);

    }

    // 領域の解放
    private void OnDisable() {

        if(m_positionBuffer != null)
            m_positionBuffer.Release();
        m_positionBuffer = null;

        if(m_eulerAngleBuffer != null)
            m_eulerAngleBuffer.Release();
        m_eulerAngleBuffer = null;

        if(m_argsBuffer != null)
            m_argsBuffer.Release();
        m_argsBuffer = null;

    }

}
