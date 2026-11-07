@echo off
rem SoftAudit v2.0.0 - choose the site, run SoftAudit.exe, fall back to PowerShell if the exe cannot run.
rem Results: always C:\SoftAudit, then the site share below (SoftAudit subfolder is created if missing).
cd /d "%~dp0"

echo ==========================================
echo   SoftAudit v2.0.0 - Chon site
echo ==========================================
echo   1. LITV    (Lotte IT Vietnam)
echo   2. HOT     (Lotte Hotel)
echo   3. LDS     (Lotte Shopping)
echo   4. Coralis
echo   5. Chi luu tren may (C:\SoftAudit)
echo.
choice /c 12345 /n /m "Chon [1-5]: "
set "SITE=%errorlevel%"

set "SHARE="
if "%SITE%"=="1" set "SHARE=\\10.252.9.153\filesrv\3. DT Division\1. IDC\Customer Service Center\LCH-Tool\DATA\SoftAudit"
if "%SITE%"=="2" set "SHARE=\\10.172.25.45\Public\It\2. Setup\Quanbm\DATA\SoftAudit"
if "%SITE%"=="3" set "SHARE=\\10.168.109.254\Software\1. Tool Soft Audit\Data"
if "%SITE%"=="4" set "SHARE=\\10.181.9.26\share\Setup\Quanbm\DATA\SoftAudit"
echo.

SoftAudit.exe "%SHARE%" /nopause
if %errorlevel% neq 0 (
    echo.
    echo SoftAudit.exe could not finish - running PowerShell fallback...
    echo.
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SoftAudit-Fallback.ps1" -SharePath "%SHARE%"
)

echo.
pause
