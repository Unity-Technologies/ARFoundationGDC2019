using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.Mock
{
    internal static class MockPlaneExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRPlaneExtensions.RegisterGetTrackingStateHandler(k_SubsystemId, GetTrackingState);
        }

        static TrackingState GetTrackingState(XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            TrackingState trackingState;
            PlaneApi.TryGetTrackingState(planeId, out trackingState);

            return trackingState;
        }

        static readonly string k_SubsystemId = "UnityXRMock-Plane";
    }
}
