@echo off
setlocal

set PROTOC=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin\protoc.exe
set PROTO_DIR=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin
set OUTPUT_DIR=C:\Users\owner\Desktop\Files\TeamProject\Client\Assets\Scripts\Server\Proto


if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Generating C# files from proto...
"%PROTOC%" -I="%PROTO_DIR%" --csharp_out="%OUTPUT_DIR%" "%PROTO_DIR%\Enum.proto"
"%PROTOC%" -I="%PROTO_DIR%" --csharp_out="%OUTPUT_DIR%" "%PROTO_DIR%\Struct.proto"
"%PROTOC%" -I="%PROTO_DIR%" --csharp_out="%OUTPUT_DIR%" "%PROTO_DIR%\Protocol.proto"

echo.
echo ==========================================
echo Generated files:
echo - Enum.cs
echo - Struct.cs
echo - Protocol.cs
echo Output: %OUTPUT_DIR%
echo ===========
pause