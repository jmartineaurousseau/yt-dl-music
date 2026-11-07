@echo off
rem Compile et lance les tests des fonctions de nettoyage des titres.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist build mkdir build
"%CSC%" /nologo /target:exe /codepage:65001 /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:build\Tests.exe tests\Tests.cs src\NomsChansons.cs src\Conversion.cs src\Outils.cs src\CleUsb.cs || exit /b 1
build\Tests.exe
