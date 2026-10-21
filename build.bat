@echo off
rem Builds bin\AlienKeyboardLights.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo .NET Framework 4 C# compiler not found.
  exit /b 1
)
cd /d "%~dp0"
if not exist bin mkdir bin
"%CSC%" /nologo /target:winexe /optimize+ /out:bin\AlienKeyboardLights.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
if errorlevel 1 exit /b 1
echo Built bin\AlienKeyboardLights.exe
