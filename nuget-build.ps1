$version=gitversion /output json /showvariable FullSemVer
$csprojdir="Weingartner.Json.Migration.Roslyn_"
$csproj="$csprojdir\Weingartner.Json.Migration.Roslyn.csproj"
$pkg="$csprojdir\artifacts\Weingartner.Json.Migration.Roslyn.$version.nupkg"
dotnet pack -o ./artifacts  --configuration Release /p:Version=$version $csproj
Write-Host $pkg
#dotnet nuget push Weingartner.Json.Migration.Roslyn\artifacts\Weingartner.Json.Migration.Roslyn.$version.nupkg