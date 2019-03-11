using CommonRemoting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.Mock;

namespace EditorRemoting
{
    public interface IMockProxy
    {
		void SetEnabled(bool enabled);
		bool GetEnabled();

        void ProcessPlane(byte[] planeData);
        void ProcessPlane(BoundedPlaneData planeData);

        void ProcessDepthData(byte[] depthData);
        void ProcessDepthData(DepthData depthData);
        void ProcessDepthData(List<Vector3> points);

        void UpdateFrameData(CameraFrameData frameData);
        void UpdateTrackingState(int trackingData);

        void ProcessCameraPose(Vector3 position, Quaternion rotation);
    }

	// Example Mock consuming data from the remoting logic and allowing to redirect it somewher else
    public class DebugMockProxy : IMockProxy
    {
		bool m_IsEnabled = false;

		public void SetEnabled(bool enabled)  
	    {
			m_IsEnabled = enabled;
			Debug.Log("SetEnabled");
        }

        public bool GetEnabled()  
	    {
			return m_IsEnabled;
        }

        public void ProcessPlane(byte[] planeData)
        {
			Debug.Log("ProcessPlane");
        }
        public void ProcessPlane(BoundedPlaneData planeData)
        {
			Debug.Log("ProcessPlane");
        }

        public void ProcessDepthData(byte[] depthData)
        {
			Debug.Log("ProcessDepthData");
        }
        public void ProcessDepthData(DepthData depthData)
        {
			Debug.Log("ProcessDepthData");
        }
        public void ProcessDepthData(List<Vector3> points)
        {
			Debug.Log("ProcessDepthData");
        }

        public void UpdateFrameData(CameraFrameData frameData)
        {
			Debug.Log("UpdateFrameData");
        }
        public void UpdateTrackingState(int trackingData)
        {
			Debug.Log("UpdateTrackingState");
        }

        public void ProcessCameraPose(Vector3 position, Quaternion rotation)
        {
			Debug.Log("ProcessCameraPose");
        }
	}

    /// <summary>
    /// Class implementing XR SDK Proxy allowing to inject data to the XR SDK
    /// </summary>
    public class XRSDKMockProxy : IMockProxy
    {
		bool m_IsEnabled = false;

		public void SetEnabled(bool enabled)  
	    {
			m_IsEnabled = enabled;
			Debug.Log("SetEnabled");
        }

        public bool GetEnabled()  
	    {
			return m_IsEnabled;
        }

        static Vector3[] s_Points;

        public EditorDataSender DataSender;
        public IConnectionProvider ConnectionProvider;
        public IConnectionProvider SeparateThreadConnectionProvider;

        private static XRSDKMockProxy Instance;
        
        public void Initialize()
        {
            Instance = this;
            
            NativeApi.UnityARMock_setAddReferencePointHandler(
                new NativeApi.AddReferencePointHandler(AddReferencePointHandler),
                new NativeApi.RequestRemoveReferencePointDelegate(RemoveReferencePointHandler));
            
            NativeApi.UnityARMock_setRaycastHandler(
                new NativeApi.UnityProcessRaycastCallbackDelegate(RaycastHandler));

            NativeApi.UnityXRMock_setLightEstimation(
                new NativeApi.SetLightEstimationDelegate(SetLightEstimation));

        }

        static bool hasResponse = false;
        static NativeApi.AddReferenceResult response;

        // Code below is a prototype code to do TryAdd/TryRemove calls to remote device and wait for the respond before proceeding. TODO - Refactor
        static private bool RemoveReferencePointHandler(ulong id1, ulong id2)
        {
            Instance.SeparateThreadConnectionProvider.OnStreamReceived += SeparateThreadConnectionProvider_OnStreamReceived;

            MemoryStream stream = new MemoryStream(1024 * 1024);

            PacketWriter writer = new PacketWriter();

            writer.BeginMessage((byte)RemoteMessageID.ARReferencePointRemove);
            writer.Write(id1);
            writer.Write(id2);
            writer.EndMessage(stream);

            Instance.SeparateThreadConnectionProvider.Update();

            Instance.SeparateThreadConnectionProvider.SendMessage(stream);

            do
            {
                Thread.Sleep(250);

                Instance.SeparateThreadConnectionProvider.Update();

            } while (!hasResponse);


            Instance.SeparateThreadConnectionProvider.OnStreamReceived -= SeparateThreadConnectionProvider_OnStreamReceived;


            return true;
        }

        static IntPtr AddReferencePointHandler(float px, float py, float pz,
            float rx, float ry, float rz, float rw)
        {
            Instance.SeparateThreadConnectionProvider.OnStreamReceived += SeparateThreadConnectionProvider_OnStreamReceived;

            MemoryStream stream = new MemoryStream(1024 * 1024);

            PacketWriter writer = new PacketWriter();

            writer.BeginMessage((byte)RemoteMessageID.ARReferencePointAdd);
            writer.Write(px);
            writer.Write(py);
            writer.Write(pz);
            writer.Write(rx);
            writer.Write(ry);
            writer.Write(rz);
            writer.Write(rw);
            writer.EndMessage(stream);

            Instance.SeparateThreadConnectionProvider.Update();

            Instance.SeparateThreadConnectionProvider.SendMessage(stream);

            while (!hasResponse)
            {
                Thread.Sleep(250);

                Instance.SeparateThreadConnectionProvider.Update();
            }


            Instance.SeparateThreadConnectionProvider.OnStreamReceived -= SeparateThreadConnectionProvider_OnStreamReceived;

            response.result = true;
            NativeApi.UnityXRMock_addReferenceResultData(response.id1, response.id2, response.result, response.trackingState);
            
            byte[] bytesToSend = RemotingUtils.StructToByteArray<NativeApi.AddReferenceResult>(response);
            
            IntPtr ptr = Marshal.AllocHGlobal(bytesToSend.Length);
            Marshal.StructureToPtr(response, ptr, false);
            
            return ptr;
        }

