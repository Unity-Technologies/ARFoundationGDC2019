using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TerraformerLightEstimationController : MonoBehaviour
{

    LightEstimation lightEstimation;
    private Light light;

    [Header("Editor Test")]
    public float editorEmissionValue;

    private int arLightEmissionID;
    private int arLightColorID;


    void OnEnable()
    {
        arLightEmissionID = Shader.PropertyToID("_AR_LightEmission");
        arLightColorID = Shader.PropertyToID("_AR_LightColor");

        lightEstimation = FindObjectOfType<LightEstimation>();
        light = GameObject.Find("Directional Light").GetComponent<Light>();

    }
    

    void Update()
    {
        if(!Application.isPlaying)
        {
           Shader.SetGlobalFloat(arLightEmissionID, editorEmissionValue);
        } else {

            Shader.SetGlobalFloat(arLightEmissionID, lightEstimation.brightness.Value);
            Shader.SetGlobalColor(arLightColorID, light.color);

            
        }
    }
}
