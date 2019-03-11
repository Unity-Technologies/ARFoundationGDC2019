
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

public class LWRPRemoteCaptureProxy: MonoBehaviour, IAfterRender
{
    public RemotePass RemotePass;
    public RenderTexture rt;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public ScriptableRenderPass GetPassToEnqueue()
    {
        if (RemotePass == null)
        {
            RemotePass = new RemotePass();
            RemotePass.OnCompleted = Completed;
        }

        if (RemotePass.target == null)
        {
            //RemotePass.target = rt;
        }
            
       return RemotePass;
    }

    private void Completed()
    {
    }
}