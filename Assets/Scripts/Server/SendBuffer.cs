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
        // Marshal.SizeOf(typeof(T)): 런타임에 구조체의 unmanaged 크기를 바이트 단위로 반환한다.
        // InteropServices는 C#에서 C/C++와 같은 unmanaged 코드와 상호 운용할 수 있도록 도와주는 namespace임.
        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        byte[] bytes = new byte[size];

        // 1. 구해진 사이즈만큼 메모리를 할당
        IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        // 2. RAM에 구조체 바이트로 변환.
        System.Runtime.InteropServices.Marshal.StructureToPtr(value, ptr, false);
        // 3. 복사
        System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, size);
        // 4. 정리
        System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);

        _stream.Write(bytes, 0, bytes.Length);
    }

    public void Write(byte[] bytes)
    {
        _stream.Write(bytes, 0, bytes.Length);
    }
}
