using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace EditorRemoting
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraData
    {
        public int eventType;
        public int eventSubType;

        public float px, py, pz;
        public float rx, ry, rz, rw;

        public Matrix4x4 pm;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraFrameData
    {
        public int eventType;
        public int eventSubType;

        public Int64 timeStampNs;
        public float averageBrightness;
        public float averageTemperature;

        public Matrix4x4 projectionMatrix;
        public Matrix4x4 displayMatrix;

        public byte cameraFramePropertyFlags;
    };
}