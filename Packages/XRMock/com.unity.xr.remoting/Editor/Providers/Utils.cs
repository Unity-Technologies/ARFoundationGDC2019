using System;
using System.Runtime.InteropServices;

namespace EditorRemoting
{
    public class RemotingUtils
    {
        public static byte[] StructToByteArray<T>(T data)
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static byte[] StructToByteArray<T>(T data, int size)
        {
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static T ConvertFromByteArray<T>(byte[] data) where T : struct
        {
            T returnValue = new T();

            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(data, 0, ptr, size);

            returnValue = (T)Marshal.PtrToStructure(ptr, returnValue.GetType());
            Marshal.FreeHGlobal(ptr);

            return returnValue;
        }

        public static T ConvertFromByteArray<T>(byte[] data, int size) where T : struct
        {
            T returnValue = new T();

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(data, 0, ptr, size);

            returnValue = (T)Marshal.PtrToStructure(ptr, returnValue.GetType());
            Marshal.FreeHGlobal(ptr);

            return returnValue;
        }
    }
}