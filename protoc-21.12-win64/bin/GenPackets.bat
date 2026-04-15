@echo off
setlocal

REM protoc.exe 절대경로
set PROTOC=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin\protoc.exe

REM .proto 파일 위치
set PROTO_DIR=C:\Users\owner\Desktop\Files\TeamProject\Client\protoc-21.12-win64\bin

REM 출력 경로
set OUTPUT_DIR=C:\Users\owner\Desktop\Files\TeamProject\Client\Assets\Scripts\Server\Proto

REM 출력 폴더 생성
if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

echo Generating C# files from proto...

REM proto 파일 처리
for %%f in ("%PROTO_DIR%\*.proto") do (
    echo Processing %%~nxf ...
    "%PROTOC%" -I="%PROTO_DIR%" --csharp_out="%OUTPUT_DIR%" "%%~ff"
)

echo.
echo ==========================================
echo Done!
echo Output: %OUTPUT_DIR%
echo ==========================================
pause