@echo off
setlocal
echo ============================================
echo   Building Brave Browser UI Edition...
echo ============================================

if not exist bin mkdir bin

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# compiler not found at %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /optimize+ /out:bin\AuraBrowser.exe /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,Microsoft.CSharp.dll src\*.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Built successfully: bin\AuraBrowser.exe
) else (
    echo [FAILED] Compilation failed with error code %ERRORLEVEL%
)

endlocal
