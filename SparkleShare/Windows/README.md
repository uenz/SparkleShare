## Windows
You can choose to build SparkleShare from source or to run the Windows installer.


### Installing build requirements

Install [VisualStudioCommunity](https://visualstudio.microsoft.com/de/vs/community/)
or install [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if you haven't already.

Open a command prompt and execute the following:

```
cd C:\path\to\SparkleShare-sources
cd SparkleShare\Windows
build
```
`C:\path\to\SparkleShare-sources\bin` should now contain `SparkleShare.exe`, which you can run.


### Creating a Windows installer

```
cd C:\path\to\SparkleShare-sources\SparkleShare\Windows
build installer
```

This will create `SparkleShare.msi` in the directory .\Installer\build\setup\x64\Release\en-US.


### Resetting SparkleShare settings

Remove `My Documents\SparkleShare` and `AppData\Roaming\org.sparkleshare.SparkleShare` (`AppData` is hidden by default).


### Uninstalling

You can uninstall SparkleShare through the Windows Control Panel.

