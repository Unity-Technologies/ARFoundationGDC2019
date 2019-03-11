using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonRemoting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Mock;
using UnityEditor.Hardware;

namespace EditorRemoting
{
    public partial class EditorRemoting : EditorWindow
    {
        private void StopLocalConnectionOverUSB()
        {
#if UNITY_IOS
            UnityEditor.iOS.Usbmuxd.StopIosProxy((ushort)standardPort);
            UnityEditor.iOS.Usbmuxd.StopIosProxy((ushort)streamingPort);
            UnityEditor.iOS.Usbmuxd.StopIosProxy((ushort)defferedPort);

            var devices = DevDeviceList.GetDevices();

            if (devices.Length > 1 && devices[1].module == "iOS")
            {
                var id = devices[1].id;
                IDeviceUtils.StopRemoteSupport(id);
            }
#endif
        }

        private void SetupLocalConnectionOverUSB()
        {
#if UNITY_IOS
            var devices = DevDeviceList.GetDevices();

            if(devices.Length > 1 && devices[1].module == "iOS")
            {
                var id = devices[1].id;
                var remoteAddress = IDeviceUtils.StartRemoteSupport(id);
                m_ipAddress = remoteAddress.ip;
                standardPort = remoteAddress.port;
                streamingPort = standardPort + 1;
                defferedPort = standardPort + 2;

                for (ushort port = (ushort)streamingPort; port < streamingPort + 100; port++)
                {
                    if (UnityEditor.iOS.Usbmuxd.StartIosProxy(port, (ushort)7202, id))
                    {
                        UnityEngine.Debug.Log(port);
                        streamingPort = port;
                        defferedPort = port + 1;
                        break;
                    }
                }

                for (ushort port = (ushort)defferedPort; port < defferedPort + 100; port++)
                {
                    if (UnityEditor.iOS.Usbmuxd.StartIosProxy(port, (ushort)7203, id))
                    {
                        UnityEngine.Debug.Log(port);
                        defferedPort = port;
                        break;
                    }
                }
            }
#elif UNITY_ANDROID
            AndroidCommandHelper("forward tcp:7201 tcp:7201");
            AndroidCommandHelper("forward tcp:7202 tcp:7202");
            AndroidCommandHelper("forward tcp:7203 tcp:7203");
#endif
        }

        private void AndroidCommandHelper(string commandToRun)
        {
            var devies = DevDeviceList.GetDevices();
            
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

            var androidSdkRootPath = EditorPrefs.GetString("AndroidSdkRoot");

            var pathToFile = "\\platform-tools\\adb.exe";
            
            #if UNITY_EDITOR_OSX
            pathToFile = "/platform-tools/adb";
            #endif
            
            var platform_tools = androidSdkRootPath + pathToFile;
            
            startInfo.FileName = platform_tools;
            startInfo.Arguments = commandToRun;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
    }
}