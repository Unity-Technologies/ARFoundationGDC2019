using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        [MenuItem("AR Tools/Remote Window")]
        static void Init()
        {
            EditorRemoting window = (EditorRemoting)EditorWindow.GetWindow(typeof(EditorRemoting));
            window.Show(); 
            window.titleContent = new GUIContent("Remote");
        }

        static string m_subsystemFilter = "UnityXRMock";

        void OnDisable()
        {
            if(m_standardDataReceiver != null)
                m_standardDataReceiver.OnCustomDataReceived -= HandleCustomData;

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;

            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("EditorRemotingData", data);
        }

        private GUIStyle myFoldoutStyle;
        private string[] m_configurations;
        private RemoteScreenConfiguration m_remoteScreenConfiguration;

        private UdpClient m_udpClient;
        IAsyncResult ar_ = null;
        
        // Use this for initialization
        void OnEnable()
        {
            var data = EditorPrefs.GetString("EditorRemotingData", JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);

            EnableGUI();
            OnEnableConnection();
            
            ARSubsystemManager.subsystemFilter = m_subsystemFilter;

            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            m_configurations = AssetDatabase.FindAssets("t:RemoteScreenConfiguration");

            if (m_configurations.Length > 0)
            {
                m_configurationsNames = new string[m_configurations.Length];

                for (int i = 0; i < m_configurationsNames.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(m_configurations[i]);
                    var fileName = Path.GetFileName(path);
                    m_configurationsNames[i] = fileName;
                }
            }

#if ENABLE_BROADCAST
            var updateThread = new Thread( () =>
            {
                IPEndPoint localpt = new IPEndPoint(IPAddress.Any, 8888);
                
                var Server = new UdpClient();
                Server.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Server.Client.Bind(localpt);
                Server.Client.ReceiveTimeout = 0;
                
                var ClientEp = new IPEndPoint(IPAddress.Any, 0);
                
                while (true)
                {
                    try
                    {
                        Server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
                        var ClientRequestData = Server.Receive(ref ClientEp);
                        var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                    }
                    catch (Exception e)
                    {
                    }

                    if(!history.Contains(ClientEp.Address.ToString()))
                        history.Add(ClientEp.Address.ToString());
                    
                    Thread.Sleep(10);
                    
                    m_ipHistory = history.ToArray();
                }
            });

            updateThread.Start();
#endif
        }
        
#if ENABLE_BROADCAST
        List<string> history = new List<string>();
#endif

        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (connectOnStart && obj == PlayModeStateChange.EnteredPlayMode)
            {
                Connect();
            }
            else if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                Disconnect();
            }
        }
        
        void Update()
        {
            UpdateConnection();
        }
    }
}