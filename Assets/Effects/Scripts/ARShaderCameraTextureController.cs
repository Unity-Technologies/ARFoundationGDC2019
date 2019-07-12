using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[ExecuteAlways]
public class ARShaderCameraTextureController : MonoBehaviour
{

    public int imageDownSampleFactor = 2;

    [Header("Editor Testing (Ignored at Runtime)")]
    public Color editorCameraTextureColor;

    private int arCameraTextureColorID;

    ARCameraManager m_CameraManager;

    void OnEnable()
    {
        m_CameraManager.frameReceived += OnCameraFrameReceived;
        arCameraTextureColorID = Shader.PropertyToID("_AR_CameraTextureColor");
    }

    void OnDisable()
    {
        m_CameraManager.frameReceived -= OnCameraFrameReceived;
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
       
        XRCameraImage image;
        if (!m_CameraManager.TryGetLatestImage(out image))
            return;

       
        var format = TextureFormat.RGBA32;

        if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
            m_Texture = new Texture2D(image.width, image.height, format, false);

       
        var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.None);
        conversionParams.outputDimensions = new Vector2Int(image.width/imageDownSampleFactor, image.height/imageDownSampleFactor);

      
        var rawTextureData = m_Texture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
           
            image.Dispose();
        }

       
        m_Texture.Apply();

        

        Shader.SetGlobalColor(arCameraTextureColorID, AverageColorFromTexture(m_Texture));

       
    }

    void Update()
    {
        if(!Application.isPlaying)
        {
           Shader.SetGlobalColor(arCameraTextureColorID, editorCameraTextureColor);

        }
    }

    Texture2D m_Texture;

    Color32 AverageColorFromTexture(Texture2D tex)
{
 
        Color32[] texColors = tex.GetPixels32();
 
        int total = texColors.Length;
 
        float r = 0;
        float g = 0;
        float b = 0;
 
        for(int i = 0; i < total; i++)
        {
 
            r += texColors[i].r;
 
            g += texColors[i].g;
 
            b += texColors[i].b;
 
        }
 
        return new Color32((byte)(r / total) , (byte)(g / total) , (byte)(b / total) , 0);
 
}
}
