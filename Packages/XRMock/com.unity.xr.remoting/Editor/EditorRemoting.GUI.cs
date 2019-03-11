using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CommonRemoting;
using UnityEditor;
using UnityEditor.Experimental;
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
        enum PlaybackState
        {
            Playing,
            Stopped
        }

        enum RecordingState
        {
            Recording,
            Stopped
        }
        
        private readonly Regex m_regexIPValidation = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]).){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$", RegexOptions.IgnoreCase);
        
        private bool m_ShowAdvancedOptions = false;
        private bool m_UseRecording = false;
        
        private PlaybackState m_playbackState = PlaybackState.Stopped;
        private RecordingState m_recordingState = RecordingState.Stopped;

        
        void EnableGUI()
        {
            if (string.IsNullOrEmpty(m_ipAddress))
            {
            }
        }

        private bool VerifyIPAddress(string ipaddress)
        {
            return !string.IsNullOrEmpty(ipaddress) && m_regexIPValidation.IsMatch(ipaddress.Trim());
        }
        
        private void UpdateRemoteMachineHistory()
        {
            List<string> history = new List<string>(m_ipHistory);

            // check for existing item in history
            for (int i = 0; i < m_ipHistory.Length; ++i)
            {
                if (m_ipHistory[i].Equals(m_ipAddress, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (i == 0)
                        return;

                    history.RemoveAt(i);
                    break;
                }
            }

            history.Insert(0, m_ipAddress);

            if (history.Count > 50)
            {
                history.RemoveRange(50, history.Count - 50);
            }

            m_ipHistory = history.ToArray();
        }

        #region UI Methods
        private void ShowConnectionStatus()
        {
            EditorGUILayout.LabelField(new GUIContent("Status :"), new GUIContent(m_connectionStatus.ToString()));
        }

        private void ShowCompressionOption()
        {
#if !NET_LEGACY
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Enable compression");
            m_dataSender.ShouldEnableCompression = EditorGUILayout.ToggleLeft("", m_dataSender.ShouldEnableCompression);

            EditorGUILayout.EndHorizontal();
#else
            EditorGUI.BeginDisabledGroup(true);
            
            EditorGUILayout.HelpBox("Enable .NET 4.x scripting back end to be able to use data compression.", MessageType.Warning);
            m_dataSender.ShouldEnableCompression = GUILayout.Toggle(m_dataSender.ShouldEnableCompression, "Enable compression");

            EditorGUI.EndDisabledGroup();
#endif
        }

        private void ShowDeviceList()
        {
            EditorGUI.BeginChangeCheck();
            
            if (string.IsNullOrEmpty(m_ipAddress))
            {
                m_ipAddress = "Type ID Address";
            }
            
            var address = EditorGUI.DelayedTextFieldDropDown(
                GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textFieldDropDownText), new GUIContent(""),
                m_ipAddress, m_ipHistory);


            if (!string.IsNullOrEmpty(address) && VerifyIPAddress(address))
            {
                m_ipAddress = address;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                UpdateRemoteMachineHistory();
            }
        }

        private void ShowConnectButton()
        {
            if (VerifyIPAddress(m_ipAddress)
                && m_connectionStatus == ConnectionStatus.Disconnected)
            {
                if (GUILayout.Button("Connect"))
                {
                    m_connectionStatus = ConnectionStatus.Connecting;

                    this.Repaint();

                    Connect();
                }
            }
            else if(m_connectionStatus == ConnectionStatus.Connected)
            {
                if (GUILayout.Button("Disconnect From Remote App"))
                {
                    Disconnect();
                }
            }
        }

        private void ShowRecordButton()
        {
            if (m_recordingState != RecordingState.Recording)
            {
                if (GUILayout.Button("Record Session"))
                {
                    m_recordedEvents.Clear();
                    startedRecordingTime = Time.realtimeSinceStartup;
                    m_recordingState = RecordingState.Recording;
                }
            }
            else if (m_recordingState == RecordingState.Recording)
            {
                if (GUILayout.Button("Stop Recording"))
                {
                    m_recordingState = RecordingState.Stopped;
                    
                    if (EditorUtility.DisplayDialog("Recording session stopped. Do you want to save recording ?", "Save Recording", "Discard"))
                    {
                        SaveRecording();
                    }
                }
            }
        }

        private void ShowAutoConnect()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Autoconnect");
            connectOnStart = EditorGUILayout.ToggleLeft("", connectOnStart);

            EditorGUILayout.EndHorizontal();
        }

        private void ShowFactorOption()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Downscale Factor");
            EditorGUI.BeginChangeCheck();
            ScaleFactor = EditorGUILayout.IntSlider(ScaleFactor, 1, 10);

            
            if (EditorGUI.EndChangeCheck())
            {
                GameViewScreenHelper.Rescale(ScaleFactor);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void ShowCameraOption()
        {
            if(cameraToStream == null)
            {
                EditorGUILayout.HelpBox("Assign Camera which output should be streamed to the device.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying != false);
            EditorGUILayout.BeginHorizontal();
            cameraToStream = EditorGUILayout.ObjectField("Camera to stream", cameraToStream, typeof(Camera), true, null) as Camera;
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private void ShowRecordControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Play"))
            {
                NativeApi.UnityXRMock_connectDevice(1);

                m_recordingState = RecordingState.Stopped;

                lastProcessedTime = 0;
                lastIndex = 0;

                m_playbackState = PlaybackState.Playing;
            }
            else if (GUILayout.Button("Stop Playing"))
            {
                NativeApi.UnityXRMock_disconnectDevice(1);

                m_recordingState = RecordingState.Stopped;

                m_playbackState = PlaybackState.Stopped;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void ShowLoadRecordButton()
        {
            if (GUILayout.Button("Load"))
            {
                m_recordingState = RecordingState.Stopped;
                m_playbackState = PlaybackState.Stopped;
    
                var path = EditorUtility.OpenFilePanel(
                    "Load recording",
                    "",
                    "remoting");
    
                if (path.Length != 0)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    using (Stream input = File.OpenRead(path))
                    {
                        byte[] buffer = new byte[32 * 1024];
                        int bytesRead;
                        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, bytesRead);
                        }
                    }
    
                    memoryStream.Position = 0;
    
                    m_recordedEvents.Clear();
    
                    BinaryReader reader = new BinaryReader(memoryStream);
                    reader.ReadByte(); // id
                    reader.ReadUInt32(); // id2
                    var count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var tick = reader.ReadSingle();
                        var available = reader.ReadInt32();
                        var dataBytes = reader.ReadBytes(available);
                        var processedD = reader.ReadBoolean();
    
                        m_recordedEvents.Add(new RecordEvent()
                        {
                            size = available,
                            data = dataBytes,
                            tick = tick,
                            processed = processedD
                        });
                    }
                }
            }
        }
        
        private void ShowRecordingOptions()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Use Recording");
            m_UseRecording = EditorGUILayout.ToggleLeft("", m_UseRecording);

            EditorGUILayout.EndHorizontal();

            if (m_UseRecording)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    ShowLoadRecordButton();
                    ShowRecordControlButtons();
                }
            }
            
        }
        
        private void SaveRecording()
        {
            m_recordingState = RecordingState.Stopped;
            m_playbackState = PlaybackState.Stopped;

            PacketWriter writer = new PacketWriter();
            const int StreamBufferSize = 1024 * 1024 * 2;
            MemoryStream saveStream = new MemoryStream(StreamBufferSize);

            writer.BeginMessage(0);
            writer.Write(m_recordedEvents.Count);
            for (int i = 0; i < m_recordedEvents.Count; i++)
            {
                writer.Write(m_recordedEvents[i].tick);
                writer.Write(m_recordedEvents[i].size);
                writer.Write(m_recordedEvents[i].data);
                writer.Write(m_recordedEvents[i].processed);
            }

            writer.EndMessage(saveStream);

            var path = EditorUtility.SaveFilePanel(
                "Save recording",
                "",
                ".remoting",
                "remoting");

            if (path.Length != 0)
            {
                File.WriteAllBytes(path, saveStream.ToArray());
            }
        }
        
        private void ShowAdvancedOptions()
        {
            ShowCameraOption();

            ShowRemoteScreenOptions();
            
            m_remoteScreenConfiguration = EditorGUILayout.ObjectField("Remote screen Configuration", m_remoteScreenConfiguration, typeof(RemoteScreenConfiguration), true, null) as RemoteScreenConfiguration;
            
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying == false || cameraToStream == null);

            EditorGUI.EndDisabledGroup();

            if (myFoldoutStyle == null)
            {
                myFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            showVisualization = EditorGUILayout.Foldout(showVisualization, "Visualization", myFoldoutStyle);
            if (showVisualization)
            {
                visualizeRemotingData = GUILayout.Toggle(visualizeRemotingData, "Enable Visualisation");
                visualizeCameraPath = GUILayout.Toggle(visualizeCameraPath, "Show Camera Path");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Vizualize Factor");
                visualizeScale = EditorGUILayout.Slider(visualizeScale, 0.1f, 2.0f);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Camera Colors", EditorStyles.boldLabel);
                cameraFarPlaneColor = EditorGUILayout.ColorField("Camera FarPlane Color", cameraFarPlaneColor);
                cameraFarPlaneOutlineColor =
                    EditorGUILayout.ColorField("Camera FarPlane Outline Color", cameraFarPlaneOutlineColor);

                cameraSidePlaneColor = EditorGUILayout.ColorField("Camera Side Color", cameraSidePlaneColor);
                cameraSidePlaneOutlineColor =
                    EditorGUILayout.ColorField("Camera Side Outline Color", cameraSidePlaneOutlineColor);

                EditorGUILayout.LabelField("Points and Planes Colors", EditorStyles.boldLabel);
                pointOutlineColor = EditorGUILayout.ColorField("Point Outline", pointOutlineColor);
                pointColor = EditorGUILayout.ColorField("Point", pointColor);
                planeOutlineColor = EditorGUILayout.ColorField("Plane Outline", planeOutlineColor);
                planeColor = EditorGUILayout.ColorField("Plane", planeColor);
            }

            showDebugHelpers = EditorGUILayout.Foldout(showDebugHelpers, "[DEBUG] Helpers", myFoldoutStyle);
            if (showDebugHelpers)
            {
                if (GUILayout.Button("[DEBUG] Send Webcam start"))
                {
                    m_dataSender.SendWebcamStart(null);
                }

#if UNITY_IOS
            if (GUILayout.Button("Start Remote Device - Local"))
            {
                SetupLocalConnectionOverUSB();
            }

            if (GUILayout.Button("Stop Remote Device - Local"))
            {
                StopLocalConnectionOverUSB();
            }
#elif UNITY_ANDROID
                if (GUILayout.Button("Start Remote Device - Local"))
                {
                    SetupLocalConnectionOverUSB();
                    AndroidCommandHelper("adb forward tcp:7201 tcp:7201");
                    AndroidCommandHelper("adb forward tcp:7202 tcp:7202");
                    AndroidCommandHelper("adb forward tcp:7203 tcp:7203");
                }
#endif
            }
        }

        private void ShowRemoteScreenOptions()
        {
            EditorGUI.BeginDisabledGroup(m_connectionStatus != ConnectionStatus.Disconnected);

            m_waitOnFrame = GUILayout.Toggle(m_waitOnFrame, "Use Coroutines for Frame Capture");

            if (m_remoteScreenComponent != null &&
                m_remoteScreenComponent.WaitOnFrame != m_waitOnFrame)
            {
                m_remoteScreenComponent.WaitOnFrame = m_waitOnFrame;
            }
            
            EditorGUI.EndDisabled();
        }

        #endregion

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            ShowDeviceList();
            ShowConnectButton();
            ShowRecordButton();
            
            EditorGUILayout.EndHorizontal();
            
            ShowConnectionStatus();
            ShowAutoConnect();
            ShowCompressionOption();
            ShowFactorOption();
            ShowRecordingOptions();
            
            /* Adanced Options */
            
            EditorGUILayout.Separator();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Show Advanced Options");
            m_ShowAdvancedOptions = EditorGUILayout.ToggleLeft("", m_ShowAdvancedOptions);
            EditorGUILayout.EndHorizontal();
            
            if(m_ShowAdvancedOptions)
            {
                ShowAdvancedOptions();
            }
        }
    }
}