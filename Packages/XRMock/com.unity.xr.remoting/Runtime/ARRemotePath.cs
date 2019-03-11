using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.XR;
using UnityEngine.Profiling;
using UnityEngine.XR.ARFoundation;
using System.Threading;
#if XRREMOTING_USE_NEW_INPUT_SYSTEM
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
#endif
using CommonRemoting;

namespace ClientRemoting
{
    public class ARRemotePath : RemotePath
    {
        public static DateTime connectedTime;

        RenderTexture toStream;
        RenderTexture rt;
        RenderTexture finalRt;


        Thread updateThread;

        IConnectionProvider connectionProvider;
        DirectConnectionProvider connectionProviderAR;
        public static DirectConnectionProvider connectionProviderDefferredCalls;

        private DataReceiver globalReceived;
        DataReceiver streamingDataReceiver;
        DataReceiver defferedCallsReceived;

        public DataSender dataSender;
        public DataSender pointsDataSender;
        public static DataSender separateThreadDataSender;

        ScreenStream screenStreamer;
        WebCamStreamer webCamStreamer;
        List<Vector3> depthPoints;
        
        bool pendingTexAssign = false;

        bool defferedInit;

        private ARCameraFrameEventArgs m_ARFrameDataToSend;
        private bool m_hasARFrameDataToSend;
        private bool m_hasARFrameImageToSet;

        private bool arConnected = false;
        public Camera arCamera;
        RawImage syncedFrame;

        int m_lastWidth = -1;
        int m_lastHeight = -1;
        int m_lastOrientation = -1;

        public ARRemotePath(DataReceiver receiver)
        {
            globalReceived = receiver;
        }
        
        public void Stop()
        {
            UnsubscribeToAREvenets();

            if (updateThread != null)
            {
                updateThread.Abort();
                updateThread = null;
            }

            defferedInit = false;
            
            screenStreamer.OnDisconnect();

            connectionProviderAR.Disconnect();
            connectionProviderDefferredCalls.Disconnect();
            
            arConnected = false;
            streamingDataReceiver.Reset();
            streamingDataReceiver.frames.Clear();

            defferedCallsReceived.Reset();

            dataSender.Reset();
            pointsDataSender.Reset();
            separateThreadDataSender.Reset();

            pendingTexAssign = false;
        }

        public void DefferedInit()
        {
            if (!defferedInit)
            {
                defferedInit = true;

                streamingDataReceiver.frames.Clear();

                dataSender.SendHello(connectionProvider);
                dataSender.SendWebCamDeviceList(connectionProvider, webCamStreamer.Devices);
                dataSender.SendDeviceFeatures(connectionProvider);

                dataSender.SendReadyToStream(connectionProvider);

                connectedTime = DateTime.UtcNow;

                screenStreamer.isLegacyPath = false;
                
                SubscribeToAREvenets();
            }
        }

        public void SetCamera(Camera xrCamera)
        {
            arCamera = xrCamera;
        }
        
        public void Start(IConnectionProvider standardConnectionProvider, ScreenStream screen)
        {
#if XRREMOTING_USE_NEW_INPUT_SYSTEM
            InputSystem.onEvent += InputSystem_onEvent;
#endif

            dataSender = new DataSender();
            pointsDataSender = new DataSender();
            separateThreadDataSender = new DataSender();

            screenStreamer = screen;

            connectionProvider = standardConnectionProvider;

            arCamera.targetTexture = new RenderTexture(arCamera.pixelWidth, arCamera.pixelHeight, 16, RenderTextureFormat.ARGB32);

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            depthPoints = new List<Vector3>();

            webCamStreamer = new WebCamStreamer();
            streamingDataReceiver = new DataReceiver(screenStreamer, webCamStreamer);
            defferedCallsReceived = new DataReceiver(screenStreamer, webCamStreamer);

            streamingDataReceiver.handleScreenStream = () =>
            {
                pendingTexAssign = true;
            };
            
            globalReceived.OnARRemoteSettingsReceived += OnArRemoteSettingsReceived;

            connectionProviderDefferredCalls = new DirectConnectionProvider();
            connectionProviderDefferredCalls.hasNoDelay = true;
            connectionProviderDefferredCalls.SetRemotePort(7203);

            connectionProviderAR = new DirectConnectionProvider();
            ((DirectConnectionProvider)connectionProviderAR).hasNoDelay = true;
            connectionProviderAR.SetRemotePort(7202);

            connectionProviderAR.OnConnected += ConnectionProviderAR_OnConnected;
            connectionProviderAR.OnDisconnected += ConnectionProviderar_OnDisconnected;
            connectionProviderAR.OnDataReceived += ConnectionProviderar_OnDataReceived;
            connectionProviderAR.OnStreamReceived += ConnectionProviderar_OnStreamReceived;

            connectionProviderDefferredCalls.OnStreamReceived += ConnectionProviderSeparateThread_OnStreamReceived;

            connectionProviderAR.Initialize();
            connectionProviderAR.StartListening();

            connectionProviderDefferredCalls.Initialize();
            connectionProviderDefferredCalls.StartListening();
        }

