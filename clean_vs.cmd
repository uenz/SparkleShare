@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
set "DEEP=0"

:parse
if "%~1"=="" goto parsed
set "ARG=%~1"
set "ARG=%ARG:"=%"

if /I "%ARG%"=="/deep"  set "DEEP=1" & shift & goto parse
if /I "%ARG%"=="-deep"  set "DEEP=1" & shift & goto parse
if /I "%ARG%"=="/h"     goto help
if /I "%ARG%"=="-h"     goto help
if /I "%ARG%"=="/?"     goto help

set "ROOT=%~1"
shift
goto parse

:parsed
for %%I in ("%ROOT%") do set "ROOT=%%~fI"
if not exist "%ROOT%\" (
  echo [ERROR] Root path does not exist: "%ROOT%"
  exit /b 2
)

echo.
echo Cleaning under "%ROOT%"  (DEEP=%DEEP%)
echo.

call :rmdir_if_exists "%ROOT%\.vs"
call :rmdir_if_exists "%ROOT%\TestResults"

call :del_if_exists "%ROOT%\*.suo"
call :del_if_exists "%ROOT%\*.user"
call :del_if_exists "%ROOT%\*.userosscache"
call :del_if_exists "%ROOT%\*.sln.docstates"

echo [INFO] Removing bin/obj recursively...
for /d /r "%ROOT%" %%D in (bin,obj) do (
  echo "%%~fD" | findstr /I "\\node_modules\\" >nul
  if errorlevel 1 call :rmdir_if_exists "%%~fD"
)

if "%DEEP%"=="1" (
  call :rmdir_if_exists "%ROOT%\packages"
)

echo.
echo [DONE]
exit /b 0


:help
echo Usage:
echo   %~nx0 [rootPath] [/deep]
echo.
exit /b 0


:rmdir_if_exists
set "TARGET=%~1"
if exist "%TARGET%\" (
  echo [DEL] "%TARGET%"
  rd /s /q "%TARGET%" >nul 2>&1
)
goto :eof


:del_if_exists
set "PATTERN=%~1"
for %%G in (%PATTERN%) do (
  if exist "%%~fG" (
    echo [DEL] "%%~fG"
    del /f /q "%%~fG" >nul 2>&1
  )
)
goto :eof