using CommonRemoting;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.Profiling;
using UnityEngine.XR.ARFoundation;

#if !NET_LEGACY
using K4os.Compression.LZ4;
#endif

namespace CommonRemoting
{
    public struct IdToArray
    {
        public ulong id0;
        public ulong id1;
    }

    public enum RemoteMessageID
    {
        Invalid = 0,

        Hello = 1,
        Options = 2,
        ScreenOrientation = 3,

        DeviceOrientation = 4,
        DeviceFeatures = 5,
        GyroSettings = 7,

        ReadyToScreenStream = 8,
        ScreenStream = 9,

        TouchInput = 10,
        AccelerometerInput = 11,
        TrackBallInput = 12,
        Key = 13,
        GyroInput = 14,
        MousePresence = 15,
        JoystickInput = 16,
        JoystickNames = 17,

        WebCamStartStream = 20,
        WebCamStopStream = 21,

        LocationServiceStart = 30,
        LocationServiceStop = 31,
        CompassSetEnabled = 32,

        ARCameraData = 33,
        ARPlaneData = 34,
        ARPointCloudData = 35,
        ARCameraFrameData = 36,
        ARTrackingStateChanged = 37,

        ARLightEstimation = 100,
        ARReferencePointAdd = 101,
        ARReferencePointRemove = 103,
        ARRemoteSettings = 104,

        CustomData = 254,
        Reserved = 255,
    };

    public enum CustomDataID : byte
    {
        InputEvent = 100
    }

}

namespace ClientRemoting
{
    public class DataReceiver
    {
        MemoryStream data = new MemoryStream();
        byte[] buffer = new byte[4096];
        ScreenStream screen;
        WebCamStreamer webCamStreamer;

        public DataReceiver(ScreenStream screen, WebCamStreamer webCamStreamer)
        {
            this.screen = screen;
            this.webCamStreamer = webCamStreamer;
        }

        public void Reset()
        {
            data.Position = 0;
            data.SetLength(0);
        }

        public void AppendData(Stream stream, int available)
        {
            Profiler.BeginSample("AppendData");

            data.Position = data.Length;
            Utils.CopyToStream(stream, data, buffer, available);

            Profiler.EndSample();
        }

        public void AppendData(byte[] streamData, int available)
        {
            Profiler.BeginSample("AppendData");

            data.Position = data.Length;
            data.Write(streamData, 0, available);

            Profiler.EndSample();
        }

        public void ProcessMessages()
        {
            Profiler.BeginSample("ProcessMessages");

            data.Position = 0;

            while (HasFullMessage(data))
                ProcessMessage(data);

            // Copy leftover bytes
            long left = data.Length - data.Position;
            byte[] buffer = data.GetBuffer();
            Array.Copy(buffer, data.Position, buffer, 0, left);
            data.Position = 0;
            data.SetLength(left);

            Profiler.EndSample();
        }


        private static bool HasFullMessage(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            long oldPosition = stream.Position;
            bool result = true;

            if (stream.Length - stream.Position < 5)
                result = false;

            if (result)
            {
                reader.ReadByte();
                uint size = reader.ReadUInt32();
                if (stream.Length - stream.Position < size)
                    result = false;
            }

            stream.Position = oldPosition;
            return result;
        }


