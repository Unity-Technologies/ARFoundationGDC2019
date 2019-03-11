using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.Mock
{
    public static class SessionApi
    {
        public static TrackingState trackingState
        {
            set
            {
                NativeApi.UnityXRMock_setTrackingState(value);
            }
        }
    }
}
