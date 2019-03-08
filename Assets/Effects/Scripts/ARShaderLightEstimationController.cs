using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ARShaderLightEstimationController : MonoBehaviour
{

    [Header("References")]
    public Light directionalLight;

    [Header("Editor Testing (Ignored at Runtime)")]
    public float editorEmissionValue;

    private int arLightEmissionID;

    void OnEnable()
    {
        arLightEmissionID = Shader.PropertyToID("_AR_LightEmission");
    }

    void Update()
    {
         if(!Application.isPlaying)
        {
           Shader.SetGlobalFloat(arLightEmissionID, editorEmissionValue);

        } else {

            Shader.SetGlobalFloat(arLightEmissionID, directionalLight.intensity);
            
        }
    }
}
