using ClientRemoting;
using CommonRemoting;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace EditorRemoting
{
    public partial class DataReceiver
    {
        protected MemoryStream data = new MemoryStream();
        protected byte[] buffer = new byte[1024*1024 * 2];

        List<IMockProxy> m_proxies;

        List<Vector3> points = new List<Vector3>();
        BoundedPlaneData plane;

        CameraFrameData frameData;

        public Camera cameraMain;
        
        public DataReceiver(List<IMockProxy> mockProxy)
        {
            m_proxies = mockProxy;
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

        public void ClearData()
        {
            data.Position = 0;
            data.SetLength(0);
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

            BinaryReader reader = new BinaryReader(stream);
            byte msg = reader.ReadByte();
            uint size = reader.ReadUInt32();

            if (Enum.IsDefined(typeof(RemoteMessageID), (RemoteMessageID)msg))
            {
                switch ((RemoteMessageID)msg)
                {
                    case RemoteMessageID.Hello: HandleHello(reader); break;
                    case RemoteMessageID.ReadyToScreenStream: HandleReadyToScreenStream(reader); break;
                    case RemoteMessageID.ScreenStream: HandleScreenStream(reader); break;
                    case RemoteMessageID.Options: HandleOptions(reader); break;
                    case RemoteMessageID.GyroSettings: HandleGyroSettings(reader); break;
                    case RemoteMessageID.DeviceOrientation: HandleDeviceOrientation(reader); break;
                    case RemoteMessageID.DeviceFeatures: HandleDeviceFeatures(reader); break;
                    case RemoteMessageID.ScreenOrientation: HandleScreenOrientation(reader); break;
                    case RemoteMessageID.WebCamStartStream: HandleWebCamStartStream(reader); break;
                    case RemoteMessageID.WebCamStopStream: HandleWebCamStream(reader); break;
                    case RemoteMessageID.LocationServiceStart: HandleLocationServiceStart(reader); break;
                    case RemoteMessageID.AccelerometerInput: HandleAccelerometerInput(reader); break;
                    case RemoteMessageID.TrackBallInput: HandleTrackBallInput(reader); break;
                    case RemoteMessageID.Key: HandleKey(reader); break;
                    case RemoteMessageID.GyroInput: HandleGyroInput(reader); break;
                    case RemoteMessageID.MousePresence: HandleMousePresence(reader); break;
                    case RemoteMessageID.JoystickInput: HandleJoystickInput(reader); break;
                    case RemoteMessageID.JoystickNames: HandleJoystickNames(reader); break;
                    case RemoteMessageID.LocationServiceStop: HandleLocationServiceStop(reader); break;
                    case RemoteMessageID.CompassSetEnabled: HandleCompassSetEnabled(reader); break;

                    case RemoteMessageID.ARCameraData: HandleARCameraData(reader); break;
                    case RemoteMessageID.ARPlaneData: HandleARPlaneData(reader); break;
                    case RemoteMessageID.ARPointCloudData: HandlePointCloudData(reader); break;
                    case RemoteMessageID.ARCameraFrameData: HandleCameraFrameData(reader); break;
                    case RemoteMessageID.ARTrackingStateChanged: HandleTrackingStateChanged(reader); break;

                    case RemoteMessageID.CustomData: HandleCustomData(reader); break;
                }
            }
            else
            {
                reader.ReadBytes((int)size);
            }

            Profiler.EndSample();
        }

#if XRREMOTING_USE_NEW_INPUT_SYSTEM
        public static unsafe void Process(InputRemoting.Message msg, int deviceId, FourCC type)
        {
            fixed (byte* dataPtr = msg.data)
            {
                var dataEndPtr = new IntPtr(dataPtr + msg.data.Length);
                var eventCount = 0;
                var eventPtr = new InputEventPtr((InputEvent*)dataPtr);

                while (eventPtr.data.ToInt64() < dataEndPtr.ToInt64())
                {
                    eventPtr.deviceId = deviceId;

                    InputSystem.QueueEvent(eventPtr);

                    ++eventCount;
                    eventPtr = eventPtr.Next();
                }

                InputSystem.Update();
            }
        }
#endif

        public Action<CustomDataEvent> OnCustomDataReceived;

        private void HandleCustomData(BinaryReader reader)
        {
            var customDataID = reader.ReadInt32();
            
            var eventData = new CustomDataEvent()
            {
                EventId = customDataID,
                BinaryReader = reader,
                Handled = false
            };

            if(OnCustomDataReceived != null)
                OnCustomDataReceived.Invoke(eventData);
        }

        public Action<int, int, int, bool> OnOptionsReceived;
        private void HandleOptions(BinaryReader reader)
        {
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();

            int orientation = reader.ReadInt32();

            if (OnOptionsReceived != null)
                OnOptionsReceived.Invoke(w, h, orientation, true);
        }

        private void HandleTrackingStateChanged(BinaryReader reader)
        {
            var trackingValue = reader.ReadInt32();

			foreach(var proxy in m_proxies)
			{
                proxy.UpdateTrackingState(trackingValue);
			}
        }

        private void HandleCameraFrameData(BinaryReader reader)
        {
            var averageBrithness = reader.ReadSingle();
            var averageColor = reader.ReadSingle();
            var timeStamp = reader.ReadSingle();

            Matrix4x4 dm = Matrix4x4.identity;

            var gotDisplayMatrix = reader.ReadBoolean();

            if (gotDisplayMatrix)
            {
                for (int i = 0; i < 16; i++)
                {
                    dm[i] = reader.ReadSingle();
                }
            }

            Matrix4x4 pm = Matrix4x4.identity;
            var gotProjectionMatrix = reader.ReadBoolean();

            if (gotProjectionMatrix)
            {
                for (int i = 0; i < 16; i++)
                {
                    pm[i] = reader.ReadSingle();
                }
            }

            float t = pm[1,1];
            const float Rad2Deg = 180.0f / Mathf.PI;
            float fov = Mathf.Atan(1.0f / t) * 2.0f * Rad2Deg;

            cameraMain.fieldOfView = fov;

            frameData.averageBrightness = averageBrithness;
            frameData.averageTemperature = averageColor;
            frameData.timeStampNs = (long)timeStamp;
            frameData.displayMatrix = dm;
            frameData.projectionMatrix = pm;
            frameData.eventSubType = 0;
            frameData.eventType = 0;
            frameData.cameraFramePropertyFlags = reader.ReadByte();

            foreach(var proxy in m_proxies)
			{
            	proxy.UpdateFrameData(frameData);
			}
        }

        private void HandlePointCloudData(BinaryReader reader)
        {
            var size = reader.ReadInt32();
            
            if (size > 0)
            {
                points.Clear();
            }

            for (int i = 0; i < size; i++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();
                points.Add(new Vector3(x, y, z));
            }
            
            foreach(var proxy in m_proxies)
			{
            	proxy.ProcessDepthData(points);
			}
        }
        
        private void HandleARPlaneData(BinaryReader reader)
        {
            //@TODO Remove from the contract - timeStamp
            reader.ReadDouble();
            
            Vector3 center;
            center.x = reader.ReadSingle();
            center.y = reader.ReadSingle();
            center.z = reader.ReadSingle();
            plane.Center = center;

            Vector3 position;
            position.x = reader.ReadSingle();
            position.y = reader.ReadSingle();
            position.z = reader.ReadSingle();
            plane.Position = position;

            Quaternion rotation;
            rotation.x = reader.ReadSingle();
            rotation.y = reader.ReadSingle();
            rotation.z = reader.ReadSingle();
            rotation.w = reader.ReadSingle();
            plane.Rotation = rotation;

            Vector2 size;
            size.x = reader.ReadSingle();
            size.y = reader.ReadSingle();
            plane.Size = size;

            plane.Alignment = reader.ReadInt32();

            plane.eventType = reader.ReadInt32();

            plane.id0 = reader.ReadUInt64();
            plane.id1 = reader.ReadUInt64();

            plane.sid0 = reader.ReadUInt64();
            plane.sid1 = reader.ReadUInt64();

            foreach(var proxy in m_proxies)
			{
            	proxy.ProcessPlane(plane);
			}
        }

        private void HandleARCameraData(BinaryReader reader)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            position.x = reader.ReadSingle();
            position.y = reader.ReadSingle();
            position.z = reader.ReadSingle();

            rotation.x = reader.ReadSingle();
            rotation.y = reader.ReadSingle();
            rotation.z = reader.ReadSingle();
            rotation.w = reader.ReadSingle();

            Matrix4x4 projectionMatrix = Matrix4x4.identity;

            for (int i = 0; i < 16; i++)
            {
                projectionMatrix[i] = reader.ReadSingle();
            }

            //@TODO - Remove from the contract - fov
            reader.ReadSingle();

            long tickId = reader.ReadInt64();
            
            EditorDataSender.frameIds.Enqueue(tickId);

            foreach(var proxy in m_proxies)
			{
            	proxy.ProcessCameraPose(position, rotation);
			}
        }

        public void HandleDeviceOrientation(BinaryReader reader)
        {
            var orientationData = reader.ReadInt32();
            Screen.orientation = (ScreenOrientation)orientationData;
        }

        public void HandleAccelerometerInput(BinaryReader reader)
        {
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
        }
        public void HandleTrackBallInput(BinaryReader reader)
        {
        }
        public void HandleKey(BinaryReader reader)
        {
        }
        public void HandleGyroInput(BinaryReader reader)
        {
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
        }
        public void HandleMousePresence(BinaryReader reader)
        {
        }

        public void HandleJoystickInput(BinaryReader reader)
        {
            while (reader.BaseStream.Length >= 5)
            {
                reader.ReadByte();
            }

            for (int joystick = 0; joystick < 9; joystick++)
            {
                reader.ReadByte();
                reader.ReadUInt32();

                for (int axisX = 0; axisX < 28; axisX++)
                {
                    reader.ReadByte();
                    reader.ReadUInt32();
                }
            }
        }
        public void HandleJoystickNames(BinaryReader reader)
        {
            byte size = reader.ReadByte();
            for (byte i = 0; i < size; i++)
            {
                reader.ReadCustomString();
            }

        }

        public void HandleHello(BinaryReader reader)
        {
            string magic = reader.ReadCustomString();
            if (magic != "UnityRemote")
                throw new ApplicationException("Handshake failed");

            uint version = reader.ReadUInt32();
            if (version != 0)
                throw new ApplicationException("Unsupported protocol version: " + version);
        }

        public Action ReadyToScreenStream;
        public void HandleReadyToScreenStream(BinaryReader reader)
        {
            if(ReadyToScreenStream != null)
            {
                ReadyToScreenStream.Invoke();
            }
        }

        public void HandleScreenStream(BinaryReader reader)
        {
        }

        public void HandleDeviceFeatures(BinaryReader reader)
        {
            reader.ReadBoolean();
            reader.ReadBoolean();
        }

        public void HandleGyroSettings(BinaryReader reader)
        {
            var gyroEnabled = (reader.ReadInt32() != 0);
            float updateInterval = reader.ReadSingle();

            Input.gyro.enabled = gyroEnabled;

            if (updateInterval != 0.0f && updateInterval != Input.gyro.updateInterval)
                Input.gyro.updateInterval = updateInterval;
        }


        public void HandleScreenOrientation(BinaryReader reader)
        {
            var orientation = (ScreenOrientation)reader.ReadInt32();
            var autorotateToPortrait = reader.ReadInt32() != 0;
            var autorotateToPortraitUpsideDown = reader.ReadInt32() != 0;
            var autorotateToLandscapeLeft = reader.ReadInt32() != 0;
            var autorotateToLandscapeRight = reader.ReadInt32() != 0;

            Screen.orientation = orientation;
            Screen.autorotateToPortrait = autorotateToPortrait;
            Screen.autorotateToPortraitUpsideDown = autorotateToPortraitUpsideDown;
            Screen.autorotateToLandscapeLeft = autorotateToLandscapeLeft;
            Screen.autorotateToLandscapeRight = autorotateToLandscapeRight;
        }

        public Material mat;
        Texture2D backgroundTexture;

        public void HandleWebCamStream(BinaryReader reader)
        {
            reader.ReadCustomString(); //name
            reader.ReadUInt32(); // width
            reader.ReadUInt32(); // height
            reader.ReadInt32(); // angle
            reader.ReadBoolean(); // verticallyMirrored
            int size = (int)reader.ReadUInt32();

            reader.ReadBytes(size); // image

            /*
            if(backgroundTexture == null)
            {
                backgroundTexture = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
            }

            backgroundTexture.LoadImage(image);
            renderer.backgroundTexture = backgroundTexture;

            var o = GameObject.FindGameObjectWithTag("Player");
            o.GetComponent<Renderer>().material.mainTexture = backgroundTexture;

            ARSubsystemManager.cameraSubsystem.TrySetupARBackgroundRenderer(renderer, mat);

            */
        }

        //@TODO Implement WebCamStream
        public static string deviceName = "";
        public static uint w = 0;
        public static uint h = 0;
        public static int fpg = -1;

        public void HandleWebCamStartStream(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            for (int i = 0; i < size; i++)
            {
                reader.ReadBoolean(); // isFrontFacing

                string device = reader.ReadCustomString();
                // uint width = reader.ReadUInt32();
                //uint height = reader.ReadUInt32();
                //int fps = reader.ReadInt32();

                deviceName = device;
                w = 400;// width;
                h = 400;// height;
                fpg = 25;// fps;
            }
            /*
                int index = WebCamStreamIndex(m_WebCamStreams, name);
                if (index < 0)
                    return;

                Texture2D* texture = CreateObjectFromCode<Texture2D>();
                texture->SetHideFlags(Object::kHideAndDontSave);
                bool loaded;
                {
                    PROFILER_AUTO(gLoadJpg, NULL);
                    loaded = LoadMemoryBufferIntoTexture(*texture, image, size, kLoadImageUncompressed);
                }
                if (loaded)
                    m_WebCamStreams[index].texture->UpdateFrom(*texture, angle, verticallyMirrored);
                    */
        }

        public void HandleLocationServiceStart(BinaryReader reader)
        {
            float desiredAccuracyInMeters = reader.ReadSingle();
            float updateDistanceInMeters = reader.ReadSingle();
            //ThreadUtils.dispatcher.ExecuteOnMainThread(() =>
            {
                Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
            }
            //);
        }


        public void HandleLocationServiceStop(BinaryReader reader)
        {
            //ThreadUtils.dispatcher.ExecuteOnMainThread(() =>
            {
                Input.location.Stop();
            }
            //);
        }


        public void HandleCompassSetEnabled(BinaryReader reader)
        {
            bool enabled = reader.ReadBoolean();
            //ThreadUtils.dispatcher.ExecuteOnMainThread(() =>
            {
                Input.compass.enabled = enabled;
            }
            //);
        }
    }
}
