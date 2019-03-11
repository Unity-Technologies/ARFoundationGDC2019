using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.Mock
{
    public static class ReferencePointApi
    {
        public static void Update(TrackableId trackableId, Pose pose, TrackingState trackingState)
        {
            NativeApi.UnityXRMock_updateReferencePoint(trackableId, pose, trackingState);
        }
    }
}
