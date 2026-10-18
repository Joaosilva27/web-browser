@echo off
setlocal
echo ============================================
echo   Building Brave Browser Desktop...
echo ============================================

if not exist bin mkdir bin
if not exist lib\Microsoft.Web.WebView2.WinForms.dll (
    echo [INFO] Downloading Chromium WebView2 libraries...
    powershell -ExecutionPolicy Bypass -File scripts\get_webview2.ps1
)

copy /Y lib\*.dll bin\ >nul 2>&1

if not exist bin\filters mkdir bin\filters
copy /Y scripts\*.adblock bin\filters\ >nul 2>&1

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# compiler not found at %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /optimize+ /out:bin\AuraBrowser.exe /r:System.dll,System.Core.dll,System.Windows.Forms.dll,System.Drawing.dll,Microsoft.CSharp.dll,lib\Microsoft.Web.WebView2.WinForms.dll,lib\Microsoft.Web.WebView2.Core.dll src\*.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Built successfully: bin\AuraBrowser.exe
) else (
    echo [FAILED] Compilation failed with error code %ERRORLEVEL%
)

endlocal
