using System;
using System.IO;
using System.Runtime.InteropServices; 
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.Rendering;

namespace EditorRemoting
{
    public static class Utils
    {
        public static void CopyToStream(Stream src, Stream dst, byte[] buffer, int numBytes)
        {
            while (numBytes > 0)
            {
                int req = Math.Min(buffer.Length, numBytes);
                int read = src.Read(buffer, 0, req);
                dst.Write(buffer, 0, read);
                numBytes -= read;
            }
        }
    }

    public class ThreadUtils
    {
        public static EditorRemotingMainThreadDispathcer dispatcher;
    }

    public class EditorRemotingMainThreadDispathcer : MonoBehaviour
    {
        static readonly Queue<Action> actionsToRun = new Queue<Action>();

        public void ExecuteOnMainThread(Action action)
        {
            ExecuteOnMainThread(CoroutineWrapper(action));
        }

        IEnumerator CoroutineWrapper(Action actionToRun)
        {
            //run immediately then yield
            actionToRun();
            yield return null;
        }

        public void ExecuteOnMainThread(IEnumerator action)
        {
            lock (actionsToRun)
            {
                actionsToRun.Enqueue(() =>
                {
                    StartCoroutine(action);
                });
            }
        }

        public void Update()
        {
            lock (actionsToRun)
            {
                while (actionsToRun.Count > 0)
                {
                    actionsToRun.Dequeue().Invoke();
                }
            }
        }
    }
}