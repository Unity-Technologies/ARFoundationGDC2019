namespace UnityEngine.XR.Mock
{
    public static class InputApi
    {
        public static Pose pose
        {
            set
            {
                var transform = new Matrix4x4();
                transform.SetTRS(value.position, value.rotation, Vector3.one);
                NativeApi.UnityXRMock_setPose(value, transform);
            }
        }
    }
}
