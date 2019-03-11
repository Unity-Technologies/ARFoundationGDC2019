using CommonRemoting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

#if !NET_LEGACY
using K4os.Compression.LZ4;
#endif

namespace EditorRemoting
{
    public class EditorDataSender
    {
        PacketWriter writer;
        MemoryStream stream;

        public static Queue<long> frameIds = new Queue<long>();

        public EditorDataSender()
        {
            const int StreamBufferSize = 1024 * 1024 * 1;
            stream = new MemoryStream(StreamBufferSize);
            writer = new PacketWriter();
        }

        public void SendReadyToStream(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage((byte)RemoteMessageID.ReadyToScreenStream);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }

        static byte[] dataToSendArray = null;
        static byte[] toSendData = null;
        private static int frameProcessedCounter = 0;
        private GCHandle handle;
        private bool allocated = false;
        private IntPtr address = IntPtr.Zero;

        public bool ShouldEnableCompression = false;

        public void SendScreen(FrameInfo frame, IConnectionProvider streamConnectionProvider, Action callback)
        {
            unsafe
            {
                if (streamConnectionProvider != null)
                {
                    if (frameIds.Count > 0)
                    {
                        var fId = frameIds.Peek();

                        if (fId != -1 && streamConnectionProvider.GetStream() != null && (streamConnectionProvider as DirectConnectionProvider).IsWriteBufferEmpty())
                        {
                            frameProcessedCounter++;

                            var data = frame.readback.Value.GetData<Byte>();
                            var dataLength = data.Length;
                            var intPtrData = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data);

                            if (dataToSendArray == null || dataToSendArray.Length != dataLength)
                            {
                                allocated = false;
                                dataToSendArray = new byte[dataLength];
                                handle = GCHandle.Alloc(dataToSendArray, GCHandleType.Pinned);
                                address = handle.AddrOfPinnedObject ();
                            }

                            var updateThread = new Thread( () =>
                            {
                                lock ((streamConnectionProvider as DirectConnectionProvider).LockObject)
                                {
                                    if (intPtrData != IntPtr.Zero)
                                    {
                                        UnsafeUtility.MemCpy(address.ToPointer(), intPtrData.ToPointer(), dataLength);

                                        int lengthOfData = 0;
                                        
                                        bool shouldEnableCompression = ShouldEnableCompression;
#if !NET_LEGACY
                                        if (shouldEnableCompression)
                                        {
                                            if (toSendData == null)
                                            {
                                                toSendData = new byte[LZ4Codec.MaximumOutputSize(dataToSendArray.Length)];
                                            }
                                            
                                            lengthOfData = LZ4Codec.Encode(
                                                dataToSendArray, 0, dataToSendArray.Length,
                                                toSendData, 0, toSendData.Length);
                                        }
#else
                                        toSendData = dataToSendArray;
                                        lengthOfData = toSendData.Length;
#endif
                                        writer.BeginMessage((byte) RemoteMessageID.ScreenStream);
                                        writer.Write(frame.width);
                                        writer.Write(frame.height);
                                        writer.Write(fId);
                                        writer.Write(shouldEnableCompression);
                                        writer.Write(frame.textureFormatID);
                                        
                                        writer.Write(lengthOfData);
                                        writer.Write(toSendData.Length);

                                        writer.Write(toSendData, lengthOfData);

                                        writer.EndMessage(streamConnectionProvider.GetStream());

                                        fId = frameIds.Dequeue();

                                        if (callback != null)
                                            callback.Invoke();
                                    }
                                }
                            });

                            updateThread.Start();
                        }
                    }
                }
            }
        }

        public void SendARSettings(bool enableARPreview, int scale, IConnectionProvider connectionProvider)
        {
            writer.BeginMessage((byte)RemoteMessageID.ARRemoteSettings);
            writer.Write(enableARPreview);
            writer.Write(scale);
            writer.EndMessage(stream);

            connectionProvider.SendMessage(stream);
        }
        
        public void SendHelloMessage(IConnectionProvider connectionProvider)
        {
            writer.BeginMessage((byte)RemoteMessageID.Hello);
            writer.Write("UnityRemote");
            writer.Write((uint)10);
            writer.EndMessage(stream);

            connectionProvider.SendMessage(stream);
        }

        public void SendWebcamStart(IConnectionProvider connectionProvider)
        {
            // TO DO - Create Properties Storage
            writer.BeginMessage((byte)RemoteMessageID.WebCamStartStream);
            writer.Write(DataReceiver.deviceName);
            writer.Write(DataReceiver.w);
            writer.Write(DataReceiver.h);
            writer.Write(DataReceiver.fpg);
            writer.EndMessage(stream);

            connectionProvider.SendMessage(stream);
        }

        public void SendLigthEstimation(bool data, IConnectionProvider connectionProvider)
        {
            writer.BeginMessage((byte)RemoteMessageID.ARLightEstimation);
            writer.Write(data);
            writer.EndMessage(stream);

            if (connectionProvider != null)
            {
                connectionProvider.SendMessage(stream);
            }
        }
    };
}