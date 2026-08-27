export projectFolder="$(dirname "$(realpath "$0")")"
echo "${projectFolder}"
dotnet publish "${projectFolder}/../../../../SparkleShare.sln" /target:SparkleShare_Avalonia:Rebuild /p:Configuration=ReleaseAvalonia /p:Platform="Any CPU" -m -v:minimal
