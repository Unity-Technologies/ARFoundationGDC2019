using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.Mock
{
    /// <summary>
    /// Provides functionality to inject data into the XRMock provider.
    /// </summary>
    public static class NativeApi
    {
        static NativeApi()
        {
            UnityXRMock_setTrackableIdGenerator(NewTrackableId);
        }

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_connectDevice(int id);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_disconnectDevice(int id);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setPose(Pose pose, Matrix4x4 transform);

        [StructLayout(LayoutKind.Sequential)]
        public struct AddReferenceResult
        {
            public ulong id1;
            public ulong id2;

            public int trackingState;
            public bool result;
        };

        public delegate IntPtr AddReferencePointHandler(float px, float py, float pz,
            float rx, float ry, float rz, float rw);

        public delegate bool RequestRemoveReferencePointDelegate(UInt64 id1, UInt64 id2);

        [DllImport("UnityXRMock")]
        public static extern void UnityARMock_setAddReferencePointHandler(
            AddReferencePointHandler fp, RequestRemoveReferencePointDelegate fp2);

        //Test passing all params without need for marshaling
        [DllImport("UnityXRMock")]
        public static extern bool UnityXRMock_addReferenceResultData(ulong id0, ulong id1, bool result, int trackingState);

        [DllImport("UnityXRMock")]
        public static extern bool UnityXRMock_processPlaneEvent(IntPtr planeData, int size);

        public delegate void SetLightEstimationDelegate(bool enabled);

        public delegate void UnityProcessRaycastCallbackDelegate(float x, float y, byte type);

        [DllImport("UnityXRMock")]
        public static extern void UnityARMock_setRaycastHandler(UnityProcessRaycastCallbackDelegate fp);

        #region CameraProvider
        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setLightEstimation(SetLightEstimationDelegate fp);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setCameraFrameData(IntPtr frameData);
        #endregion

        #region SessionProvider
        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setTrackingState(int trackingData);
        #endregion

        [DllImport("UnityXRMock")]
        public static extern TrackableId UnityXRMock_createTrackableId(Guid guid);

        [MonoPInvokeCallback(typeof(Func<TrackableId>))]
        public static TrackableId NewTrackableId()
        {
            return UnityXRMock_createTrackableId(Guid.NewGuid());
        }

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setTrackingState(
            TrackingState trackingState);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setPlaneData(
            TrackableId planeId, Pose pose, Vector3 center, Vector2 bounds,
            Vector3[] boundaryPoints, int numPoints);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_removePlane(TrackableId planeId);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setDepthData(
            Vector3[] positions, float[] confidences, int count);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setProjectionMatrix(
            Matrix4x4 projectionMatrix, Matrix4x4 inverseProjectionMatrix, bool hasValue);

       [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setDisplayMatrix(
            Matrix4x4 displayMatrix, bool hasValue);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setAverageBrightness(
            float averageBrightness, bool hasValue);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setAverageColorTemperature(
            float averageColorTemperature, bool hasValue);

        [DllImport("UnityXRMock")] 
        public static extern void UnityXRMock_setTrackableIdGenerator(
            Func<TrackableId> generator);

        [DllImport("UnityXRMock")]
        public static extern TrackableId UnityXRMock_attachReferencePoint(
            TrackableId trackableId, Pose pose);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_updateReferencePoint(
            TrackableId trackableId, Pose pose, TrackingState trackingState);

        [DllImport("UnityXRMock")]
        public static extern void UnityXRMock_setRaycastHits(
            XRRaycastHit[] hits, int size);

    }
}
