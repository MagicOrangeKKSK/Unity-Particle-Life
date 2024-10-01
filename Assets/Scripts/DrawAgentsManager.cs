using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class DrawAgentsManager : MonoBehaviour
{
    public struct Agent
    {
        public int type;
        public Vector2 position;
        public Vector2 velocity;
    }

    public int numAgents = 100;
    public float speed = 10f;
    private ComputeBuffer agentBuffer;
    private Agent[] agents;
    
    public ComputeShader compute;
    public RenderTexture texture;

    [Header("Window Settings")]
    public int width = 1920;
    public int height = 1080;
    public FilterMode filterMode = FilterMode.Point;
    public GraphicsFormat format = GraphicsFormat.R16G16B16A16_SFloat;

    public void Start()
    {
        Init();
        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = texture;
    }

    private void Init()
    {
        ComputeHelper.CreateRenderTexture(ref texture,width,height,filterMode,format);

        agents = new Agent[numAgents];
        for(int i = 0; i< numAgents; i++)
        {
            agents[i] = new Agent()
            {
                type = Random.Range(0, 6),
                position = new Vector2(Random.Range(0, width), Random.Range(0, height)),
                velocity = Random.insideUnitCircle * speed,
            };
        }

        ComputeHelper.CreateAndSetBuffer(ref agentBuffer, agents, compute, "agents");
        compute.SetInt("numAgents",numAgents);
        compute.SetInt("width", width);
        compute.SetInt("height", height);
        compute.SetFloat("speed", speed);
        compute.SetTexture(0, "TargetTexture", texture);
        compute.SetTexture(1, "TargetTexture", texture);

    }


    private void Update()
    {

        int threadGroupsX = Mathf.CeilToInt(width / 16.0f);
        int threadGroupsY = Mathf.CeilToInt(height / 16.0f);
        compute.Dispatch(1, threadGroupsX, threadGroupsY, 1); // 调用 ClearTexture kernel222222

        compute.SetFloat("speed", speed);
        compute.SetFloat("dt", Time.deltaTime);
         threadGroupsX = Mathf.CeilToInt(numAgents / 16f);
        compute.Dispatch(0, threadGroupsX, 1, 1);
    }

    private void OnDestroy()
    {
        if(agentBuffer != null)
        {
            agentBuffer.Release();
        }
    }
}
