using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonRemoting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Mock;
using UnityEditor.Hardware;

namespace EditorRemoting
{
    public partial class EditorRemoting : EditorWindow
    {
        void OnFocus()
        {
            //SceneView.onPreSceneGUIDelegate -= this.OnSceneGUI;

            //SceneView.onPreSceneGUIDelegate += this.OnSceneGUI;
        }

        void OnDestroy()
        {
           // SceneView.onPreSceneGUIDelegate -= this.OnSceneGUI;
        }

        private List<Vector3> points = new List<Vector3>();
        private List<BoundedPlane> planes = new List<BoundedPlane>();

#if UNITY_2018_3_OR_NEWER
        static TSubsystem CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id = null)
            where TDescriptor : IntegratedSubsystemDescriptor<TSubsystem>
            where TSubsystem : IntegratedSubsystem<TDescriptor>
#else
        static TSubsystem CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id = null)
            where TDescriptor : SubsystemDescriptor<TSubsystem>
            where TSubsystem : Subsystem<TDescriptor>
#endif
        {
            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);
            if (descriptors.Count > 0)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    foreach (TDescriptor descriptor in descriptors)
                    {
                        if (descriptor.id.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0)
                            return descriptor.Create();
                    }
                }
                else
                {
                    TDescriptor descriptor = descriptors[0];
                    if (descriptors.Count > 1)
                    {
                        System.Type type = typeof(TDescriptor);
                        Debug.LogWarningFormat("Found {0} {1}s. Using \"{2}\"", (object)descriptors.Count, (object)type.Name, (object)descriptor.id);
                    }
                    return descriptor.Create();
                }
            }
            return (TSubsystem)null;
        }

        private Queue<Vector3> m_cameraPositions = new Queue<Vector3>();

        private List<XRDepthSubsystemDescriptor> m_depthDescriptors = new List<XRDepthSubsystemDescriptor>();
        private XRDepthSubsystem m_depth;

        private List<XRPlaneSubsystemDescriptor> m_planeDescriptors = new List<XRPlaneSubsystemDescriptor>();
        private XRPlaneSubsystem m_plane;

        [SerializeField]
        Color cameraFarPlaneColor = new Color(0, 0, 1, 0.1f);
        [SerializeField]
        Color cameraFarPlaneOutlineColor = new Color(0, 0, 0, 0.75f);

        [SerializeField]
        Color cameraSidePlaneColor = new Color(0.5f, 0.5f, 0.5f, 0.12f);
        [SerializeField]
        Color cameraSidePlaneOutlineColor = new Color(0, 0, 0, 0.3f);

        [SerializeField] private Color pointOutlineColor;
        [SerializeField] private Color pointColor;
        [SerializeField] private Color planeOutlineColor;
        [SerializeField] private Color planeColor;

        void OnSceneGUI(SceneView sceneView)
        {
            if (visualizeRemotingData && EditorApplication.isPlaying)
            {
                if (m_depth == null)
                {
                    m_depth = CreateSubsystem<XRDepthSubsystemDescriptor, XRDepthSubsystem>(m_depthDescriptors,
                        m_subsystemFilter);

                    m_depth.Start();
                }

                m_depth.GetPoints(points);

                if (m_plane == null)
                {
                    m_plane = CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(m_planeDescriptors,
                        m_subsystemFilter);

                    m_plane.Start();
                }
                
                foreach (var p in points)
                {
                    Handles.color = pointOutlineColor;
                    Handles.DrawWireCube(p, Vector3.one * visualizeScale);
                }

                points.Clear();
                planes.Clear();
                m_plane.GetAllPlanes(planes);

                foreach (var plane in planes)
                {
                    if (plane.TryGetBoundary(points))
                    {
                        Handles.color = planeColor;

                        var boundaryArray = points.ToArray();
                        Handles.DrawAAConvexPolygon(boundaryArray);

                        Handles.color = planeOutlineColor;

                        Handles.DrawPolyLine(boundaryArray);
                        Handles.DrawLine(boundaryArray[boundaryArray.Length - 1], boundaryArray[0]);
                    }
                }

                Handles.color = Color.white;

                if (visualizeCameraPath)
                {
                    foreach (var cameraPos in m_cameraPositions)
                    {
                        Handles.DrawPolyLine(m_cameraPositions.ToArray());
                    }

                }

                Handles.color = cameraSidePlaneOutlineColor;

                if (cameraToStream != null)
                {
                    var camera = cameraToStream;
                    Vector3[] frustumCorners = new Vector3[4];
                    camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

                    for (int i = 0; i < 4; i++)
                    {
                        var worldSpaceCorner = camera.transform.TransformVector(frustumCorners[i]);
                        Handles.DrawLine(cameraToStream.transform.position, worldSpaceCorner);
                    }

                Handles.color = cameraFarPlaneOutlineColor;

                Handles.DrawLine(camera.transform.TransformVector(frustumCorners[0]), camera.transform.TransformVector(frustumCorners[1]));
                Handles.DrawLine(camera.transform.TransformVector(frustumCorners[1]), camera.transform.TransformVector(frustumCorners[2]));
                Handles.DrawLine(camera.transform.TransformVector(frustumCorners[2]), camera.transform.TransformVector(frustumCorners[3]));
                Handles.DrawLine(camera.transform.TransformVector(frustumCorners[3]), camera.transform.TransformVector(frustumCorners[0]));

                Handles.color = cameraSidePlaneColor;

                Vector3[] topFrustrum = { camera.transform.TransformVector(frustumCorners[1]), camera.transform.TransformVector(frustumCorners[2]), camera.transform.position };
                Handles.DrawAAConvexPolygon(topFrustrum);

                Vector3[] sideFrustrum = { camera.transform.TransformVector(frustumCorners[2]), camera.transform.TransformVector(frustumCorners[3]), camera.transform.position };
                Handles.DrawAAConvexPolygon(sideFrustrum);

                Vector3[] bottomFrustrum = { camera.transform.TransformVector(frustumCorners[3]), camera.transform.TransformVector(frustumCorners[0]), camera.transform.position };
                Handles.DrawAAConvexPolygon(bottomFrustrum);

                Vector3[] leftFrustrum = { camera.transform.TransformVector(frustumCorners[0]), camera.transform.TransformVector(frustumCorners[1]), camera.transform.position };
                Handles.DrawAAConvexPolygon(leftFrustrum);

                Handles.color = cameraFarPlaneColor;

                Vector3[] cameraFrustrum = { camera.transform.TransformVector(frustumCorners[0]), camera.transform.TransformVector(frustumCorners[1]), camera.transform.TransformVector(frustumCorners[2]), camera.transform.TransformVector(frustumCorners[3]) };

                Handles.DrawAAConvexPolygon(cameraFrustrum);

                }
                Handles.BeginGUI();

                Handles.EndGUI();
            }
        }
    }
}