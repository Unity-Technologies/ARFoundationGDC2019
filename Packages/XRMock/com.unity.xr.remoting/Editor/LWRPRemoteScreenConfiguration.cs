using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace EditorRemoting
{
    [CreateAssetMenu(fileName = "LWRPRemoteScreenConfiguration", menuName = "XR/LWRPRemoteScreenConfiguration")]
    public sealed class LWRPRemoteScreenConfiguration : RemoteScreenConfiguration
    {
        RemotePass RemotePass;
        private RemoteScreenComponent m_RemoteScreenComponent;
        
        public override void Initialize(RemoteScreenComponent rm)
        {
            RemotePass = new RemotePass();

            var proxy = rm.gameObject.AddComponent<LWRPRemoteCaptureProxy>();

            proxy.RemotePass = RemotePass;

            RemotePass.OnBeforeRender = OnBeforeRender;
            RemotePass.OnCompleted = Completed;
            RemotePass.arPreviewMaterial = new Material(Shader.Find("Test/Composition"));
            
            m_RemoteScreenComponent = rm;
            
            //m_RemoteScreenComponent.CompositeImage = CompositeImage;
        }

        private void CompositeImage(RenderTexture arg1, RenderTexture arg2)
        {
        }

        private void OnBeforeRender()
        {
            if (RemotePass.target == null)
            {
                RemotePass.target = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
                RemotePass.arPreview = m_RemoteScreenComponent.m_injectedBackground;
            }
        }

        private void Completed()
        {
            m_RemoteScreenComponent.OnRenderImageCall(RemotePass.target, null);
            RemotePass.target = null;
        }

        private void RenderImageCall(RenderTexture arg1, RenderTexture arg2)
        {
        }
    }
}