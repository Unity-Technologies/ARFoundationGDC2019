using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.Mock
{
    public static class PlaneApi
    {
        public static TrackableId Add(Pose pose, Vector3 center, Vector2 size, TrackingState trackingState = TrackingState.Tracking)
        {
            var planeId = NativeApi.UnityXRMock_createTrackableId(Guid.NewGuid());
            s_TrackingStates[planeId] = trackingState;
            NativeApi.UnityXRMock_setPlaneData(planeId, pose, center, size, null, 0);
            return planeId;
        }

        public static void Update(TrackableId planeId, Pose pose, Vector3 center, Vector2 size)
        {
            NativeApi.UnityXRMock_setPlaneData(planeId, pose, center, size, null, 0);
        }

        public static TrackableId Add(Pose pose, Vector2[] boundaryPoints, TrackingState trackingState = TrackingState.Tracking)
        {
            if (boundaryPoints == null)
                throw new ArgumentNullException("boundaryPoints");

            var planeId = NativeApi.UnityXRMock_createTrackableId(Guid.NewGuid());
            s_TrackingStates[planeId] = trackingState;
            SetPlaneData(planeId, pose, boundaryPoints);
            return planeId;
        }

        public static void Update(TrackableId planeId, Pose pose, Vector2[] boundaryPoints)
        {
            SetPlaneData(planeId, pose, boundaryPoints);
        }

        public static void SetTrackingState(TrackableId planeId, TrackingState trackingState)
        {
            if (!s_TrackingStates.ContainsKey(planeId))
                return;

            s_TrackingStates[planeId] = trackingState;
        }

        public static bool TryGetTrackingState(TrackableId planeId, out TrackingState trackingState)
        {
            return s_TrackingStates.TryGetValue(planeId, out trackingState);
        }

        public static void Remove(TrackableId planeId)
        {
            NativeApi.UnityXRMock_removePlane(planeId);
            s_TrackingStates.Remove(planeId);
        }

        static Vector3 ComputeCenter(Vector3[] boundaryPoints)
        {
            var center = Vector3.zero;
            foreach (var point in boundaryPoints)
            {
                center += point;
            }
            center /= (float)boundaryPoints.Length;

            return center;
        }

        static void SetPlaneData(TrackableId planeId, Pose pose, Vector2[] boundaryPoints)
        {
            var sessionSpacePoints = TransformBoundary(pose, boundaryPoints);
            var center = ComputeCenter(sessionSpacePoints);
            var size = ComputeSize(boundaryPoints);

            NativeApi.UnityXRMock_setPlaneData(planeId, pose, center,
                size, sessionSpacePoints, sessionSpacePoints.Length);
        }

        static Vector2 ComputeSize(Vector2[] boundaryPoints)
        {
            Vector2 min = new Vector2(Mathf.Infinity, Mathf.Infinity);
            Vector2 max = new Vector2(-Mathf.Infinity, -Mathf.Infinity);

            foreach (var point in boundaryPoints)
            {
                min.x = Mathf.Min(min.x, point.x);
                min.y = Mathf.Min(min.y, point.y);

                max.x = Mathf.Max(max.x, point.x);
                max.y = Mathf.Max(max.y, point.y);
            }

            return new Vector2(max.x - min.x, max.y - min.y);
        }

        static Vector3[] TransformBoundary(Pose pose, Vector2[] pointsPlaneSpace)
        {
            var pointsSessionSpace = new Vector3[pointsPlaneSpace.Length];
            for (int i = 0; i < pointsSessionSpace.Length; ++i)
            {
                var point2d = pointsPlaneSpace[i];
                var point3d = new Vector3(point2d.x, 0, point2d.y);
                pointsSessionSpace[i] = pose.rotation * point3d + pose.position;
            }

            return pointsSessionSpace;
        }

        static Dictionary<TrackableId, TrackingState> s_TrackingStates = new Dictionary<TrackableId, TrackingState>();
    }
}
