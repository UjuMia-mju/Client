@echo off
setlocal
cd /d %~dp0protoc-21.12-win64\bin

REM 현재 디렉토리가 proto 파일들이 있는 위치
REM 출력 디렉토리 설정
set OUTPUT_DIR=..\..\Assets\Scripts\Server\Proto

REM 출력 디렉토리 없으면 생성
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

REM C# 코드 생성 (protoc 사용)
echo Generating C# files from proto...
protoc.exe -I=. --csharp_out=%OUTPUT_DIR% Enum.proto
protoc.exe -I=. --csharp_out=%OUTPUT_DIR% Struct.proto
protoc.exe -I=. --csharp_out=%OUTPUT_DIR% Protocol.proto

echo.
echo ==========================================
echo Generated files:
echo - Enum.cs
echo - Struct.cs
echo - Protocol.cs
echo Output: %OUTPUT_DIR%
echo ===========