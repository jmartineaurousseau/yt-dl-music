@echo off
rem Compile MusiquePourAuto.exe avec le compilateur C# inclus dans Windows
rem (.NET Framework 4.x). Aucune installation supplementaire n'est requise.
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Compilateur C# introuvable. Windows 10 ou 11 est requis.
  exit /b 1
)
if not exist build mkdir build

"%CSC%" /nologo /target:winexe /codepage:65001 /optimize+ /warnaserror+ ^
  /out:build\MusiquePourAuto.exe ^
  /win32icon:src\icone.ico ^
  /win32manifest:src\app.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.VisualBasic.dll ^
  src\*.cs
if errorlevel 1 exit /b 1
echo Compilation reussie : build\MusiquePourAuto.exe
