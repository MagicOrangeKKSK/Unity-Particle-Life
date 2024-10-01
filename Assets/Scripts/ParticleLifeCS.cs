using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class ParticleLifeCS : MonoBehaviour
{
     int updateKernel = 0;
     int clearKernal = 1;


    [Header("Game Settings")]
    public int numAgents = 1000;
    public int totalType = 6;
    public float dt = 0.02f;
    public float frictionHalfLife = 0.04f;
    public float rMax = 1f;
    public float frictionFactor;
    public float forceFactor = 10f;
    public float beta = 0.3f;
    public float[] matrix;

    [Header("Window Settings")]
    public int width = 1920;
    public int height = 1080;
    public FilterMode filterMode = FilterMode.Point;
    public  GraphicsFormat format = GraphicsFormat.R16G16B16A16_SFloat;

    [Header("cs")]
    public ComputeShader compute;
    ComputeBuffer agentBuffer;
    ComputeBuffer agentMatrixBuffer;

    [SerializeField, HideInInspector] protected RenderTexture texture;

    private void Start()
    {
        updateKernel = compute.FindKernel("DrawParticleLife");
        clearKernal = compute.FindKernel("ClearTexture");
        frictionFactor = Mathf.Pow(0.5f, dt / frictionHalfLife);
        Init();
        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture= texture;
    }

    private void Init()
    {
        //创建渲染用的贴图
        ComputeHelper.CreateRenderTexture(ref texture, width, height, filterMode, format);
        compute.SetTexture(updateKernel, "Result", texture);
        compute.SetTexture(clearKernal, "Result", texture);

        //创建Agents 并初始化坐标
        Agent[] agents = new Agent[numAgents];
        for(int i =0;i<numAgents;i++)
        {
            agents[i] = new Agent()
            {
                position = new Vector2(Random.Range(0, width), Random.Range(0, height)),
                velocity = Vector2.zero,
                type = Random.Range(0,totalType),
            };

            Debug.Log(agents[i].type);
        }

        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer,agents,compute,"agents",updateKernel);
        compute.SetFloat("totalType", totalType);
        compute.SetInt("numAgents",numAgents);
        compute.SetInt("width", width);
        compute.SetInt("height", height);
        compute.SetFloat("dt",dt);
        compute.SetFloat("frictionHalfLife", frictionHalfLife);
        compute.SetFloat("rMax", rMax);
        compute.SetFloat("frictionFactor",frictionFactor);
        compute.SetFloat("forceFactor", forceFactor);
        compute.SetFloat("beta", beta);

        matrix = new float[totalType * totalType];
        for(int i=0;i<matrix.Length;i++)
            matrix[i] = Random.Range(-1f, 1f);

        ComputeHelper.CreateAndSetBuffer(ref agentMatrixBuffer, matrix, compute, "agent_matrix", updateKernel);
    }

    private float timer = 0f;
    public void Update()
    {
        timer += Time.deltaTime;
        while(timer >= dt)
        {
            compute.SetFloat("frictionHalfLife", frictionHalfLife);
            compute.SetFloat("rMax", rMax);
            compute.SetFloat("forceFactor", forceFactor);
            compute.SetFloat("beta", beta);


            timer -= dt;
            int threadGroupsX = Mathf.CeilToInt(width / 16);
            int threadGroupsY = Mathf.CeilToInt(height / 16);
            compute.Dispatch(clearKernal, threadGroupsX, threadGroupsY, 1); 

            compute.SetFloat("dt", Time.deltaTime);
            threadGroupsX = Mathf.CeilToInt(numAgents / 16f);
            compute.Dispatch(updateKernel, threadGroupsX, 1, 1);
        }
    }
    [SerializeField]
    public struct Agent
    {
        public int type;
        public Vector2 position;
        public Vector2 velocity;
    }

    private void OnDestroy()
    {
        if (agentBuffer != null)
        {
            agentBuffer.Release();
        }

        if(agentMatrixBuffer != null)
        {
            agentMatrixBuffer.Release();
        }
    }
}


public static class ComputeHelper 
{
    #region TOOL
    public static void CreateRenderTexture(ref RenderTexture texture, int width, int height, FilterMode filterMode, GraphicsFormat format)
    {
        if (texture == null || !texture.IsCreated() || texture.width != width || texture.height != height || texture.graphicsFormat != format)
        {
            if (texture != null)
            {
                texture.Release();
            }
            texture = new RenderTexture(width, height, 0);
            texture.graphicsFormat = format;
            texture.enableRandomWrite = true;

            texture.autoGenerateMips = false;
            texture.Create();
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filterMode;
    }

    public static void CreateAndSetBuffer<T>(ref ComputeBuffer buffer, T[] data, ComputeShader cs, string nameID, int kernelIndex = 0)
    {
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        CreateStructuredBuffer<T>(ref buffer, data.Length);
        buffer.SetData(data);
        cs.SetBuffer(kernelIndex, nameID, buffer);
    }

    public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, int count)
    {
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        bool createNewBuffer = buffer == null || !buffer.IsValid() || buffer.count != count || buffer.stride != stride;
        if (createNewBuffer)
        {
            Release(buffer);
            buffer = new ComputeBuffer(count, stride);
        }
    }
    /// Releases supplied buffer/s if not null
    public static void Release(params ComputeBuffer[] buffers)
    {
        for (int i = 0; i < buffers.Length; i++)
        {
            if (buffers[i] != null)
            {
                buffers[i].Release();
            }
        }
    }
    #endregion

}
