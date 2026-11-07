@echo off
rem SoftAudit v2.0.0 - runs SoftAudit.exe, falls back to PowerShell if the exe cannot run.
rem To send results to another site share, change SHARE below.
cd /d "%~dp0"
set "SHARE=\\10.168.109.254\Software\1. Tool Soft Audit\Data"

SoftAudit.exe "%SHARE%" /nopause
if %errorlevel% neq 0 (
    echo.
    echo SoftAudit.exe could not finish - running PowerShell fallback...
    echo.
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SoftAudit-Fallback.ps1" -SharePath "%SHARE%"
)

echo.
pause
