using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace EditorRemoting
{
    public class DirectConnectionProvider : IConnectionProvider
    {
        int RemotePort = 7201;
        string IPAddress = "127.0.0.1";
        int StreamBufferSize = 2 * 1024 * 1024;

        Socket m_listenSocket;
        TcpClient m_tcpClient;

        // Stream we write data into, which is going to be used to send over network
        MemoryStream m_writeStream;
        byte[] m_copyBuffer;

        byte[] m_readBuffer;
        MemoryStream m_readStream;

        bool m_raisedConnected = false;
        private bool m_isListening = false;

        #region Class Implementation

        public override void Initialize()
        {
        }

        public override bool StartListening()
        {
            StartListening(null);
            
            return true;
        }

        private CancellationToken ct;
        
        public bool StartListening(Action callback)
        {
            m_raisedConnected = false;

            m_isListening = false;
            
            
            try
            {
                var addresses = Dns.GetHostAddresses(IPAddress);
                
                m_tcpClient =  new TcpClient(AddressFamily.InterNetwork);

                m_tcpClient.Connect(addresses[0], RemotePort);
                
                m_tcpClient.NoDelay = true;

                m_copyBuffer = new byte[10 * 1024 * 1024];

                m_readBuffer = new byte[StreamBufferSize];
                m_readStream = new MemoryStream(m_readBuffer);
                m_readStream.Position = 0;
                m_readStream.SetLength(0);

                m_writeStream = new MemoryStream(StreamBufferSize);

                OnConnected += OnConnectedHandler;
                OnDisconnected += OnDisconnectedHandler;

                if(callback != null)
                    callback.Invoke();

                m_isListening = true;
            }
            catch (SocketException e)
            {
                Debug.LogErrorFormat("SocketException in StartListening: {0}\n{1}", e.Message, e.StackTrace);
                return false;
            }
            
            return true;
        }

        public override void Disconnect()
        {
            if (m_tcpClient != null)
            {
                m_tcpClient.GetStream().Close();
                m_tcpClient.Client.Close();
                m_tcpClient.Close();
                m_tcpClient.Dispose();
                m_tcpClient = null;
            }

            OnDisconnectedHandler();

            RaiseDisconnected();
        }

        public override Stream GetStream()
        {
            if(m_tcpClient == null)
            {
                return null;
            }

            return m_tcpClient.GetStream();
        }

        public override void SendMessage(byte[] message, Guid id = default(Guid))
        {
            if (Status != ConnectionStatus.Connected)
                return;

            m_writeStream.Write(message, 0, message.Length);
        }

        public override void SendMessage(Stream message, Guid id = default(Guid))
        {
            if (Status != ConnectionStatus.Connected)
                return;

            message.Position = 0;
            Utils.CopyToStream(message, m_writeStream, m_copyBuffer, (int)message.Length);
            message.Position = 0;
            message.SetLength(0);
        }

        public bool IsValid()
        {
            return m_tcpClient != null && m_isListening == true;
        }

        public object LockObject = new object();

        public override void Update()
        {
            if (!IsValid())
                return;

            lock (LockObject)
            {

                if (!m_raisedConnected && m_tcpClient != null)
                {
                    if (m_tcpClient.Connected)
                    {
                        m_raisedConnected = true;
                        RaiseConnected();
                    }
                }

                try
                {
                    ProcessIncomingData();
                    ProcessOutgoingData();
                }
                catch (Exception ex)
                {
                    Debug.Log("Exception : " + ex);
                    RaiseDisconnected();
                }
            }
        }
        #endregion

        #region Public Methods

        public bool IsWriteBufferEmpty()
        {
            return (int)m_writeStream.Length == 0;
        }

        public void SetRemotePort(int port)
        {
            RemotePort = port;
        }

        public void SetIPAddress(string ipAddress)
        {
            IPAddress = ipAddress;
        }
        #endregion

        #region Private Methods

        private void OnConnectedHandler()
        {
        }

        private void OnDisconnectedHandler()
        {
            if (m_readStream != null)
            {
                m_readStream.Position = 0;
                m_readStream.SetLength(0);
            }

            if (m_writeStream != null)
            {
                m_writeStream.Position = 0;
                m_writeStream.SetLength(0);
            }

            if (m_tcpClient != null)
                m_tcpClient.Close();
            m_tcpClient = null;
        }

        public void ProcessIncomingData()
        {
            Profiler.BeginSample("ProcessIncomingData");

            if (m_tcpClient.Client.Available == 0)
                return;

            NetworkStream stream = m_tcpClient.GetStream();
            RaiseStreamReceived(stream, m_tcpClient.Available);

            Profiler.EndSample();
        }

        public void ProcessOutgoingData()
        {
            Profiler.BeginSample("ProcessOutgoingData");

            m_writeStream.Position = 0;
            NetworkStream stream = m_tcpClient.GetStream();
            Utils.CopyToStream(m_writeStream, stream, m_copyBuffer, (int)m_writeStream.Length);
            m_writeStream.Position = 0;
            m_writeStream.SetLength(0);

            Profiler.EndSample();
        }

        public void ProcessOutgoingData(Stream src)
        {
            Profiler.BeginSample("ProcessOutgoingData");

            src.Position = 0;
            NetworkStream stream = m_tcpClient.GetStream();
            Utils.CopyToStream(src, stream, m_copyBuffer, (int)src.Length);
            src.Position = 0;
            src.SetLength(0);

            Profiler.EndSample();
        }

        #endregion
    }
}