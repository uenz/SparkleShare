param (
    [string]$version
)

if (-not $version) {
    Write-Output "No version number specified. Usage: .\bump-version.ps1 -version VERSION_NUMBER"
} else {
    (Get-Content ../SparkleShare/Windows/SparkleShare.wxs) -replace " Version='[^']*'", " Version='$version'" | Set-Content ../SparkleShare/Windows/SparkleShare.wxs
    (Get-Content ../Sparkles/InstallationInfo.Directory.cs) -replace 'assembly:AssemblyVersion *\("[^"]*"\)', "assembly:AssemblyVersion (\"$version\")" | Set-Content ../Sparkles/InstallationInfo.Directory.cs
    (Get-Content ../meson.build) -replace "configuration.set('VERSION', '[^\"]*')", "configuration.set('VERSION', '$version')" | Set-Content ../meson.build

    $infoPlist = Get-Content ../SparkleShare/Mac/Info.plist
    $infoPlist = $infoPlist -replace '(?s)(<key>CFBundleShortVersionString<\/key>\s*<string>)[^<]*(<\/string>)', "`$1$version`$2"
    $infoPlist = $infoPlist -replace '(?s)(<key>CFBundleVersion<\/key>\s*<string>)[^<]*(<\/string>)', "`$1$version`$2"
    Set-Content ../SparkleShare/Mac/Info.plist -Value $infoPlist

    Remove-Item ../meson.build.bak -ErrorAction SilentlyContinue
    Remove-Item ../SparkleShare/Mac/Info.plist.tmp -ErrorAction SilentlyContinue
    Remove-Item ../SparkleShare/Windows/SparkleShare.wxs.bak -ErrorAction SilentlyContinue
    Remove-Item ../Sparkles/InstallationInfo.Directory.cs.bak -ErrorAction SilentlyContinue
}