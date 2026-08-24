export projectFolder="$(dirname "$(realpath "$0")")"
echo "${projectFolder}"
dotnet build "${projectFolder}/../../../../SparkleShare.sln" /target:SparkleShare_Avalonia:Rebuild /p:Configuration=DebugAvalonia /p:Platform="Any CPU" -m -v:minimal
./postBuild.sh "../../bin/Debug/net9.0/SparkleShare.Avalonia.dll" "x64"