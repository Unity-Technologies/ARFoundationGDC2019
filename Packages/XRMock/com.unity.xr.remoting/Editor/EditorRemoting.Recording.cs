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
        protected byte[] buffer = new byte[1024 * 1024 * 10];
        private MemoryStream recordedStream = new MemoryStream();

        [Serializable]
        private class RecordEvent
        {
            public float tick;
            public byte[] data;
            public int size;
            public bool processed;
        }

        List<RecordEvent> m_recordedEvents = new List<RecordEvent>();
        float startedRecordingTime = 0;
    }
}