using System;

namespace UnityEngine.XR.Mock
{
    public static class DepthApi
    {
        public static void SetDepthData(Vector3[] positions, float[] confidences = null)
        {
            if (positions == null)
                throw new ArgumentNullException("positions");

            if (confidences != null && positions.Length != confidences.Length)
                throw new ArgumentException("confidences must be the same length as positions");

            NativeApi.UnityXRMock_setDepthData(positions, confidences, positions.Length);
        }
    }
}
