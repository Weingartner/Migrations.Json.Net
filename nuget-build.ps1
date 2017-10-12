$version=gitversion /output json /showvariable FullSemVer
$csprojdir="Weingartner.Json.Migration_"
$csproj="$csprojdir\Weingartner.Json.Migration.csproj"
$pkg="$csprojdir\artifacts\Weingartner.Json.Migration.$version.nupkg"
dotnet pack -o ./artifacts  --configuration Release /p:Version=$version $csproj
Write-Host $pkg
invoke-item $pkg
#dotnet nuget push $pkg