        private bool sendARPreview = true;
        private int arPreviewScale = 10;
        
        private void OnArRemoteSettingsReceived(DataReceiver.ARRemoteSettings obj)
        {
            sendARPreview = obj.EnableARPreview;
            arPreviewScale = obj.ARPreviewScale;
        }

        public void UpdatePath()
        {
            DefferedInit();

            if (!defferedInit)
                return;

            if (connectionProviderAR != null && !arConnected)
            {
                connectionProviderAR.Update();
            }

            if (connectionProvider != null)
            {
                connectionProvider.Update();
            }

            if (connectionProviderDefferredCalls != null)
            {
                connectionProviderDefferredCalls.Update();
            }

            if (dataSender != null)
            {
                if (streamingDataReceiver.frames.Count == 0)
                {

                    if ((int)Screen.orientation != m_lastOrientation)
                    {
                        dataSender.SendDeviceOrientation(connectionProvider);
                        m_lastOrientation = (int)Screen.orientation;
                    }

                    if(Screen.width != m_lastWidth || Screen.height != m_lastHeight)
                    {
                        m_lastHeight = Screen.height;
                        m_lastWidth = Screen.width;
                        dataSender.SendOptions(connectionProvider);   
                    }

                    // Add accelerometer data
                    if (SystemInfo.supportsAccelerometer)
                        dataSender.SendAccelerometerInput(connectionProvider);

                    // Add gyroscope data
                    if (SystemInfo.supportsGyroscope)
                        dataSender.SendGyroscopeSettings(connectionProvider);

                    if (Input.gyro.enabled)
                        dataSender.SendGyroscopeInput(connectionProvider);

                    // Add touch data
                    //if (Input.touchCount > 0)
                    //dataSender.SendTouchInput(connectionProvider);

                    //dataSender.SendJoystickNames(connectionProvider);
                    //dataSender.SendJoystickInput(connectionProvider);

                    SendWebCamStreams(connectionProvider);
                    /*
                    // Add location services (GPS) and compass data
                    if (SystemInfo.supportsLocationService)
                        dataSender.SendLocationServiceData(connectionProvider);

                    dataSender.SendCompassData(connectionProvider);
                    */

                    streamingDataReceiver.SetAwaitingFrameId(pendingId, null);
                    
                    timeFrame = DateTime.UtcNow;

                    pendingId = timeFrame.Ticks;

                    if (arCamera != null)
                    {
                        dataSender.SendCameraData(connectionProvider, arCamera, timeFrame);
                    }

                    if (m_hasARFrameDataToSend)
                    {
                        m_hasARFrameDataToSend = false;
                        m_hasARFrameImageToSet = true;
                        dataSender.SendFrameReceived(connectionProvider, m_ARFrameDataToSend);

                        #region Preview AR Stream

                        //if (sendARPreview)
                        {
                            var activeRenderTexture = RenderTexture.active;
                            RenderTexture.active = toStream;
                            if (m_CachedCameraTexture == null)
                                m_CachedCameraTexture = new Texture2D(toStream.width, toStream.height,
                                    TextureFormat.RGB24, false);

                            m_CachedCameraTexture.ReadPixels(new Rect(0, 0, toStream.width, toStream.height), 0, 0);
                            m_CachedCameraTexture.Apply(false);

                            RenderTexture.active = activeRenderTexture;

                            dataSender.SendCustomData(connectionProvider, (PacketWriter writer) =>
                            {
                                var data = m_CachedCameraTexture.EncodeToJPG(70);
                                writer.Write(data.Length);
                                writer.Write(data);
                                writer.Write((int) m_CachedCameraTexture.format);
                                writer.Write(m_CachedCameraTexture.width);
                                writer.Write(m_CachedCameraTexture.height);
                                return 0;
                            }, 100);
                        }

                        #endregion

                    }
                }
            }

            if (connectionProvider != null)
            {
                connectionProvider.Update();
            }

            if (connectionProviderDefferredCalls != null)
            {
                connectionProviderDefferredCalls.Update();
            }
        }

        Texture2D m_CachedCameraTexture;

