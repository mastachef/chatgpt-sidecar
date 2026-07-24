@echo off
setlocal
cd /d "%~dp0"

set "APP=Sidecar.exe"
set "LOG=%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log"

if not exist "%APP%" (
  echo Sidecar.exe was not found beside this launcher.
  echo Extract the entire ZIP to a normal folder before running Sidecar.
  echo.
  pause
  exit /b 2
)

echo Starting Sidecar...
"%APP%"
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
  echo.
  echo Sidecar exited with code %EXIT_CODE%.
  if exist "%LOG%" (
    echo Opening startup report: %LOG%
    start "" notepad.exe "%LOG%"
  ) else (
    echo No managed startup log was created.
    echo Windows may have blocked or quarantined the executable before it started.
  )
  echo.
  pause
)

endlocal
exit /b %EXIT_CODE%