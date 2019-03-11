using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;


namespace ClientRemoting
{
    public class ScreenStream : MonoBehaviour
    {
        public GameObject uiBox;
        public Text warningText;
        public RawImage screenImage;

        Texture2D screen = null;
        bool synced = false;

        byte[] image;
        int width;
        int height;

        void Update()
        {
            if (synced && (screen != null) && screenImage.texture == null)
            {
                screenImage.gameObject.SetActive(true);
                screenImage.texture = screen;
            }
            else
            {
                if (SystemInfo.supportsGyroscope)
                    Input.gyro.enabled = false;
            }

            string warningString = "";

#if UNITY_EDITOR
            warningString += "Warning: This project should not be run in the editor, please deploy to a device to use.";
#endif
            if (warningString.Length > 0)
            {
                warningText.text = warningString;
            }

            if (synced)
            {
                uiBox.SetActive(false);
            }
            else
            {
                uiBox.SetActive(true);
            }
        }

        public bool isLegacyPath = false;

        public void LateUpdate()
        {
            Profiler.BeginSample("ScreenStream.LateUpdate");

            Profiler.BeginSample("LoadImage");
            if ((image != null) && (image.Length > 0) && hasChanged)
            {
                hasChanged = false;

                if (screen == null || screen.width != width || screen.height != height)
                {
                    TextureFormat format = TextureFormat.RGB24;
                    if (textureFormatToUse != -1)
                    {
                        Debug.Log("Texture format to use : " + (TextureFormat)textureFormatToUse);
                        format = (TextureFormat)textureFormatToUse;
                    }

                    screenImage.texture = null;
                    screen = new Texture2D(width, height, format, false, false);
                }

                if (!isLegacyPath)
                {
                    screen.LoadRawTextureData(image);
                    screen.Apply();
                }
                else
                {
                    screen.LoadImage(image);
                }

                synced = true;
                image = null;
            }
            Profiler.EndSample();

            Profiler.EndSample();
        }


        public void OnDisconnect()
        {
            synced = false;
            image = null;
        }

        int textureFormatToUse = -1;
        private bool hasChanged = false;

        public void UpdateScreen(byte[] data, int width, int height, int textureFormat = -1)
        {
            // Loading texture takes a lot of time, so we postpone it and do it in
            // LateUpdate(), in case we receive several images during single frame.
            this.image = data;
            this.width = width;
            this.height = height;
            this.textureFormatToUse = textureFormat;
            this.hasChanged = true;
        }
    }
}