        public void LateUpdate()
        {
            if (pendingTexAssign && streamingDataReceiver.frames.Count == 1)
            {
                pendingTexAssign = false;
                
                Graphics.Blit(rt , finalRt);
                
                streamingDataReceiver.frames.Dequeue();
                m_hasARFrameImageToSet = false;
            }
        }

        DateTime timeFrame;
        long pendingId = -1;      
        public void OnPreRenderUpdate()
        {
        }
        
        ARCameraBackground m_background;

        public void OnPostRenderUpdate()
        {
        }

        #region Events
        private void UnsubscribeToAREvenets()
        {
            ARSubsystemManager.planeAdded -= ARSubsystemManager_PlaneAdded;
            ARSubsystemManager.planeUpdated -= ARSubsystemManager_PlaneUpdated;
            ARSubsystemManager.planeRemoved -= ARSubsystemManager_PlaneRemoved;
            
            ARSubsystemManager.sessionSubsystem.TrackingStateChanged -= ARSubsystemManager_TrackingStateChanged;
            
            ARSubsystemManager.cameraFrameReceived -= ARSubsystemManager_CameraFrameReceived;

            ARSubsystemManager.pointCloudUpdated -= ARSubsystemManager_PointCloudUpdated;
            ARSubsystemManager.referencePointUpdated -= ProcessRerencePointUpdated;
        }

        private void SubscribeToAREvenets()
        {
#if UNITY_EDITOR
            ARSubsystemManager.CreateSubsystems();
#endif

            ARSubsystemManager.planeAdded += ARSubsystemManager_PlaneAdded;
            ARSubsystemManager.planeUpdated += ARSubsystemManager_PlaneUpdated;
            ARSubsystemManager.planeRemoved += ARSubsystemManager_PlaneRemoved;

            ARSubsystemManager.sessionSubsystem.TrackingStateChanged += ARSubsystemManager_TrackingStateChanged;
            ARSubsystemManager.cameraFrameReceived += ARSubsystemManager_CameraFrameReceived;

            ARSubsystemManager.pointCloudUpdated += ARSubsystemManager_PointCloudUpdated;
            ARSubsystemManager.referencePointUpdated += ProcessRerencePointUpdated;
        }
        
        DataSender.ReferencePointUpdateID referenceUpdateData = new DataSender.ReferencePointUpdateID();
        private void ProcessRerencePointUpdated(ReferencePointUpdatedEventArgs obj)
        {
            referenceUpdateData.eventType = -1;
            referenceUpdateData.eventSubType = 1;

            IdToArray idToArray;
            unsafe
            {
                var id = obj.ReferencePoint.Id;
                var ptr = (TrackableId*)&id;
                idToArray = *(IdToArray*)ptr;
            }

            //byte[] idToByte = DataSender.StructToByteArray<TrackableId>(obj.ReferencePoint.Id);
            //DataSender.IdToArray idToArray = DataSender.ByteArrayToType<DataSender.IdToArray>(idToByte);

            referenceUpdateData.id1 = idToArray.id0;
            referenceUpdateData.id2 = idToArray.id1;

            referenceUpdateData.trackingState = (int)obj.ReferencePoint.TrackingState;

            referenceUpdateData.px = obj.ReferencePoint.Pose.position.x;
            referenceUpdateData.py = obj.ReferencePoint.Pose.position.y;
            referenceUpdateData.pz = obj.ReferencePoint.Pose.position.z;

            referenceUpdateData.rx = obj.ReferencePoint.Pose.rotation.x;
            referenceUpdateData.ry = obj.ReferencePoint.Pose.rotation.y;
            referenceUpdateData.rz = obj.ReferencePoint.Pose.rotation.z;
            referenceUpdateData.rw = obj.ReferencePoint.Pose.rotation.w;

            referenceUpdateData.ppx = obj.PreviousPose.position.x;
            referenceUpdateData.ppy = obj.PreviousPose.position.y;
            referenceUpdateData.ppz = obj.PreviousPose.position.z;

            referenceUpdateData.prx = obj.PreviousPose.rotation.x;
            referenceUpdateData.pry = obj.PreviousPose.rotation.y;
            referenceUpdateData.prz = obj.PreviousPose.rotation.z;
            referenceUpdateData.prw = obj.PreviousPose.rotation.w;

            referenceUpdateData.previousTrackingState = (int)obj.PreviousTrackingState;

            if (dataSender != null)
            {
                dataSender.SendReferenePointUpdate(connectionProvider, referenceUpdateData);
            }
        }

        private void ARSubsystemManager_PointCloudUpdated(PointCloudUpdatedEventArgs obj)
        {
            obj.DepthSubsystem.GetPoints(depthPoints);

            if (pointsDataSender != null)
            {
                pointsDataSender.SendPointCloudUpdated(connectionProvider, depthPoints);
            }
        }

