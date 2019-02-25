using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EnvironmentSliceController : MonoBehaviour
{
    
    void Update()
    {
        Shader.SetGlobalVector("_Slice_Transform", transform.position);
    }
}
