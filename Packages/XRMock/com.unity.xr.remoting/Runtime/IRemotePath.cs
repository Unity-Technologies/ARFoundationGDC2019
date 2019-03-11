namespace ClientRemoting
{
    public interface RemotePath
    {
        void Start(IConnectionProvider provider, ScreenStream screen);
        void UpdatePath();
        void LateUpdate();
        void Stop();
    }
}