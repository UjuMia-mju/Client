@echo off
setlocal

REM 배치파일 기준 경로
set SCRIPT_DIR=%~dp0
set ROOT_DIR=%SCRIPT_DIR%..\..

REM protoc.exe 경로
set PROTOC=%SCRIPT_DIR%protoc.exe

REM proto 파일 디렉터리 (bin 폴더)
set PROTO_DIR=%SCRIPT_DIR%

REM 출력 디렉터리
set OUTPUT_DIR=%ROOT_DIR%\Assets\Scripts\Server\Proto


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