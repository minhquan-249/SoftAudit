@echo off
rem SoftAudit v2.0.0 - site settings are in SoftAudit.ini (same folder).
rem Runs SoftAudit.exe; if it cannot run (e.g. .NET Framework 4.8 missing) runs the PowerShell fallback.
cd /d "%~dp0"

SoftAudit.exe /nopause
if %errorlevel% neq 0 (
    echo.
    echo SoftAudit.exe could not finish - running PowerShell fallback...
    echo.
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SoftAudit-Fallback.ps1"
)

echo.
pause
