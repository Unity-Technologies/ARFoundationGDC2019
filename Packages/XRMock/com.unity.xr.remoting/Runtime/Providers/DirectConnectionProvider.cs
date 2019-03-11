using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace ClientRemoting
{
    public class DirectConnectionProvider : IConnectionProvider
    {
        int RemotePort = 7201;
        int StreamBufferSize = 10 * 768 * 1024;

        Socket m_listenSocket;
        TcpClient m_tcpClient;

        // Stream we write data into, which is going to be used to send over network
        MemoryStream m_writeStream;
        byte[] m_copyBuffer;

        byte[] m_readBuffer;
        MemoryStream m_readStream;

        #region Class Implementation

        public override void Initialize()
        {
        }

        public override void StartListening()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            CloseSockets();

            m_listenSocket = SetupListening(RemotePort);

            m_copyBuffer = new byte[10 * 1024 * 1024];

            m_readBuffer = new byte[StreamBufferSize];
            m_readStream = new MemoryStream(m_readBuffer);
            m_readStream.Position = 0;
            m_readStream.SetLength(0);

            m_writeStream = new MemoryStream(StreamBufferSize);

            OnConnected += OnConnectedHandler;
            OnDisconnected += OnDisconnectedHandler;
        }

        public object LockObject = new object();
        
        public override void Disconnect()
        {
            lock (LockObject)
            {
                if (Status == IConnectionProvider.ConnectionStatus.Disconnected)
                    return;
                
                //m_listenSocket.Shutdown(SocketShutdown.Both);

                CloseSockets();

                Status = IConnectionProvider.ConnectionStatus.Disconnected;

                RaiseDisconnected();
            }
        }

        void CloseSockets()
        {
            if (m_tcpClient != null)
            {
                var client = m_tcpClient.Client;
                if (m_tcpClient.Connected)
                {
                    var stream = m_tcpClient.GetStream();
                    stream.Close();
                    stream.Dispose();
                }

                client.Close();
                m_tcpClient.Close();
                m_tcpClient = null;
            }

            if (m_listenSocket != null)
            {
                m_listenSocket.Disconnect(false);
                m_listenSocket.Close();
                m_listenSocket = null;
            }
        }

        public bool IsConnected()
        {
            return Status == IConnectionProvider.ConnectionStatus.Connected;
        }

        public override Stream GetStream()
        {
            return m_writeStream;
        }

        public override void SendMessage(byte[] message, Guid id = default(Guid))
        {
            m_writeStream.Write(message, 0, message.Length);
        }

        public override void SendMessage(Stream message, Guid id = default(Guid))
        {
            message.Position = 0;
            Utils.CopyToStream(message, m_writeStream, m_copyBuffer, (int)message.Length);
            message.Position = 0;
            message.SetLength(0);
        }

        public bool hasNoDelay = false;

        public override void Update()
        {
            // Happens on script reload
            if (m_listenSocket == null)
                return;

            var shouldDisconnect = false;
            lock (LockObject)
            {
                if (m_tcpClient == null)
                {
                    if ((m_tcpClient = AcceptIncoming(m_listenSocket)) == null)
                    {
                        return;
                    }
                    else
                    {
                        m_tcpClient.NoDelay = hasNoDelay;
                        RaiseConnected();
                    }
                }

                try
                {
                    if (Status == IConnectionProvider.ConnectionStatus.Connected)
                    {
                        ProcessIncomingData();
                        ProcessOutgoingData();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogFormat("Exception in Update : {0}\n{1}", ex.Message, ex.StackTrace);
                    shouldDisconnect = true;
                }
            }

            if (shouldDisconnect)
            {
                Disconnect();
                StartListening();
            }
        }

        public void SyncUpdate()
        {
            // Happens on script reload
            if (m_listenSocket == null)
                return;

            if (m_tcpClient == null)
            {
                if ((m_tcpClient = AcceptIncoming(m_listenSocket)) == null)
                {
                    m_tcpClient.NoDelay = hasNoDelay;
                    return;
                }
                else
                    RaiseConnected();
            }
        }

        public void AsyncUpdate()
        {
            if (m_tcpClient == null)
                return;

            try
            {
                if (Status == IConnectionProvider.ConnectionStatus.Connected)
                {
                    ProcessIncomingData();
                    ProcessOutgoingData();
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception : " + ex);
                RaiseDisconnected();
            }

            if (!m_tcpClient.Connected && Status == IConnectionProvider.ConnectionStatus.Connected)
            {
                Disconnect();
            }
        }

        #endregion

        #region Public Methods
        public void SetRemotePort(int port)
        {
            RemotePort = port;
        }
        #endregion

        #region Private Methods

        private void OnConnectedHandler()
        {
        }

        private void OnDisconnectedHandler()
        {
            m_readStream.Position = 0;
            m_readStream.SetLength(0);

            m_writeStream.Position = 0;
            m_writeStream.SetLength(0);

            CloseSockets();
        }

        private void ProcessIncomingData()
        {
            //Profiler.BeginSample("ProcessIncomingData");

            if (m_tcpClient == null)
                return;

            if (m_tcpClient.Client.Available == 0)
                return;

            NetworkStream stream = m_tcpClient.GetStream();
            RaiseStreamReceived(stream, m_tcpClient.Available);

            //Profiler.EndSample();
        }

        private void ProcessOutgoingData()
        {
            //Profiler.BeginSample("ProcessOutgoingData");

            if (m_tcpClient == null || !m_tcpClient.Connected)
                return;

            try
            {
                m_writeStream.Position = 0;
                NetworkStream stream = m_tcpClient.GetStream();
                Utils.CopyToStream(m_writeStream, stream, m_copyBuffer, (int) m_writeStream.Length);
                m_writeStream.Position = 0;
                m_writeStream.SetLength(0);
            }
            catch (Exception e)
            {
                Debug.LogFormat("Exception in ProcessOutgoingData: {0}\n{1}", e.Message, e.StackTrace);
                throw;
            }

            //Profiler.EndSample();
        }
        #endregion

        #region Static Helpers

        static Socket SetupListening(int listeningPort)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Any, listeningPort);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Blocking = false;
            socket.Bind(endPoint);
            socket.Listen(128);
            return socket;
        }

        static TcpClient AcceptIncoming(Socket listeningSocket)
        {
            try
            {
                var tcpSocket = listeningSocket.Accept();
                tcpSocket.Blocking = true;
                Debug.LogFormat("Accepted incoming socket. Endpoints are: {0}, {1}", tcpSocket.LocalEndPoint, tcpSocket.RemoteEndPoint);
                return new TcpClient { Client = tcpSocket };
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == (int)SocketError.WouldBlock)
                    return null;

                Debug.Log("SocketException in AcceptIncoming: " + ex);
                throw (ex);
            }
        }
        #endregion
    }
}