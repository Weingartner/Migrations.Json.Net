#I @"lib"
#r @"Fake\tools\FakeLib.dll"

open Fake
open Fake.MSBuildHelper
open Fake.Testing

let nugetApiKey = getBuildParam "NuGetApiKey"
let outputPath = @".\artifacts" |> FullName
let buildOutputPath = @"bin\Release"

Target "Clean" (fun _ ->
    CleanDir outputPath
)


Target "BuildSolution" (fun () ->

    RestoreMSSolutionPackages (fun p->p) "Weingartner.Json.Migration.sln"

    let slnPath = @".\Weingartner.Json.Migration.sln"
    let setParams defaults = {
        defaults with
            Targets = [ "Rebuild" ]
            Properties =
                [
                    "Optimize", "True"
                    "Configuration", "Release"
                    "Platform", "Any CPU"
                    "BuildParallel", "True"
                    "BuildProjectReferences", "True"
                    "DebugSymbols", "True"
                    "OutputPath", buildOutputPath
                ]
            Verbosity = Some(MSBuildVerbosity.Normal)
            NodeReuse = false
    }
    build setParams slnPath
)

let xunit_version = "2.2.0"
let xunit_folder = "xunit.runner.console."  + xunit_version

Target "RunTests" (fun () ->
    let setParams (p: XUnit2Params) =
        { p with
            ToolPath = __SOURCE_DIRECTORY__ @@ "lib" @@  xunit_folder @@ @"tools\xunit.console.exe"
            Parallel = ParallelMode.All
            ErrorLevel = TestRunnerErrorLevel.Error
        }
    !! @"**\bin\Release\*.Spec.dll"
    |> xUnit2 setParams
)

let nugetVersion = if buildVersion = "LocalBuild" then ( environVarOrFail "GitVersion_SemVer" ) else buildVersion

Target "NugetMigration" (fun()->

    let buildDir = "Weingartner.Json.Migration_" @@ buildOutputPath

    NuGet (fun p -> 
    {p with
        Authors = ["Weingartner Machinenbau GMBH"]
        Project = "Weingartner.Json.Migration"
        Description = "Assists in migrating serialized JSON.Net objects"
        OutputPath = "./artifacts"
        Dependencies = 
            [ "Newtonsoft.Json", "10.0"]
        Files = [ ("Weingartner*.dll", Some "lib/netstandard2.0", None )]
        Summary = "Assists in migrating serialized JSON.Net objects"
        Version = nugetVersion 
        WorkingDir = buildDir
        AccessKey = nugetApiKey
        Publish = false }) 
        "./base.nuspec"
)

Target "NugetAnalyzer" (fun()->

    let buildDir = "Weingartner.Json.Migration.Roslyn_" @@ buildOutputPath

    NuGet (fun p -> 
    {p with
        Authors = ["Weingartner Machinenbau GMBH"]
        Project = "Weingartner.Json.Migration.Analyzer"
        Description = "Assists in migrating serialized JSON.Net objects"
        OutputPath = "./artifacts"
        Dependencies = 
            [ "Weingartner.Json.Migration", nugetVersion
              "Newtonsoft.Json", "10.0"
            ]
        Summary = "Assists in migrating serialized JSON.Net objects"
        Version = nugetVersion
        AccessKey = nugetApiKey
        WorkingDir = buildDir
        Files = [ ( "Weingartner.Json.Migration.Roslyn.dll", Some "analyzers/dotnet/cs", Some "**/Microsoft.*;**/System.*") 
                  ( "tools/**/*.*", None, None)
                ]
        Publish = false }) 
        "./base.nuspec"
)

Target "NugetPack" DoNothing
Target "Default" DoNothing

"NugetMigration" <== [ "BuildSolution" ]
"NugetAnalyzer" <== [ "BuildSolution" ]
"NugetPack" <== [ "NugetMigration"; "NugetAnalyzer" ] 
"NugetPack" <== [ "BuildSolution" ]
"RunTests" <== [ "BuildSolution" ]

"Default" <== [ "Clean"; "NugetPack"; "RunTests" ]


RunTargetOrDefault "Default"