        private void ARSubsystemManager_CameraFrameReceived(ARCameraFrameEventArgs newFrameDataToSend)
        {
            m_hasARFrameDataToSend = true;
            m_ARFrameDataToSend = newFrameDataToSend;
            
            if (finalRt == null)
            {
                var desc = arCamera.targetTexture.descriptor;
                desc.depthBufferBits = 0;

                rt = new RenderTexture(desc);
                finalRt = new RenderTexture(desc);

                desc.width = desc.width / arPreviewScale;
                desc.height = desc.height / arPreviewScale;

                toStream = new RenderTexture(desc);
                syncedFrame.texture = finalRt;

            }
            
            if (m_background == null)
            {
                m_background = arCamera.GetComponent<ARCameraBackground>();
            }

            if (m_background.material != null && !m_hasARFrameImageToSet)
            {
                Graphics.Blit(null, rt, m_background.material);
                Graphics.Blit(null, toStream, m_background.material);
            }
        }

        private void ARSubsystemManager_TrackingStateChanged(SessionTrackingStateChangedEventArgs obj)
        {
            if (dataSender != null)
            {
                dataSender.SendTrackingStateChanged(connectionProvider, (int)obj.NewState);
            }
        }

        private void ARSubsystemManager_PlaneRemoved(PlaneRemovedEventArgs obj)
        {
            SendPlaneEvent(3, obj.Plane);
        }

        private void ARSubsystemManager_PlaneUpdated(PlaneUpdatedEventArgs obj)
        {
            SendPlaneEvent(2, obj.Plane);
        }

        private void ARSubsystemManager_PlaneAdded(PlaneAddedEventArgs obj)
        {
            SendPlaneEvent(1, obj.Plane);
        }

        private void SendPlaneEvent(int type, BoundedPlane Plane)
        {
            if (dataSender != null)
            {
                dataSender.SendPlane(connectionProvider, type, Plane);
            }
        }

        public void SetSyncFrame(RawImage sync)
        {
            syncedFrame = sync;
        }

#if XRREMOTING_USE_NEW_INPUT_SYSTEM
        private unsafe void InputSystem_onEvent(UnityEngine.Experimental.Input.LowLevel.InputEventPtr obj)
        {
            var device = InputSystem.TryGetDeviceById(obj.deviceId);
            if (device is Touchscreen)
            {
                InputRemoting.Message msg;
                msg.participantId = 1;
                msg.type = InputRemoting.MessageType.NewEvents;
                
                // Find total size of event buffer we need.
                var totalSize = 0u;
                totalSize += obj.sizeInBytes;

                // Copy event data to buffer. Would be nice if we didn't have to do that
                // but unfortunately we need a byte[] and can't just pass the 'events' IntPtr
                // directly.
                var data = new byte[totalSize];
                fixed (byte* dataPtr = data)
                {
                    IntPtr intP = obj.data;
                    //Marshal.Copy(intPSendTouchInput, data, 0, (int)totalSize);

                    UnsafeUtility.MemCpy(dataPtr, obj.data.ToPointer(), totalSize);
                }
                
                if (dataSender != null)
                {
                    int phase = (int)Touchscreen.current.phase.ReadValue();
                    float posX = Touchscreen.current.position.x.ReadValue();
                    float posY = Touchscreen.current.position.y.ReadValue();

                    dataSender.SendTouchInput(connectionProvider, phase, posX, posY);
                }
            }
        }
#endif

        private void ConnectionProviderar_OnDisconnected()
        {
        }

        private void ConnectionProviderAR_OnConnected()
        {
            arConnected = true;
            if (updateThread == null)
            {
                updateThread = new Thread(() =>
                {
                    while (true)
                    {
                        if (arConnected && connectionProviderAR != null && ((DirectConnectionProvider)connectionProvider).IsConnected())
                        {
                            connectionProviderAR.Update();
                        }
                        else
                        {
                            break;
                        }
                    }
                });
            }

            updateThread.Start();

        }

        private void ConnectionProviderar_OnStreamReceived(System.IO.Stream stream, int available)
        {
            streamingDataReceiver.AppendData(stream, available);
            streamingDataReceiver.ProcessMessages();
        }

        private void ConnectionProviderar_OnDataReceived(byte[] data, int available)
        {
        }

        private void ConnectionProviderSeparateThread_OnStreamReceived(System.IO.Stream stream, int available)
        {
            defferedCallsReceived.AppendData(stream, available);
            defferedCallsReceived.ProcessMessages();
        }
#endregion

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