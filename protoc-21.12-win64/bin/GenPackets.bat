@echo off
setlocal
pushd %~dp0protoc-21.12-win64\bin

REM C# 코드 생성 (--csharp_out 옵션)
protoc.exe -I=./ --csharp_out=../../Assets/Scripts/Server/Proto ./Enum.proto
protoc.exe -I=./ --csharp_out=../../Assets/Scripts/Server/Proto ./Struct.proto
protoc.exe -I=./ --csharp_out=../../Assets/Scripts/Server/Proto ./Protocol.proto

REM GenPackets는 C# 옵션 추가 (만약 지원한다면)
REM GenPackets.exe --path=./Protocol.proto --output=PacketHandler --recv=C_ --send=S_ --csharp_out=../../Assets/Scripts/Server/Proto

echo.
echo ==========================================
echo C# Proto files generated successfully!
echo ==========================================
pause
