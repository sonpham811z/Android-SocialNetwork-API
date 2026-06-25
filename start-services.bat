@echo off
setlocal

set ROOT=%~dp0Back_End\src\Services

echo Starting all Social Network services...
echo.

start "Identity API :5210" cmd /k "cd /d "%ROOT%\Identity\Identity.API" && dotnet run"
timeout /t 2 /nobreak >nul

start "User API :5220" cmd /k "cd /d "%ROOT%\User\User.API" && dotnet run"
timeout /t 2 /nobreak >nul

start "Friend API :5176" cmd /k "cd /d "%ROOT%\Friend\Friend.API" && dotnet run"
timeout /t 2 /nobreak >nul

start "Post API :5175" cmd /k "cd /d "%ROOT%\Post\Post.API" && dotnet run"
timeout /t 2 /nobreak >nul

start "Message API :5177" cmd /k "cd /d "%ROOT%\Message\Message.API" && dotnet run"
timeout /t 2 /nobreak >nul

start "Notification API :5178" cmd /k "cd /d "%ROOT%\Notification\Notification.API" && dotnet run"

echo.
echo All 6 services started in separate windows.
echo   Identity    -> http://localhost:5210
echo   User        -> http://localhost:5220
echo   Friend      -> http://localhost:5176
echo   Post        -> http://localhost:5175
echo   Message     -> http://localhost:5177
echo   Notification-> http://localhost:5178
echo.
pause
