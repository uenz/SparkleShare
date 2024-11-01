<#
.DESCRIPTION
	This PowerShell script bumps the version in severas source files.
.PARAMETER version
	String with the new version.
.EXAMPLE
	PS> ./bump-version.ps1 3.8.2
	> pofershell -f bump-version.ps1 3.8.2
.NOTES
	Author: uenz
#>

param (
    [string]$version
)

if (-not $version) {
    Write-Output "No version number specified. Usage: .\bump-version.ps1 -version VERSION_NUMBER"
} else {
    # for debugging regex got to https://regex101.com/

    # replace version in installer script
    (Get-Content $PSScriptRoot/../SparkleShare/Windows/Installer/productVersion.wxi) -replace " ProductVersion=`"[^']*`"", " ProductVersion=`"$version`"" | Set-Content $PSScriptRoot/../SparkleShare/Windows/Installer/productVersion.wxi
    # replace version in assembly info
    (Get-Content $PSScriptRoot/../Sparkles/InstallationInfo.Directory.cs) -replace "assembly:AssemblyVersion *\(`"[^`']*`"\)", "assembly:AssemblyVersion (`"$version`")" | Set-Content $PSScriptRoot/../Sparkles/InstallationInfo.Directory.cs
    # replace version in meson.build
    (Get-Content $PSScriptRoot/../meson.build) -replace "configuration.set\('VERSION', ('[^`"]*')\)", "configuration.set('VERSION', '$version')" | Set-Content $PSScriptRoot/../meson.build
    $plistPath="$PSScriptRoot/../SparkleShare/Mac/Info.plist"
    # Read the content of the plist file
    $content = Get-Content -Path $plistPath -Raw
    # Define the new values for the keys
    $newValues = @{
        "CFBundleVersion" = $version
        "CFBundleShortVersionString" = $version
    }
    # Function to replace the value of a key in a plits file
    function Replace-KeyValue-InPlist {
        param (
            [string]$content,
            [string]$key,
            [string]$newValue
        )
        $pattern = "(<key>$key<\/key>\s*<string>)([^<]*)(<\/string>)"
        return [regex]::Replace($content, $pattern, "`$1 $newValue `$3")
    }
    # Replace the values for each key
    foreach ($key in $newValues.Keys) {
        $content = Replace-KeyValue-InPlist -content $content -key $key -newValue $newValues[$key]
    }

    # Write the updated content back to the plist file
    Set-Content -Path $plistPath -Value $content

    Remove-Item ../meson.build.bak -ErrorAction SilentlyContinue
    Remove-Item ../SparkleShare/Mac/Info.plist.tmp -ErrorAction SilentlyContinue
    Remove-Item ../SparkleShare/Windows/SparkleShare.wxs.bak -ErrorAction SilentlyContinue
    Remove-Item ../Sparkles/InstallationInfo.Directory.cs.bak -ErrorAction SilentlyContinue
}





