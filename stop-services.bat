@echo off
echo Stopping all Social Network services (killing dotnet processes on ports)...
echo.

for %%p in (5210 5220 5176 5175 5177 5178) do (
    for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":%%p " ^| findstr "LISTENING"') do (
        echo Killing process on port %%p (PID: %%a)
        taskkill /PID %%a /F >nul 2>&1
    )
)

echo Done. All services stopped.
pause
