using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace EditorRemoting
{
    public struct FrameInfo
    {
        public AsyncGPUReadbackRequest? readback { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public long id { get; set; }
        public RenderTexture renderTexture { get; set; }
        public byte[] dataToSend { get; set; }
        public int textureFormatID { get; set; }
    }

    /// <summary>
    /// Helper class to connect to Camera and stream it's content to the remote device;
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class RemoteScreenComponent : MonoBehaviour
    {
        private const int m_maxNumberOfFrames = 1;

        private Camera m_Camera;

        private Coroutine m_updateFrameCoroutine;
        private bool m_waitOnFrame;
        public bool WaitOnFrame
        {
            get { return m_waitOnFrame;}
            set
            {
                if (value != m_waitOnFrame)
                {
                    if (value == true)
                    {
                        m_updateFrameCoroutine = StartCoroutine(CaptureScreenshotOnImageCoroutine());
                    }
                    else
                    {
                        if (m_updateFrameCoroutine != null)
                        {
                            StopCoroutine(m_updateFrameCoroutine);
                        }
                    }
                }
                
                m_waitOnFrame = value;
            }
        }
        
        private AsyncGPUReadbackRequest m_TextureRequest;
        private readonly Queue<FrameInfo> m_queue = new Queue<FrameInfo>();

        public Texture2D m_injectedBackground;
        private bool m_isRunning = false;

        public RenderTexture RT;
        
        //@TODO Rename
        public Action<bool> SetReadyCall;
        public Action<RenderTexture, RenderTexture> OnRenderImageCall;
        public Action<RenderTexture, RenderTexture> CompositeImage;
        
        public Action<FrameInfo, Action> ProcessImage;
        public Func<bool> CanProcessImages;

        public void SetPreviewTexture(Texture2D previewTex)
        {
            m_injectedBackground = previewTex;
        }

        private void Awake()
        {
            m_Camera = GetComponent<Camera>();

            OnRenderImageCall = OnRenderImage;
            CompositeImage = CompositeARPReview;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
        }

        private void Start()
        {
            if(SetReadyCall != null)
                SetReadyCall(true);

            m_isRunning = true;
        }

        public void Reset()
        {
            isBusy = false;

            if (m_queue != null)
            {
                m_queue.Clear();
            }
        }

        private void Update()
        {
            if (m_waitOnFrame == false)
            {
                CaptureScreenshotOnImage();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void PlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                DestroyImmediate(this);
            }
        }
#endif
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
#if UNITY_EDITOR
            if (m_isRunning)
            {
                if (CanProcessImages != null && CanProcessImages() && m_queue.Count < m_maxNumberOfFrames)
                {
                    var fi = new FrameInfo();

                    fi.width = source.width;
                    fi.height = source.height;

                    if (SystemInfo.supportsAsyncGPUReadback)
                    {
                        if(SetReadyCall != null)
                            SetReadyCall(false);
                        fi.readback = AsyncGPUReadback.Request(source, 0, TextureFormat.ARGB4444);
                        fi.textureFormatID = (int) TextureFormat.ARGB4444;
                    }
                    else
                    {
                        Debug.Log("Add support for not compatible hardware");
                        //@TODO Add support for legacy capture frame
                    }

                    m_queue.Enqueue(fi);
                }
            }
#endif
            if (CompositeImage != null)
            {
                CompositeImage(source, destination);
            }
        }

        private void CompositeARPReview(RenderTexture source, RenderTexture destination)
        {
            if (m_injectedBackground != null)
            {
                Graphics.Blit(m_injectedBackground, destination, Vector2.one, Vector2.zero);

                if (compositionMaterial == null)
                {
                    compositionMaterial = new Material(Shader.Find("Test/Composition"));
                }

                Graphics.Blit(source, destination, compositionMaterial, 0);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        public Material compositionMaterial;

        private bool isBusy = false;

        private bool CanProcessCapturedFrame()
        {
            return m_queue.Count > 0 && ProcessImage != null && !isBusy;
        }
        
        private void ProcessCapturedImageCore(FrameInfo frame)
        {
            if (!frame.readback.Value.hasError)
            {
                try
                {
                    ProcessImage(frame, () =>
                    {
                        m_queue.Dequeue();
                        isBusy = false;

                        if (SetReadyCall != null)
                            SetReadyCall(true);
                    });
                }
                catch (InvalidOperationException exception)
                {
                    m_queue.Dequeue();
                    Debug.LogError(exception.Message);
                }
            }
        }
        
        void CaptureScreenshotOnImage()
        {
#if UNITY_EDITOR
            if (CanProcessCapturedFrame())
            {
                isBusy = true;
                var frame = m_queue.Peek();
                
                if(frame.readback != null)
                    frame.readback.Value.WaitForCompletion();

                ProcessCapturedImageCore(frame);
            }
#endif
        }

        public IEnumerator CaptureScreenshotOnImageCoroutine()
        {
#if UNITY_EDITOR
            do
            {
                if (CanProcessCapturedFrame())
                {
                    isBusy = true;
                    var frame = m_queue.Peek();
                    
                    yield return new WaitWhile(() => { return !frame.readback.Value.done; });
                    
                    ProcessCapturedImageCore(frame);
                }
                else
                {
                    yield return null;
                }
            } while (m_waitOnFrame);
#endif
            yield return null;
        }
    }
}