using UnityEngine;
using System.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;


    public class AppSettings : MonoBehaviour
    {
        public enum RenderRes
        {
            _Native,
            _2440p,
            _1080p,
            _720p
        }

        public RenderRes maxRenderSize = RenderRes._720p;

        // Use this for initialization
        void Start()
        {

            float res;
            
            switch (maxRenderSize)
            {
                case RenderRes._720p:
                    res = 720f;
                    break;
                case RenderRes._1080p:
                    res = 1080f;
                    break;
                case RenderRes._2440p:
                    res = 2440f;
                    break;
                default:
                    res = Camera.main.pixelHeight;
                    break;
            }
            var newRenderScale = Mathf.Clamp(res / Camera.main.pixelHeight, 0.1f, 1.0f);
            
            LightweightRenderPipelineAsset lightweightAsset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;

            lightweightAsset.renderScale = newRenderScale;

        }

       
    }
