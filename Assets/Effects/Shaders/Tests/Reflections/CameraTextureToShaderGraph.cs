using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// This component tests getting the latest camera image
/// and converting it to RGBA format. If successful,
/// it displays the image on the screen as a RawImage
/// and also displays information about the image.
/// 
/// This is useful for computer vision applications where
/// you need to access the raw pixels from camera image
/// on the CPU.
/// 
/// This is different from the ARCameraBackground component, which
/// efficiently displays the camera image on the screen. If you
/// just want to blit the camera texture to the screen, use
/// the ARCameraBackground, or use Graphics.Blit to create
/// a GPU-friendly RenderTexture.
/// 
/// In this example, we get the camera image data on the CPU,
/// convert it to an RGBA format, then display it on the screen
/// as a RawImage texture to demonstrate it is working.
/// This is done as an example; do not use this technique simply
/// to render the camera image on screen.
/// </summary>
public class CameraTextureToShaderGraph : MonoBehaviour
{

    ARCameraManager m_CameraManager;

    void OnEnable()
    {
        m_CameraManager = GetComponent<ARCameraManager>();
        m_CameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        m_CameraManager.frameReceived -= OnCameraFrameReceived;
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        XRCameraImage image;
        if (!m_CameraManager.TryGetLatestImage(out image))
            return;

        // Display some information about the camera image
        

        // Once we have a valid CameraImage, we can access the individual image "planes"
        // (the separate channels in the image). CameraImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See CameraImage.FormatSupported for a complete list of supported formats.
        var format = TextureFormat.RGBA32;

        if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
            m_Texture = new Texture2D(image.width, image.height, format, false);

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.None);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_Texture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the CameraImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_Texture.Apply();

        

        Shader.SetGlobalColor("_AR_LightColor", AverageColorFromTexture(m_Texture));

       
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
