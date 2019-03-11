using System;
using System.IO;

namespace EditorRemoting
{
    public abstract class IConnectionProvider
    {
        public enum ConnectionStatus
        {
            Connecting,
            Connected,
            Disconnecting,
            Disconnected
        }

        public ConnectionStatus Status;

        public abstract Stream GetStream();

        public delegate void ConnectedAction();
        public event ConnectedAction OnConnected;
        public event ConnectedAction OnDisconnected;

        public delegate void ConnectionDataReceived(byte[] data, int available);
        public event ConnectionDataReceived OnDataReceived;

        public delegate void ConnectionStreamReceived(Stream stream, int available);
        public event ConnectionStreamReceived OnStreamReceived;

        abstract public void Initialize();
        abstract public bool StartListening();
        abstract public void Disconnect();
        abstract public void Update();

        abstract public void SendMessage(byte[] message, Guid id = default(Guid));
        abstract public void SendMessage(Stream message, Guid id = default(Guid));

        //TODO - Decide which approach is more flexible
        public void RaiseStreamReceived(Stream stream, int available)
        {
            if (OnStreamReceived != null)
                OnStreamReceived(stream, available);
        }

        public void RaiseDataReceived(byte[] data, int available)
        {
            if (OnDataReceived != null)
                OnDataReceived(data, available);
        }

        public void RaiseConnected()
        {
            Status = ConnectionStatus.Connected;

            if (OnConnected != null)
                OnConnected();
        }

        public void RaiseDisconnected()
        {
            Status = ConnectionStatus.Disconnected;

            if (OnDisconnected != null)
                OnDisconnected();
        }
    }
}