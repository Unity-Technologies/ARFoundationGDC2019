using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ARShaderLightEstimationController : MonoBehaviour
{

    [Header("References")]
    LightEstimation lightEstimation;
    public Light directionalLight;

    [Header("Editor Testing (Ignored at Runtime)")]
    public float editorEmissionValue;
    public Color editorColorValue;

    private int arLightEmissionID;
    private int arLightColorID;

    void OnEnable()
    {
        arLightEmissionID = Shader.PropertyToID("_AR_LightEmission");
        arLightColorID = Shader.PropertyToID("_AR_LightColor");

        lightEstimation = FindObjectOfType<LightEstimation>();
    }

    void Update()
    {
         if(!Application.isPlaying)
        {
           Shader.SetGlobalFloat(arLightEmissionID, editorEmissionValue);
           Shader.SetGlobalColor(arLightColorID, editorColorValue);

        } else {

            Shader.SetGlobalFloat(arLightEmissionID, lightEstimation.brightness.Value);
            Shader.SetGlobalColor(arLightColorID, directionalLight.color);
            
        }
    }
}
