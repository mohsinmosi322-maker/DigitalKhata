@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo   Digital Khata - Build Script
echo ==========================================
echo.

set CSC_PATH=
set DLL_DIR=

if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
    set DLL_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319
    goto :found
)
if exist "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
    set DLL_DIR=C:\Windows\Microsoft.NET\Framework\v4.0.30319
    goto :found
)

echo ERROR: C# Compiler not found!
pause
exit /b 1

:found
echo Compiling Digital Khata...
"%CSC_PATH%" /nologo /optimize+ /r:"%DLL_DIR%\System.Web.Extensions.dll" /r:"%DLL_DIR%\System.Data.dll" /out:DigitalKhata.exe Program.cs Pages.cs Apis.cs

if %errorlevel% neq 0 (
    echo Compilation Failed!
    pause
    exit /b 1
)

echo Compilation Successful!
echo.
echo Starting Application...
DigitalKhata.exe
pause