        public void ProcessMessage(Stream stream)
        {
            Profiler.BeginSample("ProcessMessage");

            BinaryReader reader = new BinaryReader(data);
            byte msg = reader.ReadByte();
            uint size = reader.ReadUInt32();
            if (Enum.IsDefined(typeof(RemoteMessageID), (RemoteMessageID)msg))
            {
                switch ((RemoteMessageID)msg)
                {
                    case RemoteMessageID.Hello: HandleHello(reader); break;
                    case RemoteMessageID.ReadyToScreenStream: HandleReadyToScreenStream(reader); break;
                    case RemoteMessageID.ScreenStream: HandleScreenStream(reader); break;

                    // Due to internal enums being what they are, touch input corresponds to screen sctream for legacy path
                    // @TODO fix it;
                    case RemoteMessageID.TouchInput: HandleScreenStream(reader); break;
                    case RemoteMessageID.GyroSettings: HandleGyroSettings(reader); break;
                    case RemoteMessageID.ScreenOrientation: HandleScreenOrientation(reader); break;
                    case RemoteMessageID.WebCamStartStream: HandleWebCamStartStream(reader); break;
                    case RemoteMessageID.WebCamStopStream: HandleWebCamStopStream(reader); break;
                    case RemoteMessageID.LocationServiceStart: HandleLocationServiceStart(reader); break;
                    case RemoteMessageID.LocationServiceStop: HandleLocationServiceStop(reader); break;
                    case RemoteMessageID.CompassSetEnabled: HandleCompassSetEnabled(reader); break;

                    case RemoteMessageID.ARLightEstimation: HandleARLightEstimation(reader); break;
                    case RemoteMessageID.ARReferencePointAdd: HandleARReferencePointAdd(reader); break;
                    case RemoteMessageID.ARReferencePointRemove: HandleARReferencePointRemove(reader); break;
                    case RemoteMessageID.ARRemoteSettings: HandleARRemoteSettings(reader); break;

                }
            }
            else
            {
                //Console.WriteLine("Unknown message: " + msg);
                reader.ReadBytes((int)size);
            }

            Profiler.EndSample();
        }

        private void HandleARReferencePointRemove(BinaryReader reader)
        {
            ulong id1 = reader.ReadUInt64();
            ulong id2 = reader.ReadUInt64();

            IdToArray ids;
            ids.id0 = id1;
            ids.id1 = id2;

            byte[] t = DataSender.StructToByteArray<IdToArray>(ids);
            TrackableId id = DataSender.ByteArrayToType<TrackableId>(t);

            var result = ARSubsystemManager.referencePointSubsystem.TryRemoveReferencePoint(id);

            ARRemotePath.separateThreadDataSender.SendARReferenceRemoveResponse(result);
        }

        private void HandleARReferencePointAdd(BinaryReader reader)
        {
            float px = reader.ReadSingle();
            float py = reader.ReadSingle();
            float pz = reader.ReadSingle();
            float rx = reader.ReadSingle();
            float ry = reader.ReadSingle();
            float rz = reader.ReadSingle();
            float rw = reader.ReadSingle();

            TrackableId id;
            var result = ARSubsystemManager.referencePointSubsystem.TryAddReferencePoint(new Pose(new Vector3(px, py, pz), new Quaternion(rx, ry, rz, rw)), out id);

            ARRemotePath.separateThreadDataSender.SendARReferenceAddResponse(id, result, 1);
        }

        public struct ARRemoteSettings
        {
            public bool EnableARPreview { get; set; }
            public int ARPreviewScale { get; set; }
        }
        
        public Action<ARRemoteSettings> OnARRemoteSettingsReceived;
        private void HandleARRemoteSettings(BinaryReader reader)
        {
            ARRemoteSettings settings = new ARRemoteSettings();
            
            settings.EnableARPreview = reader.ReadBoolean();
            settings.ARPreviewScale = reader.ReadInt32();
            
            if(OnARRemoteSettingsReceived != null)
                OnARRemoteSettingsReceived.Invoke(settings);
        }
        
        private void HandleARLightEstimation(BinaryReader reader)
        {
            var lightEstimationRequest = reader.ReadBoolean();
            ARSubsystemManager.lightEstimationRequested = lightEstimationRequest;
        }


        public Action ReadyToScreenStreamCallback;
        public void HandleReadyToScreenStream(BinaryReader reader)
        {
            if(ReadyToScreenStreamCallback != null)
            {
                ReadyToScreenStreamCallback.Invoke();
            }
        }

        public bool isLegacyPath = false;

        public Action<bool> HelloMessageCallback;
        public void HandleHello(BinaryReader reader)
        {
            string magic = reader.ReadCustomString();
            if (magic != "UnityRemote")
                throw new ApplicationException("Handshake failed");

            uint version = reader.ReadUInt32();
            if (version != 10)
            {
                isLegacyPath = true;
            }

            if(HelloMessageCallback != null)
            {
                HelloMessageCallback.Invoke(isLegacyPath);
            }
        }

