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
        enum ConnectionStatus
        {
            Connecting,
            Connected,
            Disconnecting,
            Disconnected
        }

        #region Remote Window - Remoting Properties

        [SerializeField] string[] m_ipHistory = new string[0];
        
        [SerializeField] string m_ipAddress = string.Empty;

        [SerializeField] int ScaleFactor;

        [SerializeField] bool connectOnStart = false;

        [SerializeField] bool visualizeRemotingData = false;

        [SerializeField] private bool visualizeCameraPath = false;

        [SerializeField] float visualizeScale;

        [SerializeField] private string selectedConfiguration = string.Empty;

        public Camera cameraToStream { get; private set; }
        
        private bool m_waitOnFrame { get; set; }
        
        #endregion

        #region Remote Network Properties

        /// <summary>
        /// Connection provider used for synchronous data being sent every frame
        /// </summary>
        public DirectConnectionProvider standardConnectionProvider;

        /// <summary>
        /// Connection provider used for streaming screen to a remote device, updated as soon as the frame is ready
        /// </summary>
        public DirectConnectionProvider streamingConnectionProvider;

        /// <summary>
        /// Connection provider used for deffered calls. Keeping it separate for now to make maintance easy;
        /// </summary>
        public static DirectConnectionProvider separateThreadConnectionProvider;

        int standardPort = 7201;
        int streamingPort = 7202;
        int defferedPort = 7203;

        EditorDataSender m_dataSender;

        DataReceiver m_recordingDataReceiver;
        DataReceiver m_standardDataReceiver;
        DataReceiver m_streamingDataReceiver;

        ConnectionStatus m_connectionStatus;

        bool m_readyToStream = false;

        #endregion

        #region Mock proxy settings

        List<IMockProxy> m_proxies = new List<IMockProxy>();

        XRSDKMockProxy m_nativeProxy;

        public void AddProxy(IMockProxy proxyImplementation)
        {
            m_proxies.Add(proxyImplementation);
        }

        #endregion
        // Use this for initialization
        void OnEnableConnection()
        {
            m_connectionStatus = ConnectionStatus.Disconnected;
            
            m_dataSender = new EditorDataSender();

#if !NET_LEGACY
            m_dataSender.ShouldEnableCompression = true;
#endif

            standardConnectionProvider = new DirectConnectionProvider();
            streamingConnectionProvider = new DirectConnectionProvider();
            separateThreadConnectionProvider = new DirectConnectionProvider();

			m_nativeProxy = new XRSDKMockProxy();
            m_nativeProxy.ConnectionProvider = standardConnectionProvider;
            m_nativeProxy.SeparateThreadConnectionProvider = separateThreadConnectionProvider;
            m_nativeProxy.DataSender = m_dataSender;

			AddProxy(m_nativeProxy);

            GameViewScreenHelper.ScaleFactor = ScaleFactor;

            if (m_streamingDataReceiver == null)
            {
                m_streamingDataReceiver = new DataReceiver(m_proxies);
            }

            if (m_standardDataReceiver == null)
            {
                m_standardDataReceiver = new DataReceiver(m_proxies);
                m_standardDataReceiver.ReadyToScreenStream = () => { m_readyToStream = true; };
                m_standardDataReceiver.OnOptionsReceived = (int w, int h, int orientation, bool assignToCurrent) =>
                {
                    GameViewScreenHelper.SetUpGameView(w, h, orientation, assignToCurrent);
                    GameViewScreenHelper.Rescale(ScaleFactor);
                };
            }

            m_nativeProxy.Initialize();
            m_nativeProxy.SetCamera(cameraToStream);
            m_standardDataReceiver.cameraMain = cameraToStream;

            var videoMaterial = new Material(Shader.Find("Standard"));

            m_standardDataReceiver.mat = videoMaterial;
            m_streamingDataReceiver.mat = videoMaterial;

            standardConnectionProvider.OnConnected += ConnectionProvider_OnConnected;
            standardConnectionProvider.OnDisconnected += ConnectionProvider_OnDisconnected;
            standardConnectionProvider.OnDataReceived += ConnectionProvider_OnDataReceived;
            standardConnectionProvider.OnStreamReceived += ConnectionProvider_OnStreamReceived;

            m_standardDataReceiver.OnCustomDataReceived += HandleCustomData;

            streamingConnectionProvider.OnDataReceived += OnStreamingDataReceived;
            streamingConnectionProvider.OnStreamReceived += OnStreamingStreamReceived;
            streamingConnectionProvider.SetRemotePort(standardPort);

            m_playbackState = PlaybackState.Stopped;
            lastProcessedTime = 0;
            lastIndex = 0;
        }

        private Texture2D tex;
        private byte[] imgData;
        private void HandleCustomData(CustomDataEvent customData)
        {
            // ar preview data stream
            if (customData.EventId == 100)
            {
                var size = customData.BinaryReader.ReadInt32();
                imgData = customData.BinaryReader.ReadBytes(size);

                var format = customData.BinaryReader.ReadInt32();
                var width = customData.BinaryReader.ReadInt32();
                var height = customData.BinaryReader.ReadInt32();

                if (tex == null)
                {
                    tex = new Texture2D(width, height, (TextureFormat)format, false);
                }
            }
        }

        private void ConnectionProvider_OnStreamReceived(Stream stream, int available)
        {
            if (m_standardDataReceiver != null)
            {
                Utils.CopyToStream(stream, recordedStream, buffer, available);

                var receivedData = recordedStream.ToArray();
                recordedStream.Position = 0;
                recordedStream.SetLength(0);

                if (m_recordingState == RecordingState.Recording)
                {
                    m_recordedEvents.Add(new RecordEvent()
                    {
                        size = available,
                        data = receivedData,
                        tick = Time.realtimeSinceStartup - startedRecordingTime,
                        processed = false
                    });
                }

                if (m_playbackState != PlaybackState.Playing)
                {
                    m_standardDataReceiver.AppendData(receivedData, available);
                    m_standardDataReceiver.ProcessMessages();
                }
            }
        }

        private void ConnectionProvider_OnDataReceived(byte[] data, int available)
        {
            if (m_standardDataReceiver != null)
            {
                m_standardDataReceiver.AppendData(data, available);
                m_standardDataReceiver.ProcessMessages();
            }
        }

        private void ConnectionProvider_OnDisconnected()
        {
            m_readyToStream = false;
            m_connectionStatus = ConnectionStatus.Disconnected;

            EditorDataSender.frameIds.Clear();

            if (Application.isPlaying)
                NativeApi.UnityXRMock_disconnectDevice(1);
        }

        bool canConnectOnMainThread = false;

        private void ConnectionProvider_OnConnected()
        {
            m_connectionStatus = ConnectionStatus.Connected;

            if (Application.isPlaying)
                NativeApi.UnityXRMock_connectDevice(1);

            canConnectOnMainThread = true;

            //m_dataSender.SendARSettings(true,2,standardConnectionProvider);
            m_dataSender.SendHelloMessage(standardConnectionProvider);
        }


        private void OnStreamingStreamReceived(Stream stream, int available)
        {
            if (m_streamingDataReceiver != null)
            {
                m_streamingDataReceiver.AppendData(stream, available);
                m_streamingDataReceiver.ProcessMessages();
            }
        }

        private void OnStreamingDataReceived(byte[] data, int available)
        {
            if (m_streamingDataReceiver != null)
            {
                m_streamingDataReceiver.AppendData(data, available);
                m_streamingDataReceiver.ProcessMessages();
            }
        }

        private int m_lastQualitySettings;
        private int m_lastVSYNCSetting;

        private void Disconnect()
        {
            m_connectionStatus = ConnectionStatus.Disconnecting;

            standardConnectionProvider.Disconnect();
            streamingConnectionProvider.Disconnect();
            separateThreadConnectionProvider.Disconnect();
            
            QualitySettings.antiAliasing = m_lastQualitySettings;
            QualitySettings.vSyncCount = m_lastVSYNCSetting;

            if (m_remoteScreenComponent != null)
            {
                DestroyImmediate(m_remoteScreenComponent);
            }
            
            StopLocalConnectionOverUSB();
        }

        private void RefreshCamera()
        {
            cameraToStream.backgroundColor = Color.clear;
        }

        private void Connect()
        {
            if (cameraToStream == null)
            {
                cameraToStream = Camera.main;

                if (cameraToStream == null)
                {
                    Debug.LogWarning("No camera to stream.");
                    m_connectionStatus = ConnectionStatus.Disconnected;
                    return;
                }
            }

            m_standardDataReceiver.cameraMain = cameraToStream;

            SetupLocalConnectionOverUSB();

            m_lastQualitySettings = QualitySettings.antiAliasing;
            m_lastVSYNCSetting = QualitySettings.vSyncCount;
            
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            m_recordingState = RecordingState.Stopped;

            // If Camera wasn't specified, try to find AR Session Origin which uses camera object for AR purposes
            RefreshCamera();

            m_connectionStatus = ConnectionStatus.Connecting;

            standardConnectionProvider.Initialize();
            standardConnectionProvider.SetRemotePort(standardPort);
            standardConnectionProvider.SetIPAddress(m_ipAddress);
            if (!standardConnectionProvider.StartListening(OnConnectedStatus))
                m_connectionStatus = ConnectionStatus.Disconnected;
        }

        void OnConnectedStatus()
        {
            m_connectionStatus = ConnectionStatus.Connected;   
        }

        RemoteScreenComponent m_remoteScreenComponent;

        void AttachRemoteComponent()
        {
            if (cameraToStream != null)
            {
                if (cameraToStream.gameObject.GetComponent<RemoteScreenComponent>() == null)
                {
                    m_remoteScreenComponent = cameraToStream.gameObject.AddComponent<RemoteScreenComponent>();
                }
                else
                {
                    m_remoteScreenComponent = cameraToStream.gameObject.GetComponent<RemoteScreenComponent>();
                }

                m_remoteScreenComponent.WaitOnFrame = m_waitOnFrame;

                if(m_remoteScreenConfiguration != null)
                {
                    m_remoteScreenConfiguration.Initialize(m_remoteScreenComponent);
                }
                else if (!string.IsNullOrEmpty(selectedConfiguration) && m_configurations.Length > 0)
                {
                    int findIndex = 0;
                    for (int i = 0; i < m_configurationsNames.Length; i++)
                    {
                        if (m_configurationsNames[i].IndexOf(selectedConfiguration) != -1)
                        {
                            findIndex = i;
                            break;
                        }
                    }
                    
                    var path = AssetDatabase.GUIDToAssetPath(m_configurations[findIndex]);
                    var config = AssetDatabase.LoadAssetAtPath<RemoteScreenConfiguration>(path);

                    config.Initialize(m_remoteScreenComponent);
                }

                m_remoteScreenComponent.ProcessImage = (FrameInfo frameInfo, Action callback) => { m_dataSender.SendScreen(frameInfo, streamingConnectionProvider, callback); };
                m_remoteScreenComponent.CanProcessImages = () => { return EditorDataSender.frameIds.Count > 0; };
                
                cameraToStream.gameObject.GetComponent<RemoteScreenComponent>().Reset();
            }
        }

        float lastProcessedTime = 0;
        int lastIndex = 0;
        private bool showVisualization;
        private bool showRecording;
        private bool showDebugHelpers;
        private string[] m_configurationsNames;

        void UpdateConnection()
        {
            UpdateFunc();

            LateUpdate();
        }

        void LateUpdate()
        {
            if (tex != null)
            {
                if (imgData != null && cameraToStream != null)
                {
                    tex.LoadImage(imgData);
                    tex.Apply(false);

                    var rc = cameraToStream.gameObject.GetComponent<RemoteScreenComponent>();
                    if(rc != null)
                        rc.SetPreviewTexture(tex);
                }
            }
        }

        public void UpdateFunc()
        {
            if (EditorApplication.isPlaying)
            {
                if (cameraToStream != null)
                {
                    m_cameraPositions.Enqueue(cameraToStream.transform.position);

                    if (m_cameraPositions.Count > 180)
                    {
                        m_cameraPositions.Dequeue();
                    }
                }
            }

            // Deferred initialization after connection and handshake were established
            if (canConnectOnMainThread && m_readyToStream && cameraToStream!=null)
            {
                canConnectOnMainThread = false;
                m_readyToStream = false;

                AttachRemoteComponent();

                streamingConnectionProvider.Initialize();
                streamingConnectionProvider.SetRemotePort(streamingPort);
                streamingConnectionProvider.SetIPAddress(m_ipAddress);
                streamingConnectionProvider.StartListening();

                separateThreadConnectionProvider.Initialize();
                separateThreadConnectionProvider.SetRemotePort(defferedPort);
                separateThreadConnectionProvider.SetIPAddress(m_ipAddress);
                separateThreadConnectionProvider.StartListening();

                m_dataSender.SendReadyToStream(standardConnectionProvider);
            }

            if (standardConnectionProvider != null)
            {
                standardConnectionProvider.Update();
            }

            if (m_playbackState == PlaybackState.Playing)
            {
                lastProcessedTime += Time.deltaTime;

                for (int i = lastIndex; i < m_recordedEvents.Count; i++)
                {
                    if (m_recordedEvents[i].tick < lastProcessedTime && lastIndex < m_recordedEvents.Count - 1)
                    {
                        m_standardDataReceiver.AppendData(m_recordedEvents[i].data, m_recordedEvents[i].size);
                        m_standardDataReceiver.ProcessMessages();
                        lastIndex = i;
                    }
                    else
                    {
                        m_standardDataReceiver.ProcessMessages();
                        break;
                    }
                }

                if (lastIndex == m_recordedEvents.Count - 1)
                {
                    lastIndex = 0;
                    lastProcessedTime = 0;
                    m_standardDataReceiver.ClearData();
                }
            }
        }
    }
}
