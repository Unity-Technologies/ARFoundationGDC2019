namespace UnityEngine.XR.Mock
{
    public static class CameraApi
    {
        public static bool permissionGranted { get; set; }

        public static Matrix4x4? projectionMatrix
        {
            set
            {
                if (value.HasValue)
                {
                    NativeApi.UnityXRMock_setProjectionMatrix(
                        value.Value, value.Value.inverse, true);
                }
                else
                {
                    NativeApi.UnityXRMock_setProjectionMatrix(
                        Matrix4x4.identity, Matrix4x4.identity, false);
                }
            }
        }

        public static Matrix4x4? displayMatrix
        {
            set
            {
                NativeApi.UnityXRMock_setDisplayMatrix(
                    value.HasValue ? value.Value : Matrix4x4.identity, value.HasValue);
            }
        }

        public static float? averageBrightness
        {
            set
            {
                NativeApi.UnityXRMock_setAverageBrightness(
                    value.HasValue ? value.Value : 0f, value.HasValue);
            }
        }

        public static float? averageColorTemperature
        {
            set
            {
                NativeApi.UnityXRMock_setAverageColorTemperature(
                    value.HasValue ? value.Value : 0f, value.HasValue);
            }
        }

        public static Color? colorCorrection { internal get; set; }
    }
}