        public struct FrameInfo
        {
            public RenderTexture rt;
            public long FrameId;
        }


        public Queue<FrameInfo> frames = new Queue<FrameInfo>();

        bool waitingForFrame = true;
        
        public void SetAwaitingFrameId(long id, RenderTexture rt)
        {
            var fi = new FrameInfo();
            fi.FrameId = id;
            fi.rt = rt;

            frames.Enqueue(fi);
        }

        public Action handleScreenStream;
        byte[] image = null;
        byte[] imageTarget = null;
        int lastSize = -1;

        public void HandleScreenStream(BinaryReader reader)
        {
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
           
            int textureFormat = -1;
            bool enableCompression = false;
            
            if (!isLegacyPath)
            {
                //read frameID - @TODO Remove Frame ID when no longer necessary
                reader.ReadInt64();
//#if NET_4_6
                enableCompression = reader.ReadBoolean();
//#else
//                reader.ReadBoolean();
//#endif
                textureFormat = reader.ReadInt32();
            } 
            
            int size = reader.ReadInt32();
            int sizeOfArray = reader.ReadInt32();

            if (size != lastSize)
            {
                image = new byte[size];
                imageTarget = new byte[sizeOfArray];
                lastSize = size;
            }
            
            reader.Read(image, 0, size);
            
            Debug.Log("enableCompression : " + enableCompression);
            Debug.Log("size : " + size);
            Debug.Log("size of array : " + sizeOfArray);

            if (waitingForFrame && !isLegacyPath)
            {
                if (handleScreenStream != null)
                {
                    handleScreenStream();
                }

                Debug.Log("BEFORE LEGACY");
//#if NET_STANDARD_2_0
#if !NET_LEGACY
                Debug.Log("LEGACY");
                if(enableCompression)
                {
                    Debug.Log("COMPRESSION");
                    
                    var decoded = LZ4Codec.Decode(
                        image, 0, image.Length,
                        imageTarget, 0, imageTarget.Length);

                    Debug.Log(decoded);
                    
                    screen.UpdateScreen(imageTarget, width, height, textureFormat);
                }
                else
                {
                    Debug.Log("NO COMPRESSION");
                    screen.UpdateScreen(image, width, height, textureFormat);
                }
#endif
//#else
 //               screen.UpdateScreen(image, width, height, textureFormat);
//#endif
            }
            else
            {
                screen.UpdateScreen(image, width, height);
            }
        }


        public void HandleGyroSettings(BinaryReader reader)
        {
            Input.gyro.enabled = (reader.ReadInt32() != 0);
            float updateInterval = reader.ReadSingle();
            if (updateInterval != 0.0f && updateInterval != Input.gyro.updateInterval)
                Input.gyro.updateInterval = updateInterval;
        }


        public void HandleScreenOrientation(BinaryReader reader)
        {
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            /*
            Screen.orientation = (ScreenOrientation)reader.ReadInt32();
            Screen.autorotateToPortrait = reader.ReadInt32() != 0;
            Screen.autorotateToPortraitUpsideDown = reader.ReadInt32() != 0;
            Screen.autorotateToLandscapeLeft = reader.ReadInt32() != 0;
            Screen.autorotateToLandscapeRight = reader.ReadInt32() != 0;
            */
        }


        public void HandleWebCamStartStream(BinaryReader reader)
        {
            string device = reader.ReadCustomString();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int fps = reader.ReadInt32();
            webCamStreamer.StartStream(device, width, height, fps);
        }


        public void HandleWebCamStopStream(BinaryReader reader)
        {
            string device = reader.ReadCustomString();
            webCamStreamer.StopStream(device);
        }


        public void HandleLocationServiceStart(BinaryReader reader)
        {
            float desiredAccuracyInMeters = reader.ReadSingle();
            float updateDistanceInMeters = reader.ReadSingle();
            Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        }


        public void HandleLocationServiceStop(BinaryReader reader)
        {
            Input.location.Stop();
        }


        public void HandleCompassSetEnabled(BinaryReader reader)
        {
            bool enabled = reader.ReadBoolean();
            Input.compass.enabled = enabled;
        }
    }
}