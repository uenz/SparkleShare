@echo off
REM delete wix in (user).dotnet/tools
REM dotnet tool install wix --create-manifest-if-needed

REM set WinDirNet=%WinDir%\Microsoft.NET\Framework
REM set msbuild="%WinDirNet%\v4.0\msbuild.exe"
REM if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set WIX=C:\Program Files (x86)\WiX Toolset v3.14
set wixBinDir=%WIX%\bin
set OutputDir=%~dp0bin
if not exist "%OutputDir%" mkdir "%OutputDir%"

dotnet build "%~dp0..\..\SparkleShare.sln" /target:SparkleShare_Windows:Rebuild /p:Configuration=ReleaseWindows /p:Platform="Any CPU" -m

if "%1"=="installer" (
	rem dotnet tool install --global wix
	dotnet restore
	rem dotnet tool install wix --create-manifest-if-needed
	dotnet build "%~dp0..\..\SparkleShare.sln" /target:SparkleShare_Windows_Installer:Rebuild /p:Configuration=ReleaseWindows /p:Platform="Any CPU" -m
	REM if exist "%wixBinDir%" (
	REM   if exist "%~dp0\SparkleShare.msi" del "%~dp0\SparkleShare.msi"
	REM 	"%wixBinDir%\heat.exe" dir "%OutputDir%\git_scm" -cg gitScmComponentGroup -gg -scom -sreg -sfrag -srd -dr GITSCM_DIR -var wix.gitscmpath -o "%~dp0\git_scm.wxs"
	REM 	"%wixBinDir%\heat.exe" dir "%OutputDir%\Images"  -cg ImagesComponentGroup  -gg -scom -sreg -sfrag -srd -dr IMAGES_DIR  -var wix.imagespath  -o "%~dp0\images.wxs"
	REM 	"%wixBinDir%\heat.exe" dir "%OutputDir%\Presets" -cg PresetsComponentGroup -gg -scom -sreg -sfrag -srd -dr PRESETS_DIR -var wix.presetspath -o "%~dp0\presets.wxs"
	REM 	"%wixBinDir%\heat.exe" dir "%OutputDir%\runtimes" -cg runtimesComponentGroup -gg -scom -sreg -sfrag -srd -dr PRESETS_DIR -var wix.runtimespath -o "%~dp0\runtimes.wxs"
	REM 	"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
	REM 	"%wixBinDir%\candle" "%~dp0\git_scm.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
	REM 	"%wixBinDir%\candle" "%~dp0\images.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
	REM 	"%wixBinDir%\candle" "%~dp0\presets.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
	REM 	"%wixBinDir%\candle" "%~dp0\runtimes.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
	REM 	"%wixBinDir%\light" -ext WixUIExtension -ext WixUtilExtension "%~dp0Sparkleshare.wixobj" "%~dp0git_scm.wixobj" "%~dp0images.wixobj" "%~dp0presets.wixobj" "%~dp0runtimes.wixobj" -droot="%~dp0." -dgitscmpath="%OutputDir%\git_scm" -dimagespath="%OutputDir%\Images"  -dpresetspath="%OutputDir%\Presets" -druntimespath="%OutputDir%\runtimes" -o "%~dp0SparkleShare.msi"
	REM 	if exist "%~dp0\SparkleShare.msi" echo SparkleShare.msi created.
	REM ) else (
	REM 	echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	REM   echo wix is available at http://wix.sourceforge.net/
	REM 	SET ERRORLEVEL=2
	REM )
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)

