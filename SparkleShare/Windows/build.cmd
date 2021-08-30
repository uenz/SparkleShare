@echo off

set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v4.0\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
if not defined WIX set WIX="C:\Program Files (x86)\WiX Toolset v3.11\"
set wixBinDir=%WIX%\bin

if not exist %~dp0\..\..\bin mkdir %~dp0\..\..\bin
copy %~dp0\Images\sparkleshare-app.ico %~dp0\..\..\bin\

%msbuild% "%~dp0\..\..\SparkleShare.sln" /target:SparkleShare_Windows:Rebuild /p:Configuration=Release /p:Platform="Any CPU"

if "%1"=="installer" (
	if exist "%wixBinDir%" (
	  if exist "%~dp0\SparkleShare.msi" del "%~dp0\SparkleShare.msi"
		"%wixBinDir%\heat.exe" dir "%~dp0\..\..\bin\msysgit" -cg msysGitComponentGroup -gg -scom -sreg -sfrag -srd -dr MSYSGIT_DIR -var wix.msysgitpath -o msysgit.wxs
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs" -ext WixUIExtension -ext WixUtilExtension
		"%wixBinDir%\candle" "%~dp0\msysgit.wxs" -ext WixUIExtension -ext WixUtilExtension
		"%wixBinDir%\light" -ext WixUIExtension -ext WixUtilExtension Sparkleshare.wixobj msysgit.wixobj -droot="%~dp0\..\.." -dmsysgitpath="%~dp0\..\..\bin\msysgit" -dpluginsdir="%~dp0\..\..\bin\plugins"  -o SparkleShare.msi
		if exist "%~dp0\SparkleShare.msi" echo SparkleShare.msi created.
	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	  echo wix is available at http://wix.sourceforge.net/
		SET ERRORLEVEL=2
	)
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)
