using UnityEngine;
using UnityEngine.Profiling;

namespace ClientRemoting
{
    public class LegacyRemotePath : RemotePath
    {
        public DataSender dataSender;
        IConnectionProvider connectionProvider;
        private bool defferedInit = false;
        ScreenStream screenStreamer;
        WebCamStreamer webCamStreamer;

        public void Start(IConnectionProvider standardConnectionProvider, ScreenStream screen)
        {
            connectionProvider = standardConnectionProvider;
            screenStreamer = screen;

            webCamStreamer = new WebCamStreamer();
        }

        private void DefferedInit()
        {
            if (!defferedInit)
            {
                defferedInit = true;

                dataSender = new DataSender();

                dataSender.SendHello(connectionProvider);
                dataSender.SendWebCamDeviceList(connectionProvider, webCamStreamer.Devices);
                dataSender.SendDeviceFeatures(connectionProvider);

                screenStreamer.isLegacyPath = true;
            }
        }

        public void UpdatePath()
        {
            DefferedInit();

            if (dataSender != null)
            {
                dataSender.SendDeviceOrientation(connectionProvider);
                dataSender.SendOptions(connectionProvider);

                // Add accelerometer data
                if (SystemInfo.supportsAccelerometer)
                    dataSender.SendAccelerometerInput(connectionProvider);

                // Add gyroscope data
                if (SystemInfo.supportsGyroscope)
                    dataSender.SendGyroscopeSettings(connectionProvider);
                if (Input.gyro.enabled)
                    dataSender.SendGyroscopeInput(connectionProvider);

                // Add touch data
                if (Input.touchCount > 0)
                    dataSender.SendTouchInput(connectionProvider);

                //dataSender.SendJoystickNames(connectionProvider);
                //dataSender.SendJoystickInput(connectionProvider);

                SendWebCamStreams(connectionProvider);
                /*
                // Add location services (GPS) and compass data
                if (SystemInfo.supportsLocationService)
                    dataSender.SendLocationServiceData(connectionProvider);

                dataSender.SendCompassData(connectionProvider);
                */
            }

            if (connectionProvider != null)
            {
                connectionProvider.Update();
            }
        }

        public void LateUpdate() { }

        public void Stop()
        {
            defferedInit = false;
            screenStreamer.OnDisconnect();
        }

        void SendWebCamStreams(IConnectionProvider connectionProvider)
        {
            foreach (var device in webCamStreamer.Devices)
            {
                if (device.texture != null)
                {
                    Texture2D image = device.GetImage();

                    Profiler.BeginSample("EncodeToJPG");
                    byte[] encoded = image.EncodeToPNG();
                    Profiler.EndSample();

                    int angle = device.texture.videoRotationAngle;
                    bool mirrored = device.texture.videoVerticallyMirrored;
                    dataSender.SendWebCamStream(device.name, image.width, image.height, encoded, angle, mirrored, connectionProvider);
                }
            }
        }
    }
}