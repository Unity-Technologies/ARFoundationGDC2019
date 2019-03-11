using System;
using System.IO;
using UnityEngine;

namespace ClientRemoting
{
    public class FileIOProvider : IConnectionProvider
    {
        int StreamBufferSize = 10 * 768 * 1024;

        MemoryStream m_writeStream;
        byte[] m_copyBuffer;

        byte[] m_readBuffer;
        MemoryStream m_readStream;

        FileStream fileStream;

        BinaryWriter writer;
        BinaryReader reader;

        #region Class Implementation

        public override void Initialize()
        {
        }

        public override void StartListening()
        {
            m_copyBuffer = new byte[10 * 1024 * 1024];

            m_readBuffer = new byte[StreamBufferSize];
            m_readStream = new MemoryStream(m_readBuffer);
            m_readStream.Position = 0;
            m_readStream.SetLength(0);

            m_writeStream = new MemoryStream(StreamBufferSize);

            OnConnected += OnConnectedHandler;
            OnDisconnected += OnDisconnectedHandler;

            RaiseConnected();
        }

        public override void Disconnect()
        {
            RaiseDisconnected();
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

        public override void Update()
        {
        }

        #endregion

        #region Public Methods
        public void SaveToFile(string fileName)
        {
            FileStream file = File.Open(Application.persistentDataPath + "/" + fileName, FileMode.OpenOrCreate);

            m_writeStream.Position = 0;
            file.Write(m_writeStream.ToArray(), 0, (int)m_writeStream.Length);
            m_writeStream.Position = 0;
            m_writeStream.SetLength(0);

            file.Close();
        }

        public void LoadFromFile(string fileName)
        {
            if (File.Exists(Application.persistentDataPath + "/" + fileName))
            {
                var stream = File.OpenRead(Application.persistentDataPath + "/" + fileName);
                RaiseStreamReceived(stream, (int)stream.Length);
            }
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
        }

        private void ProcessIncomingData()
        {
        }

        private void ProcessOutgoingData()
        {
        }
        #endregion

        #region Static Helpers

        #endregion
    }
}