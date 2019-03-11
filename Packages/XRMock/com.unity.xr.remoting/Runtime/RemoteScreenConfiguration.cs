using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace EditorRemoting
{
    [CreateAssetMenu(fileName = "RemoteScreenConfigurationAsset", menuName = "XR/RemoteScreenConfiguration")]
    public class RemoteScreenConfiguration : ScriptableObject
    {
        public virtual void Initialize(RemoteScreenComponent rm)
        {
            
        }
    }
}