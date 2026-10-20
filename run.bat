@echo off
setlocal

if not exist bin\Toge.exe (
    call build.bat
)

if exist bin\Toge.exe (
    echo Launching Toge...
    start "" bin\Toge.exe
) else (
    echo [ERROR] bin\Toge.exe not found!
    pause
)

endlocal
