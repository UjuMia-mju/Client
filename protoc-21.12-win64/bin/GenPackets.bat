@echo off
setlocal

REM protoc.exe 절대경로
set PROTOC=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin\protoc.exe

REM proto 파일 디렉터리 (bin 폴더에 있음)
set PROTO_DIR=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin

REM 출력 디렉터리
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