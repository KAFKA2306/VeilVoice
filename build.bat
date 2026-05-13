@echo off
echo === Building VeilVoice Suite v3.0 (Zero-Trust) ===

cd VeilVoice
dotnet build -c Release
if %errorlevel% neq 0 exit /b %errorlevel%
cd ..

cd VeilVoiceAcceptanceRunner
dotnet build -c Release
if %errorlevel% neq 0 exit /b %errorlevel%
cd ..

cd VeilVoiceBinaryInspector
dotnet build -c Release
if %errorlevel% neq 0 exit /b %errorlevel%
cd ..

echo === Build Complete ===
