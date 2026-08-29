export runtimeidentifier="${1:-x64}"
export projectFolder="$(dirname "$(realpath "$0")")"
echo "${projectFolder}"
dotnet publish "${projectFolder}/../../../../SparkleShare.sln" /target:SparkleShare_Avalonia /p:Configuration=ReleaseAvalonia /p:Platform="Any CPU" -m -v:minimal /p:SelfContained=true /p:RuntimeIdentifier="osx-${runtimeidentifier}" 