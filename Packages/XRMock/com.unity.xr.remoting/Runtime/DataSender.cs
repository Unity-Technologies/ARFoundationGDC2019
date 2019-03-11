using CommonRemoting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;

namespace ClientRemoting
{
    public enum RemoteMessage : byte
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

        WebCamDeviceList = 20,
        WebCamStream = 21,

        LocationServiceData = 30,
        CompassData = 31,
        CompassSetEnabled = 32,

        ARCameraData = 33,
        ARPlaneData = 34,
        ARPointCloudData = 35,
        ARCameraFrameData = 36,
        ARTrackingStateChanged = 37,

        ARReferencePointAddResponse = 102,
        ARReferencePointRemoveResponse = 104,

        CustomData = 254,
        Reserved = 255,
    }

    public enum CustomDataID : byte
    {
        InputEvent = 100
    }


    public class PacketWriter
    {
        BinaryWriter writer;
        MemoryStream packet;
        RemoteMessage message;
        byte[] buffer = new byte[10 * 1024 * 1024];

        public void BeginMessage(RemoteMessage message)
        {
            //SDebug.Assert(message == RemoteMessage.Invalid);

            this.message = message;
            packet.Position = 0;
            packet.SetLength(0);
        }

        public void EndMessage(Stream stream)
        {
            //SDebug.Assert(message != RemoteMessage.Invalid);

            // Write message header
            stream.WriteByte((byte)message);
            uint len = (uint)packet.Length;
            stream.WriteByte((byte)(len & 0xFF));
            stream.WriteByte((byte)((len >> 8) & 0xFF));
            stream.WriteByte((byte)((len >> 16) & 0xFF));
            stream.WriteByte((byte)((len >> 24) & 0xFF));

            // Write the message
            packet.Position = 0;
            Utils.CopyToStream(packet, stream, buffer, (int)packet.Length);

            message = RemoteMessage.Invalid;
        }

        public void Write(bool value) { writer.Write(value); }
        public void Write(byte value) { writer.Write(value); }
        public void Write(int value) { writer.Write(value); }
        public void Write(uint value) { writer.Write(value); }
        public void Write(long value) { writer.Write(value); }
        public void Write(ulong value) { writer.Write(value); }
        public void Write(float value) { writer.Write(value); }
        public void Write(double value) { writer.Write(value); }
        public void Write(byte[] value) { writer.Write(value); }

        public void Write(string value)
        {
            writer.Write((uint)value.Length);
            writer.Write(Encoding.UTF8.GetBytes(value));
        }

        public PacketWriter()
        {
            packet = new MemoryStream();
            writer = new BinaryWriter(packet);
            message = RemoteMessage.Invalid;
        }
    }


    public struct OldLocationData
    {
        public bool isEnabledByUser;
        public LocationServiceStatus status;
        public LocationInfo lastData;
    }


    public struct OldCompassData
    {
        public bool enabled;
        public float magneticHeading;
        public float trueHeading;
        public float headingAccuracy;
        public Vector3 rawVector;
        public double timestamp;
    }


    public class DataSender
    {
        const int MAX_AXES = 28;
        const int MAX_JOYSTICKS = 9;

        PacketWriter writer;
        Stream stream;

        OldLocationData? oldLocationData;
        OldCompassData? oldCompassData;
        string[] oldJoystickNames = { };
        uint[] oldJoystickButtons = new uint[MAX_JOYSTICKS];
        float[,] oldJoystickAxes = new float[MAX_JOYSTICKS, MAX_AXES];

        public void SendReadyToStream(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.ReadyToScreenStream);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendHello(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.Hello);
            writer.Write("UnityRemote");
            writer.Write((uint)0);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendOptions(IConnectionProvider connectionProvider)
        {
            // Add Screen size information
            // TODO: only send when changed
            writer.BeginMessage(RemoteMessage.Options);
            writer.Write(Screen.width);
            writer.Write(Screen.height);
            writer.Write((int)Screen.orientation);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }
        
        public void SendDeviceOrientation(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.DeviceOrientation);
            writer.Write((int)Screen.orientation);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendAccelerometerInput(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.AccelerometerInput);
            writer.Write(Input.acceleration.x);
            writer.Write(Input.acceleration.y);
            writer.Write(Input.acceleration.z);
            writer.Write(Time.deltaTime);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendGyroscopeSettings(IConnectionProvider connectionProvider)
        {
            Gyroscope gyro = Input.gyro;
            writer.BeginMessage(RemoteMessage.GyroSettings);
            writer.Write(gyro.enabled ? 1 : 0);
            writer.Write(gyro.updateInterval);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendGyroscopeInput(IConnectionProvider connectionProvider)
        {
            // TODO: check updateInterval here..
            Gyroscope gyro = Input.gyro;
            writer.BeginMessage(RemoteMessage.GyroInput);
            writer.Write(gyro.rotationRate.x);
            writer.Write(gyro.rotationRate.y);
            writer.Write(gyro.rotationRate.z);
            writer.Write(gyro.rotationRateUnbiased.x);
            writer.Write(gyro.rotationRateUnbiased.y);
            writer.Write(gyro.rotationRateUnbiased.z);
            writer.Write(gyro.gravity.x);
            writer.Write(gyro.gravity.y);
            writer.Write(gyro.gravity.z);
            writer.Write(gyro.userAcceleration.x);
            writer.Write(gyro.userAcceleration.y);
            writer.Write(gyro.userAcceleration.z);
            writer.Write(gyro.attitude.x);
            writer.Write(gyro.attitude.y);
            writer.Write(gyro.attitude.z);
            writer.Write(gyro.attitude.w);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendInputData(IConnectionProvider connectionProvider, byte[] data, int deviceId, string fourCC)
        {
            writer.BeginMessage(RemoteMessage.CustomData);
            writer.Write((int)CustomDataID.InputEvent);
            writer.Write(deviceId);
            writer.Write(fourCC);;
        }

        public void SendTouchInput(IConnectionProvider connectionProvider, int ph, float x, float y)
        {
            writer.BeginMessage(RemoteMessage.CustomData);
            writer.Write((int)CustomDataID.InputEvent+1);
            writer.Write(ph);
            writer.Write(x);
            writer.Write(y);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }
        
        internal void SendCustomData(IConnectionProvider connectionProvider, Func<PacketWriter, object> p, int v)
        {
            writer.BeginMessage(RemoteMessage.CustomData);
            writer.Write(v);
            p(writer);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendCustomData(IConnectionProvider connectionProvider, byte[] data, int id)
        {
            writer.BeginMessage(RemoteMessage.CustomData);
            writer.Write(id);
            writer.Write(data.Length);
            writer.Write(data);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendTouchInput(IConnectionProvider connectionProvider)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                writer.BeginMessage(RemoteMessage.TouchInput);
                writer.Write(touch.position.x);
                writer.Write(touch.position.y);
                writer.Write((long)Time.frameCount);
                writer.Write(touch.fingerId);
                writer.Write((int)touch.phase);
                writer.Write((int)touch.tapCount);

                // Added in Unity 5.4
                writer.Write(touch.radius);
                writer.Write(touch.radiusVariance);
                writer.Write((int)touch.type);
                writer.Write(touch.pressure);
                writer.Write(touch.maximumPossiblePressure);
                writer.Write(touch.azimuthAngle);
                writer.Write(touch.altitudeAngle);

                writer.EndMessage(stream);
            }

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendDeviceFeatures(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.DeviceFeatures);
            writer.Write(Input.touchPressureSupported);
            writer.Write(Input.stylusTouchSupported);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        // joystick/axis number byte incorporates flags to differentiate between joysticks/axes and if joystick buttons are sent
        const byte JOYSTICK_FLAG = 0x80;
        const byte BUTTONS_FLAG = 0x40;
        const byte JOYSTICK_NUMBER_MASK = 0x0F;
        const byte AXIS_NUMBER_MASK = 0x1F;

        private void WriteJoystick(int joystick)
        {
            uint buttonBits = 0;
            string strJoyNum = (joystick != 0 ? joystick.ToString() : "");
            int firstCode = (int)System.Enum.Parse(typeof(KeyCode), "Joystick" + strJoyNum + "Button0");
            int lastCode = (int)System.Enum.Parse(typeof(KeyCode), "Joystick" + strJoyNum + "Button19");

            for (int intCode = firstCode; intCode <= lastCode; intCode++)
            {
                if (Input.GetKey((KeyCode)intCode))
                    buttonBits |= (1u << (intCode - firstCode));
            }

            bool writeButtons = (buttonBits != oldJoystickButtons[joystick]);
            bool joystickSent = false;

            oldJoystickButtons[joystick] = buttonBits;

            if (writeButtons)
            {
                joystickSent = true;
                writer.Write((byte)(JOYSTICK_FLAG | BUTTONS_FLAG | (joystick & JOYSTICK_NUMBER_MASK)));
                writer.Write(buttonBits);
            }

            for (byte axis = 0; axis < MAX_AXES; axis++)
            {
                float value = Input.GetAxis("j" + joystick + "a" + axis);
                if (oldJoystickAxes[joystick, axis] != value)
                {
                    oldJoystickAxes[joystick, axis] = value;
                    if (!joystickSent)
                    {
                        joystickSent = true;
                        writer.Write((byte)(JOYSTICK_FLAG | (joystick & JOYSTICK_NUMBER_MASK)));
                    }
                    writer.Write((byte)(axis & AXIS_NUMBER_MASK));
                    writer.Write(value);
                }
            }
        }


        public void SendJoystickInput(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.JoystickInput);

            for (int joystick = 0; joystick < MAX_JOYSTICKS; joystick++)
            {
                WriteJoystick(joystick);
            }

            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendJoystickNames(IConnectionProvider connectionProvider)
        {
            string[] joystickNames = Input.GetJoystickNames();

            if (joystickNames.Length == 0 && oldJoystickNames.Length == 0)
                return;

            if (!Enumerable.SequenceEqual(oldJoystickNames, joystickNames))
            {
                oldJoystickNames = joystickNames;

                writer.BeginMessage(RemoteMessage.JoystickNames);
                writer.Write((byte)joystickNames.Length);

                foreach (string joystickName in joystickNames)
                {
                    writer.Write(joystickName);
                }

                writer.EndMessage(stream);
            }

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendWebCamDeviceList(IConnectionProvider connectionProvider, RemoteWebCamDevice[] devices)
        {
            writer.BeginMessage(RemoteMessage.WebCamDeviceList);
            writer.Write((uint)devices.Length);
            foreach (var device in devices)
            {
                writer.Write(device.device.isFrontFacing);
                writer.Write(device.name);

            }
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendWebCamStream(string name, int width, int height, byte[] image, int angle, bool verticallyMirrored, IConnectionProvider connectionProvider)
        {
            writer.BeginMessage(RemoteMessage.WebCamStream);
            writer.Write(name);
            writer.Write((uint)width);
            writer.Write((uint)height);
            writer.Write(angle);
            writer.Write(verticallyMirrored);
            writer.Write((uint)image.Length);
            writer.Write(image);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendLocationServiceData(IConnectionProvider connectionProvider)
        {
            var data = new OldLocationData
            {
                isEnabledByUser = Input.location.isEnabledByUser,
                status = Input.location.status,
                lastData = Input.location.status == LocationServiceStatus.Running ? Input.location.lastData : default(LocationInfo)
            };

            if (oldLocationData.HasValue && (oldLocationData.Value.Equals(data)))
                return;

            writer.BeginMessage(RemoteMessage.LocationServiceData);
            writer.Write(data.isEnabledByUser);
            writer.Write((int)data.status);
            writer.Write(data.lastData.timestamp);
            writer.Write(data.lastData.latitude);
            writer.Write(data.lastData.longitude);
            writer.Write(data.lastData.altitude);
            writer.Write(data.lastData.horizontalAccuracy);
            writer.Write(data.lastData.verticalAccuracy);
            writer.EndMessage(stream);

            oldLocationData = data;

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        public void SendCompassData(IConnectionProvider connectionProvider)
        {
            var compass = Input.compass;
            var data = new OldCompassData
            {
                enabled = compass.enabled,
                magneticHeading = compass.magneticHeading,
                trueHeading = compass.trueHeading,
                headingAccuracy = 0.0f,
                rawVector = compass.rawVector,
                timestamp = compass.timestamp,
            };

            if (oldCompassData.HasValue && (oldCompassData.Value.Equals(data)))
                return;

            writer.BeginMessage(RemoteMessage.CompassData);
            writer.Write(data.enabled);
            writer.Write(data.magneticHeading);
            writer.Write(data.trueHeading);
            writer.Write(data.headingAccuracy);
            writer.Write(data.rawVector.x);
            writer.Write(data.rawVector.y);
            writer.Write(data.rawVector.z);
            writer.Write(data.timestamp);
            writer.EndMessage(stream);

            oldCompassData = data;

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendCameraData(IConnectionProvider connectionProvider, Camera camera, DateTime time)
        {
            writer.BeginMessage(RemoteMessage.ARCameraData);

            writer.Write(camera.transform.position.x);
            writer.Write(camera.transform.position.y);
            writer.Write(camera.transform.position.z);

            writer.Write(camera.transform.rotation.x);
            writer.Write(camera.transform.rotation.y);
            writer.Write(camera.transform.rotation.z);
            writer.Write(camera.transform.rotation.w);

            for (int i = 0; i < 16; i++)
            {
                writer.Write(camera.projectionMatrix[0]);
            }

            writer.Write(camera.fieldOfView);

            writer.Write(time.Ticks);

            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct PlaneData
        {
            public int eventType;
            public int eventSubType;

            public float x, y, z;

            public float px, py, pz;
            public float rx, ry, rz, rw;

            public float bx, by;

            public ulong id1;
            public ulong id2;
        };

        readonly Guid planeEventID = new Guid("f57938cb196b4aa082c9f03c3bcb3246");

        public void SendPlane(IConnectionProvider connectionProvider, PlaneData planeData)
        {
            connectionProvider.SendMessage(StructToByteArray<PlaneData>(planeData), planeEventID);
        }
        
        public void SendPlane(IConnectionProvider connectionProvider, int type, BoundedPlane Plane)
        {
            writer.BeginMessage(RemoteMessage.ARPlaneData);

            writer.Write((DateTime.UtcNow - ARRemotePath.connectedTime).TotalSeconds);

            writer.Write(Plane.Center.x);
            writer.Write(Plane.Center.y);
            writer.Write(Plane.Center.z);

            writer.Write(Plane.Pose.position.x);
            writer.Write(Plane.Pose.position.y);
            writer.Write(Plane.Pose.position.z);

            writer.Write(Plane.Pose.rotation.x);
            writer.Write(Plane.Pose.rotation.y);
            writer.Write(Plane.Pose.rotation.z);
            writer.Write(Plane.Pose.rotation.w);

            writer.Write(Plane.Size.x);
            writer.Write(Plane.Size.y);

            writer.Write((int)Plane.Alignment);

            writer.Write(type);

            byte[] t = StructToByteArray<TrackableId>(Plane.Id);
            IdToArray idToArray = ByteArrayToType<IdToArray>(t);

            writer.Write(idToArray.id0);
            writer.Write(idToArray.id1);

            byte[] subsumedToByte = StructToByteArray<TrackableId>(Plane.SubsumedById);
            IdToArray subsumedToArray = ByteArrayToType<IdToArray>(subsumedToByte);

            writer.Write(subsumedToArray.id0);
            writer.Write(subsumedToArray.id1);


            writer.EndMessage(stream);

            connectionProvider.SendMessage(stream);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ReferencePointUpdateID
        {
            public int eventType;
            public int eventSubType;

            public ulong id1;
            public ulong id2;

            public int trackingState;

            public float px, py, pz;
            public float rx, ry, rz, rw;

            public int previousTrackingState;

            public float ppx, ppy, ppz;
            public float prx, pry, prz, prw;
        };

        readonly Guid referencePointUpdateID = new Guid("d84cbacbf052409a9fa50bea92968fce");

        public void SendReferenePointUpdate(IConnectionProvider connectionProvider, ReferencePointUpdateID data)
        {
            connectionProvider.SendMessage(StructToByteArray<ReferencePointUpdateID>(data), referencePointUpdateID);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PointCLoudUpdated
        {
            public int size;
            public Vector3[] points;
        };

        readonly Guid pointCloudUpdatedID = new Guid("c66792c5a24a428ab424b51f42e9267c");

        public void SendPointCloudUpdated(IConnectionProvider connectionProvider, List<Vector3> points)
        {
            writer.BeginMessage(RemoteMessage.ARPointCloudData);

            writer.Write(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                writer.Write(points[i].x);
                writer.Write(points[i].y);
                writer.Write(points[i].z);
            }

            writer.EndMessage(stream);


            connectionProvider.SendMessage(stream);
        }

        public void SendPointCloudUpdated(IConnectionProvider connectionProvider, PointCLoudUpdated data)
        {
            int size = sizeof(int) + (sizeof(float) * data.size * 3);
            byte[] buffer = new byte[size];

            MemoryStream stream = new MemoryStream(buffer);
            BinaryWriter reader = new BinaryWriter(stream);

            reader.Write(data.size);

            foreach (var p in data.points)
            {
                reader.Write(p.x);
                reader.Write(p.y);
                reader.Write(p.z);
            }

            reader.Close();

            connectionProvider.SendMessage(buffer, pointCloudUpdatedID);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CameraFrameReceived
        {
            public int eventType;
            public int eventSubType;

            public Int64 timeStampNs;
            public float averageBrightness;
            public float averageTemperature;

            public Matrix4x4 projectionMatrix;
            public Matrix4x4 displayMatrix;

            public byte cameraFramePropertyFlags;
        };

        readonly Guid frameReceivedID = new Guid("07ae97033436498baf52de2e2bc7f47c");

        Matrix4x4 dm = Matrix4x4.identity;
        public void SendFrameReceived(IConnectionProvider connectionProvider, ARCameraFrameEventArgs frameEventArgs)
        {
            if (ARSubsystemManager.cameraSubsystem == null)
            {
                return;
            }
            
            writer.BeginMessage(RemoteMessage.ARCameraFrameData);

            writer.Write(frameEventArgs.lightEstimation.averageBrightness.GetValueOrDefault());
            writer.Write(frameEventArgs.lightEstimation.averageColorTemperature.GetValueOrDefault());
            writer.Write(frameEventArgs.time.GetValueOrDefault());

            var gotDisplayMatrix = ARSubsystemManager.cameraSubsystem.TryGetDisplayMatrix(ref dm);

            writer.Write(gotDisplayMatrix);

            if (gotDisplayMatrix)
            {
                for (int i = 0; i < 16; i++)
                {
                    writer.Write(dm[i]);
                }
            }

            Matrix4x4 pm = Matrix4x4.identity;
            var gotProjectionMatrix = ARSubsystemManager.cameraSubsystem.TryGetProjectionMatrix(ref pm);

            writer.Write(gotProjectionMatrix);

            if (gotProjectionMatrix)
            {
                for (int i = 0; i < 16; i++)
                {
                    writer.Write(pm[i]);
                }
            }


            byte fieldMask = 0;

            if (frameEventArgs.time.HasValue)
                fieldMask |= 1 << 0;
            if (frameEventArgs.lightEstimation.averageBrightness.HasValue)
                fieldMask |= 1 << 1;
            if (frameEventArgs.lightEstimation.averageColorTemperature.HasValue)
                fieldMask |= 1 << 2;
            if (gotProjectionMatrix)
                fieldMask |= 1 << 3;
            if (gotDisplayMatrix)
                fieldMask |= 1 << 4;

            writer.Write(fieldMask);

            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendFrameReceived(IConnectionProvider connectionProvider, CameraFrameReceived data)
        {
            connectionProvider.SendMessage(StructToByteArray<CameraFrameReceived>(data), frameReceivedID);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TrackingStateChanged
        {
            public int trackingState;
        };

        readonly Guid trackingStateChanged = new Guid("1d68eb96b09a4e43900704542327d9c3");

        public void SendTrackingStateChanged(IConnectionProvider connectionProvider, int data)
        {
            writer.BeginMessage(RemoteMessage.ARTrackingStateChanged);

            writer.Write(data);

            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        public void SendTrackingStateChanged(IConnectionProvider connectionProvider, TrackingStateChanged data)
        {
            connectionProvider.SendMessage(StructToByteArray<TrackingStateChanged>(data), trackingStateChanged);
        }

        #region Helpers
        static byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        public static byte[] StructToByteArray<T>(T data)
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;// Compress(arr);
        }

        public static byte[] StructToByteArray<T>(T data, int size)
        {
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;// Compress(arr);
        }

        public static T ByteArrayToType<T>(byte[] d) where T : struct
        {
            T returnValue = new T();

            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            var data = d;

            Marshal.Copy(data, 0, ptr, size);

            returnValue = (T)Marshal.PtrToStructure(ptr, returnValue.GetType());
            Marshal.FreeHGlobal(ptr);

            return returnValue;
        }
        #endregion

        public DataSender()
        {
            const int StreamBufferSize = 10 * 768 * 1024;
            stream = new MemoryStream(StreamBufferSize);
            writer = new PacketWriter();
        }

        public void Reset()
        {
            stream.Position = 0;
            stream.SetLength(0);
        }

        public DataSender(Stream stream)
        {
            this.stream = stream;
            writer = new PacketWriter();
        }



        public void SendARReferenceAddResponse(TrackableId id, bool result, int trackingState)
        {
            writer.BeginMessage(RemoteMessage.ARReferencePointAddResponse);


            IdToArray idToArray;
            unsafe
            {
                var ptr = (TrackableId*)&id;
                idToArray = *(IdToArray*)ptr;
            };

            writer.Write(idToArray.id0);
            writer.Write(idToArray.id1);

            writer.Write(trackingState);
            writer.Write(result);

            writer.EndMessage(stream);

            ARRemotePath.connectionProviderDefferredCalls.SendMessage(stream);
        }

        public void SendARReferenceRemoveResponse(bool response)
        {
            writer.BeginMessage(RemoteMessage.ARReferencePointRemoveResponse);
            writer.Write(response);
            writer.EndMessage(stream);

            ARRemotePath.connectionProviderDefferredCalls.SendMessage(stream);
        }
    }
}