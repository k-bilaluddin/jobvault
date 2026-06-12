@echo off
setlocal EnableExtensions

echo.
echo  ============================================
echo   JobVault - Starting API, Worker and Tunnel
echo  ============================================
echo.

REM -- Base paths ------------------------------------------------------------
set "SCRIPT_DIR=%~dp0"
set "API_DIR=%SCRIPT_DIR%"
set "ROOT_DIR=%SCRIPT_DIR%.."
set "WORKER_DIR=%ROOT_DIR%\JobVault.Worker"

REM -- GitHub webhook configuration ----------------------------------------
REM Recommended: set GITHUB_TOKEN once in Windows environment variables.
REM Example: setx GITHUB_TOKEN "your_new_token"
if "%GITHUB_TOKEN%"=="" (
    echo  GITHUB_TOKEN environment variable is not set.
    set /p GITHUB_TOKEN=Enter GitHub token for webhook update: 
)

set "GITHUB_OWNER=k-bilaluddin"
set "GITHUB_REPO=job-applications-vault"
set "WEBHOOK_ID=637487918"

REM -- Build solution/project ------------------------------------------------
echo  [1/5] Building...
set "SLN_FILE="
for %%f in ("%API_DIR%*.sln") do set "SLN_FILE=%%~ff"
if not defined SLN_FILE (
    for %%f in ("%ROOT_DIR%\*.sln") do set "SLN_FILE=%%~ff"
)

if defined SLN_FILE (
    echo  Building solution: %SLN_FILE%
    dotnet build "%SLN_FILE%"
) else (
    echo  No .sln file found. Building API project instead.
    dotnet build "%API_DIR%JobVault.API.csproj"
)

if errorlevel 1 (
    echo  ERROR: Build failed. Fix build errors first.
    pause
    exit /b 1
)

REM -- Start API -------------------------------------------------------------
echo  [2/5] Starting JobVault.API on port 5278...
start "JobVault-API" cmd /k "cd /d "%API_DIR%" && dotnet run --urls http://localhost:5278"

timeout /t 5 /nobreak > nul

REM -- Start Worker ----------------------------------------------------------
echo  [3/5] Starting JobVault.Worker...
if exist "%WORKER_DIR%\JobVault.Worker.csproj" (
    start "JobVault-Worker" cmd /k "cd /d "%WORKER_DIR%" && dotnet run"
) else (
    echo  WARNING: Worker project not found at: %WORKER_DIR%
)

REM -- Start Cloudflare tunnel ----------------------------------------------
echo  [4/5] Starting Cloudflare tunnel...
echo.

set "LOG_FILE=%TEMP%\jobvault_cloudflare_tunnel.log"
if exist "%LOG_FILE%" del "%LOG_FILE%"
start "JobVault-Tunnel" cmd /c "cloudflared tunnel --url http://localhost:5278 > "%LOG_FILE%" 2>&1"

echo  Waiting for tunnel URL...
set "TUNNEL_URL="
set "MAX_WAIT=30"
set "COUNTER=0"

:WAIT_FOR_URL
timeout /t 2 /nobreak > nul
set /a COUNTER+=2

for /f "delims=" %%i in ('powershell -NoProfile -Command "if (Test-Path '%LOG_FILE%') { $content = Get-Content '%LOG_FILE%' -Raw; if ($content -match 'https://[a-z0-9\-]+\.trycloudflare\.com') { $matches[0] } }"') do (
    set "TUNNEL_URL=%%i"
)

if not defined TUNNEL_URL (
    if %COUNTER% LSS %MAX_WAIT% goto WAIT_FOR_URL
    echo  ERROR: Could not capture tunnel URL. Update webhook manually.
    goto DONE
)

echo.
echo  ============================================
echo   Tunnel URL: %TUNNEL_URL%
echo  ============================================
echo.

REM -- Update GitHub Webhook URL --------------------------------------------
echo  [5/5] Updating GitHub webhook URL...
set "WEBHOOK_URL=%TUNNEL_URL%/api/webhook"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$body = '{\"config\":{\"url\":\"%WEBHOOK_URL%\",\"content_type\":\"json\"},\"active\":true}'; " ^
  "Invoke-RestMethod -Uri 'https://api.github.com/repos/%GITHUB_OWNER%/%GITHUB_REPO%/hooks/%WEBHOOK_ID%' " ^
  "-Method Patch " ^
  "-Headers @{Authorization='Bearer %GITHUB_TOKEN%'; 'User-Agent'='JobVault'; Accept='application/vnd.github+json'; 'X-GitHub-Api-Version'='2022-11-28'} " ^
  "-Body $body " ^
  "-ContentType 'application/json' | Out-Null"

if errorlevel 1 (
    echo  ERROR: GitHub webhook update failed.
    pause
    exit /b 1
)

echo.
echo  ============================================
echo   Webhook updated to: %WEBHOOK_URL%
echo   JobVault API and Worker are running.
echo  ============================================
echo.

:DONE
pause
