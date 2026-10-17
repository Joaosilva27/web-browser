@echo off
setlocal

if not exist bin\AuraBrowser.exe (
    call build.bat
)

if exist bin\AuraBrowser.exe (
    echo Launching Brave Browser Edition...
    start "" bin\AuraBrowser.exe
) else (
    echo [ERROR] bin\AuraBrowser.exe not found!
    pause
)

endlocal
