using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace EditorRemoting
{
    enum PlaneSubsystemEvent
    {
        PlaneAdded = 1,
        PlaneUpdated = 2,
        PlaneRemoved = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneData
    {
        public int eventType;
        public int eventSubType;

        public float x, y, z;

        public float px, py, pz;
        public float rx, ry, rz, rw;

        public float bx, by;

        public ulong id1;
        public ulong id2;
    };


    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundedPlaneData
    {
        public int eventType;

        public Vector3 Center;

        public Vector3 Position;
        public Quaternion Rotation;

        public Vector2 Size;

        public int Alignment;

        public ulong id0;
        public ulong id1;

        public ulong sid0;
        public ulong sid1;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct Vec3
    {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DepthData
    {
        public int size;
        public Vector3 points;
    };
}