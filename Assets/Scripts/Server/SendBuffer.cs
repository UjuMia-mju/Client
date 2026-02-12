using System;
using System.IO;
using UnityEngine;

public class SendBuffer
{
    private MemoryStream _stream = new MemoryStream();

    public ArraySegment<byte> GetSegment()
    {
        byte[] buffer = _stream.ToArray();
        return new ArraySegment<byte>(buffer, 0, buffer.Length);
    }

    public void Write<T>(T value) where T : struct
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        byte[] bytes = new byte[size];

        IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        System.Runtime.InteropServices.Marshal.StructureToPtr(value, ptr, false);
        System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, size);
        System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);

        _stream.Write(bytes, 0, bytes.Length);
    }

    public void Write(byte[] bytes)
    {
        _stream.Write(bytes, 0, bytes.Length);
    }
}
