using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class ShaderMainLightSettings : MonoBehaviour
{	
	private int shaderMainLightDirectionID;
	private Transform storedLightDirection;

	void OnEnable()
	{
		shaderMainLightDirectionID = Shader.PropertyToID("_MainLightDirection");
	}
	
	void Update ()
	{
		Shader.SetGlobalVector(shaderMainLightDirectionID, -transform.forward);
		Debug.Log("Set Light Rotation");
	}
}
