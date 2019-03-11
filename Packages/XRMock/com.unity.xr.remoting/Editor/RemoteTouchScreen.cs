#if XRREMOTING_USE_NEW_INPUT_SYSTEM
using UnityEngine;
using CommonRemoting;

namespace EditorRemoting
{
    class RemoteTouchscreen : Touchscreen
    {
    }

    class RemoteTouchscreenHelper
    {
        static RemoteTouchscreen m_device;

        public static void InitializeRemoteTouchScreenInput()
        {
            InputSystem.RegisterControlLayout<RemoteTouchscreen>();
            DataReceiver.OnCustomDataReceived += ProcessCustomEvent;
        }

        public static void ProcessCustomEvent(CustomDataEvent eventData)
        {
            if (eventData.CanBeProcessed((int)CustomDataID.InputEvent + 1))
            {
                var reader = eventData.BinaryReader;

                var ph = reader.ReadInt32();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();

                var ts = new TouchState
                {
                    phase = (PointerPhase)ph,
                    touchId = 0,
                    position = new Vector2(x, y)
                };

                HandleInput(ts);

                eventData.Handled = true;
            }
        }

        public static void HandleInput(TouchState touchState)
        {
            InputSystem.QueueStateEvent(m_device, touchState);
        }
    }
}
#endif
