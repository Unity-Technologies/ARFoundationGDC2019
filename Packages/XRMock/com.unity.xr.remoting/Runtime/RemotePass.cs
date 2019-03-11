using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class RemotePass : ScriptableRenderPass
    {
        const string k_RemoteTag = "Remote Tag";
        
        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }

        public RenderTexture target;
        public Texture2D arPreview;
        public Material arPreviewMaterial;

        public Action OnBeforeRender;
        public Action OnCompleted;
        
        public RemotePass()
        {
        }
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorAttachmentHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.descriptor = baseDescriptor;
        }


        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            if(OnBeforeRender != null)
                OnBeforeRender.Invoke();

            if(target == null)
                return;
            
            Material material = renderingData.cameraData.isStereoEnabled ? null : renderer.GetMaterial(MaterialHandle.Blit);

            CommandBuffer cmd = CommandBufferPool.Get(k_RemoteTag);
            
            cmd.Blit(null, target, material);
            
            if(arPreview != null)
                cmd.Blit(target, arPreview, arPreviewMaterial);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.Submit();

            if(OnCompleted != null)
                OnCompleted.Invoke();
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }
}