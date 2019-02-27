using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DissolveManager : MonoBehaviour
{
    public Transform refPoint;
    public float offset;

    void Update()
    {
        Shader.SetGlobalVector("_AR_AnchorOrigin", transform.position);
        Shader.SetGlobalFloat("_refPoint", refPoint.localPosition.y);
        Shader.SetGlobalFloat("_offset", offset);
    }
}
