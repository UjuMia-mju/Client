using System;
using UnityEngine;


public class RecvBuffer
{
    private byte[] _buffer;
    private int _readPos;
    private int _writePos;
    private int _capacity;
    const int BUFFER_COUNT = 10; 
    
    public int DataSize => _writePos - _readPos;
    public int FreeSize => _capacity - _writePos;

    public RecvBuffer(int bufferSize)
    {
        _capacity = bufferSize * BUFFER_COUNT; // C++의 BUFFER_COUNT와 동일
        _buffer = new byte[_capacity];
        _readPos = 0;
        _writePos = 0;
    }

    public void Clean()
    {
        int dataSize = DataSize;
        if (dataSize == 0)
        {
            // 읽고 쓴 위치가 같으면 리셋
            _readPos = _writePos = 0;
        }
        else
        {
            // 남은 데이터를 버퍼 앞으로 이동
            Array.Copy(_buffer, _readPos, _buffer, 0, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }
    }

    public bool OnRead(int numOfBytes)
    {
        if (numOfBytes > DataSize)
            return false;

        _readPos += numOfBytes;
        return true;
    }

    public bool OnWrite(int numOfBytes)
    {
        if (numOfBytes > FreeSize)
            return false;

        _writePos += numOfBytes;
        return true;
    }

    public ArraySegment<byte> GetReadSegment()
    {
        return new ArraySegment<byte>(_buffer, _readPos, DataSize);
    }

    public ArraySegment<byte> GetWriteSegment()
    {
        return new ArraySegment<byte>(_buffer, _writePos, FreeSize);
    }
}
