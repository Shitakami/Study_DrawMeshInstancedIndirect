using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;

public class Boids : MonoBehaviour
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

    [Header("力の強さ")]
    [Header("Boidsモデルのデータ")]
    [SerializeField]
    private float m_cohesionForce = 5f;
    [SerializeField]
    private float m_separationForce = 5f;
    [SerializeField]
    private float m_alignmentForce = 2f;

    [Space(5)]
    [Header("力の働く距離")]
    [SerializeField]
    private float m_cohesionDistance = 10f;
    [SerializeField]
    private float m_separationDistance = 6f;
    [SerializeField]
    private float m_alignmentDistance = 8f;

    [Space(5)]
    [Header("力の働く角度")]
    [SerializeField]
    private float m_cohesionAngle = 90f;
    [SerializeField]
    private float m_separationAngle = 90f;
    [SerializeField]
    private float m_alignmentAngle = 60f;

    [Space(5)]
    [SerializeField]
    private float m_boundaryForce = 1;
    [SerializeField]
    private float m_boundaryRange = 35f;

    [Space(5)]
    [SerializeField]
    private float m_maxVelocity = 0.1f;

    [SerializeField]
    private float m_minVelocity = 0.05f;

    [SerializeField]    
    private float m_maxForce = 1f;

    [Space(20)]
    [SerializeField]
    private ComputeShader m_boidsSimulator;

    private struct BoidsData {
        public Vector3 position;
        public Vector3 velocity;
    }

    private ComputeBuffer m_argsBuffer;

    private ComputeBuffer m_boidsDataBuffer;

    private int m_boidsCalcKernel;

    private int m_deltaTimeID = Shader.PropertyToID("deltaTime");

    private Vector3Int m_groupSize;

    // Start is called before the first frame update
    void Start()
    {
        m_instanceCount = Mathf.ClosestPowerOfTwo(m_instanceCount);

        Assert.IsNotNull(m_mesh, "メッシュデータが設定されていません");
        Assert.IsNotNull(m_material, "マテリアルが設定されていません");

        InitializeArgsBuffer();
        InitializeBoidsDataBuffer();
        InitializeBoidsSimulator();

    }

    // Update is called once per frame
    void Update()
    {
        BoidsCalc();

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

        var args = new uint[] { 0, 0, 0, 0, 0 };

        args[0] = m_mesh.GetIndexCount(0);
        args[1] = (uint)m_instanceCount;

        m_argsBuffer = new ComputeBuffer(1, 4 * args.Length, ComputeBufferType.IndirectArguments);

        m_argsBuffer.SetData(args);

    }

    private void InitializeBoidsDataBuffer() {

        var boidsData = new BoidsData[m_instanceCount];

        for(int i = 0; i < m_instanceCount; ++i) {
            boidsData[i].position = Random.insideUnitSphere * m_boundaryRange;
            var velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            boidsData[i].velocity = velocity.normalized * m_minVelocity; 
        }

        m_boidsDataBuffer = new ComputeBuffer(m_instanceCount, Marshal.SizeOf(typeof(BoidsData)));
        m_boidsDataBuffer.SetData(boidsData);

        m_material.SetBuffer("boidsDataBuffer", m_boidsDataBuffer);

    }

    private void InitializeBoidsSimulator() {

        m_boidsCalcKernel = m_boidsSimulator.FindKernel("BoidsCalculation");

        m_boidsSimulator.GetKernelThreadGroupSizes(m_boidsCalcKernel, out uint x, out uint y, out uint z);
        m_groupSize = new Vector3Int(m_instanceCount / (int)x, (int)y, (int)z);

        m_boidsSimulator.SetFloat("cohesionForce", m_cohesionForce);
        m_boidsSimulator.SetFloat("separationForce", m_separationForce);
        m_boidsSimulator.SetFloat("alignmentForce", m_alignmentForce);

        // ComputeShader内の距離判定で2乗の値を使用しているので合わせる
        m_boidsSimulator.SetFloat("cohesionDistance", m_cohesionDistance);
        m_boidsSimulator.SetFloat("separationDistance", m_separationDistance);
        m_boidsSimulator.SetFloat("alignmentDistance", m_alignmentDistance);

        // ComputeShader内ではラジアンで判定するので度数方からラジアンに変更する
        m_boidsSimulator.SetFloat("cohesionAngle", m_cohesionAngle * Mathf.Deg2Rad);
        m_boidsSimulator.SetFloat("separationAngle", m_separationAngle * Mathf.Deg2Rad);
        m_boidsSimulator.SetFloat("alignmentAngle", m_alignmentAngle * Mathf.Deg2Rad);

        m_boidsSimulator.SetFloat("boundaryForce", m_boundaryForce);
        m_boidsSimulator.SetFloat("boundaryRange", m_boundaryRange * m_boundaryRange);

        m_boidsSimulator.SetFloat("minVelocity", m_minVelocity);
        m_boidsSimulator.SetFloat("maxVelocity", m_maxVelocity);

        m_boidsSimulator.SetInt("instanceCount", m_instanceCount);

        m_boidsSimulator.SetFloat("maxForce", m_maxForce);

        m_boidsSimulator.SetBuffer(m_boidsCalcKernel, "boidsData", m_boidsDataBuffer);

    }

    private void BoidsCalc() {

        m_boidsSimulator.SetFloat(m_deltaTimeID, Time.deltaTime);
        m_boidsSimulator.Dispatch(m_boidsCalcKernel, m_groupSize.x, m_groupSize.y, m_groupSize.z);

    }

    private void OnDisable() {

        if(m_argsBuffer != null)
            m_argsBuffer.Release();

        m_argsBuffer = null;

        if(m_boidsDataBuffer != null)
            m_boidsDataBuffer.Release();

        m_boidsDataBuffer = null;

    }

}
