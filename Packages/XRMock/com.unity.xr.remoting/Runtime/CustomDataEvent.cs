using System.IO;

namespace CommonRemoting
{
    public class CustomDataEvent
    {
        public bool CanBeProcessed(int eventId)
        {
            return eventId == EventId && Handled == false;
        }

        public int EventId;
        public BinaryReader BinaryReader;
        public bool Handled = false;
    }
}