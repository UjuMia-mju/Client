using System;
using UnityEngine;

public class PacketHeader
{
    public const int HeaderSize = 4; // sizeof(ushort) * 2

    public ushort size;  // 패킷 전체 크기
    public ushort id;    // 프로토콜 ID

    public static PacketHeader FromBytes(byte[] buffer, int offset)
    {
        PacketHeader header = new()
        {
            size = BitConverter.ToUInt16(buffer, offset),
            id = BitConverter.ToUInt16(buffer, offset + 2)
        };
        return header;
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[HeaderSize];
        Array.Copy(BitConverter.GetBytes(size), 0, bytes, 0, 2);
        Array.Copy(BitConverter.GetBytes(id), 0, bytes, 2, 2);
        return bytes;
    }
}
