using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.Mock
{
    internal class MockReferencePointExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRReferencePointExtensions.RegisterAttachReferencePointHandler(k_SubsystemId, AttachReferencePoint);
        }

        static TrackableId AttachReferencePoint(XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose)
        {
            return NativeApi.UnityXRMock_attachReferencePoint(trackableId, pose);
        }

        static readonly string k_SubsystemId = "UnityXRMock-ReferencePoint";
    }
}
