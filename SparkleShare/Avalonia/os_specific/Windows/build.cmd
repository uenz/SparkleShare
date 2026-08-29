@echo off
set  DOTNET_CLI_TELEMETRY_OPTOUT=1


dotnet restore "%~dp0..\..\..\..\SparkleShare.sln"
dotnet publish "%~dp0..\..\..\..\SparkleShare.sln" /target:SparkleShare_Avalonia:Rebuild /p:Configuration=ReleaseAvalonia /p:Platform="Any CPU" -m -v:minimal

if "%1"=="installer" (
	dotnet publish "%~dp0..\..\..\..\SparkleShare.sln" /target:SparkleShare_Avalonia_Installer:Rebuild /p:Configuration=ReleaseAvalonia /p:Platform="Any CPU" -m -v:minimal	
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)