        private static void SeparateThreadConnectionProvider_OnStreamReceived(Stream stream, int available)
        {
            BinaryReader reader = new BinaryReader(stream);

            reader.ReadByte(); // msg
            reader.ReadUInt32(); // size

            ulong id1 = reader.ReadUInt64();
            ulong id2 = reader.ReadUInt64();

            int trackingState = reader.ReadInt32();
            bool result = reader.ReadBoolean();

            NativeApi.AddReferenceResult addReferenceResponse;
            addReferenceResponse.id1 = id1;
            addReferenceResponse.id2 = id2;
            addReferenceResponse.result = result;
            addReferenceResponse.trackingState = trackingState;

            response = addReferenceResponse;

            hasResponse = true;
        }

        static List<XRRaycastHit> s_RaycastHits = new List<XRRaycastHit>();

        static XRRaycastHit[] s_RaycastHitsArray;

        private Camera camera;

        public void SetCamera(Camera cam)
        {
            camera = cam;
        }

        XRDepthSubsystem xrDepthSubsystem;
        XRPlaneSubsystem xrPlaneSubsystem;

        public void SetSubsystemDependencies(XRDepthSubsystem depth, XRPlaneSubsystem plane)
        {
            xrDepthSubsystem = depth;
            xrPlaneSubsystem = plane;
        }

        void RaycastHandler(float x, float y, byte flag)
        {
            x = x * camera.pixelWidth;
            y = (1.0f-y) * camera.pixelHeight;
            var ray = camera.ScreenPointToRay(new Vector3(x, y, 0));
            
            XRRaycastSubsystem.Raycast(
                ray, xrDepthSubsystem, xrPlaneSubsystem,
                s_RaycastHits, (TrackableType)flag);

            int count = s_RaycastHits.Count;
            if (s_RaycastHitsArray == null || s_RaycastHitsArray.Length < count)
                s_RaycastHitsArray = new XRRaycastHit[count];
            
            for (int i = 0; i < count; ++i)
                s_RaycastHitsArray[i] = s_RaycastHits[i];

            NativeApi.UnityXRMock_setRaycastHits(
                s_RaycastHitsArray, count);
        }

        private void SetLightEstimation(bool enable)
        {
        }

        public void UpdateFrameData(byte[] frameDataBytes)
        {
            int size = Marshal.SizeOf(typeof(CameraFrameData));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(frameDataBytes, 0, ptr, frameDataBytes.Length);

            NativeApi.UnityXRMock_setCameraFrameData(ptr);
            Marshal.FreeHGlobal(ptr);
        }

        public void UpdateFrameData(CameraFrameData frameData)
        {
            var frameDataBytes = RemotingUtils.StructToByteArray<CameraFrameData>(frameData);

            UpdateFrameData(frameDataBytes);
        }

        public void ProcessPlane(byte[] planeDataBytes)
        {
            int size = Marshal.SizeOf(typeof(BoundedPlaneData));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(planeDataBytes, 0, ptr, planeDataBytes.Length);

            NativeApi.UnityXRMock_processPlaneEvent(ptr, planeDataBytes.Length);
            Marshal.FreeHGlobal(ptr);
        }

        public void ProcessPlane(BoundedPlaneData planeData)
        {
            var planeDataBytes = RemotingUtils.StructToByteArray<BoundedPlaneData>(planeData);

            ProcessPlane(planeDataBytes);
        }

        public void ProcessCameraPose(Vector3 position, Quaternion rotation)
        {
            InputApi.pose = new Pose(position, rotation);
        }

        public void ProcessDepthData(List<Vector3> points)
        {
            if (!EditorApplication.isPlaying)
                return;

            NativeApi.UnityXRMock_setDepthData(points.ToArray(), null, points.Count);
        }

        public void ProcessDepthData(byte[] planeDataBytes)
        {
            if (!EditorApplication.isPlaying)
                return;

            MemoryStream stream = new MemoryStream(planeDataBytes);
            BinaryReader reader = new BinaryReader(stream);

            var numOfPoints = reader.ReadInt32();

            if (s_Points == null || s_Points.Length < numOfPoints)
                s_Points = new Vector3[numOfPoints];

            for (int i = 0; i < numOfPoints; i++)
            {
                s_Points[i] = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }

            NativeApi.UnityXRMock_setDepthData(s_Points, null, numOfPoints);
        }

        public void ProcessDepthData(DepthData planeData)
        {
            if (!EditorApplication.isPlaying)
                return;

            var planeDataBytes = RemotingUtils.StructToByteArray<DepthData>(planeData);

            ProcessDepthData(planeDataBytes);
        }

        public void UpdateTrackingState(int trackingState)
        {
            if (!EditorApplication.isPlaying)
                return;

            SessionApi.trackingState = (TrackingState)trackingState;
        }
    }
}