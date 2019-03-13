using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LightEstimation))]
public class LightEstimationColor : MonoBehaviour
{

    public Light light;

    [SerializeField]
    Image m_ColorCorrectionImage;

    public Image colorCorrectionImage
    {
        get { return m_ColorCorrectionImage; }
        set { m_ColorCorrectionImage = value; }
    }

    void Awake()
    {
        m_LightEstimation = GetComponent<LightEstimation>();
    }

    void Update()
    {
        Color shaderValue = light.color * light.intensity;

        m_ColorCorrectionImage.color = shaderValue;
        Shader.SetGlobalColor("_AR_LightColor", shaderValue);
    }



    const string k_UnavailableText = "Unavailable";

    LightEstimation m_LightEstimation;
}

