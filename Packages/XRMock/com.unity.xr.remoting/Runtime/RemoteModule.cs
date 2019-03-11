using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ClientRemoting
{
    public class RemoteModule : MonoBehaviour
    {
        IConnectionProvider connectionProvider;

        [SerializeField]
        public Text IPAddressText;

        [SerializeField]
        public GameObject MainMenu;

        DataReceiver dataReceiver;
        RemotePath remotePath;

        public ScreenStream screenStreamer;
        public WebCamStreamer webCamStreamer;

        public Camera arCamera;

        public RawImage syncedFrame;

        private bool isLegacyPath = false;
        private bool isConnected = false;

        private bool didStart = false;

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string result = string.Empty;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    result = ip.ToString();
                }
            }

            return result;
        }

        static string m_subsystemFilter = "UnityXRMock";

        private UdpClient Client;
        public void SendBroadcastMessage(string message)
        {
            var RequestData = Encoding.ASCII.GetBytes(message);
            var ServerEp = new IPEndPoint(IPAddress.Any, 0);

            Client.EnableBroadcast = true;
            Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

            var ServerResponseData = Client.Receive(ref ServerEp);
            var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);
            Debug.LogFormat("Recived {0} from {1}", ServerResponse, ServerEp.Address.ToString());
        }
        
        // Use this for initialization
        void Start()
        {
            ARSubsystemManager.subsystemFilter = m_subsystemFilter;

            IPAddressText.text = GetLocalIPAddress();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            dataReceiver = new DataReceiver(screenStreamer, null);

            dataReceiver.HelloMessageCallback = (legacyPath) =>
            {
                isLegacyPath = legacyPath;
                isConnected = true;
                
                if (remotePath == null)
                {
                    didStart = false;

                    if (isLegacyPath)
                    {
                        remotePath = new LegacyRemotePath();
                    }
                    else
                    {
                        remotePath = new ARRemotePath(dataReceiver);
                        (remotePath as ARRemotePath).SetCamera(arCamera);
                        (remotePath as ARRemotePath).SetSyncFrame(syncedFrame);
                    }
                }
            };
            
            // Create legacy direct connection provider
            connectionProvider = new DirectConnectionProvider();
            ((DirectConnectionProvider)connectionProvider).hasNoDelay = true;
            
            connectionProvider.OnDisconnected += ConnectionProvider_OnDisconnected;
            connectionProvider.OnDataReceived += ConnectionProvider_OnDataReceived;
            connectionProvider.OnStreamReceived += ConnectionProvider_OnStreamReceived;
            
            connectionProvider.Initialize();
            connectionProvider.StartListening();

            // register the callback when enabling object
            Camera.onPreRender += MyPreRender;
            Camera.onPostRender += MyPostRender;
            
            Client = new UdpClient(AddressFamily.InterNetwork);
            
#if ENABLE_BROADCAST
            StartCoroutine(BroadcastIPAddress());
#endif
        }

#if ENABLE_BROADCAST
        private IEnumerator BroadcastIPAddress()
        {
            do
            {
                var ipAddress = GetLocalIPAddress();
                SendBroadcastMessage(ipAddress);
            
                yield return new WaitForSeconds(10);
            } while (true);
        }
#endif

        private void ConnectionProvider_OnStreamReceived(System.IO.Stream stream, int available)
        {
            dataReceiver.AppendData(stream, available);
            dataReceiver.ProcessMessages();
        }

        private void ConnectionProvider_OnDataReceived(byte[] data, int available)
        {
            dataReceiver.AppendData(data, available);
            dataReceiver.ProcessMessages();
        }

        private void ConnectionProvider_OnDisconnected()
        {
            MainMenu.SetActive(true);

            isConnected = false;
            didStart = false;
            
            // Stop on Path
            remotePath.Stop();

            connectionProvider.Disconnect();
        }


        // Update is called once per frame
        void Update()
        {
            if (!isConnected)
            {
                if (connectionProvider != null)
                {
                    connectionProvider.Update();
                }

                return;
            }
            else if(remotePath != null)
            {
                if (didStart == false)
                {
                    didStart = true;
                    remotePath.Start(connectionProvider, screenStreamer);
                    MainMenu.SetActive(false);
                }

                remotePath.UpdatePath();
            }
        }

        void LateUpdate()
        {
            if (remotePath != null)
            {
                remotePath.LateUpdate();
            }
        }

        public void MyPreRender(Camera cam)
        {
            if(arCamera == cam && remotePath != null && remotePath is ARRemotePath)
            {
                ((ARRemotePath)remotePath).OnPreRenderUpdate();
            }
        }

        public void MyPostRender(Camera cam)
        {
            if(arCamera == cam && remotePath != null && remotePath is ARRemotePath)
            {
                ((ARRemotePath)remotePath).OnPostRenderUpdate();
            }
        }
    }
}