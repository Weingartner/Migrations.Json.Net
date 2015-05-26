#I @"..\BuildTools\scripts"
#r @"..\lib\Fake\tools\FakeLib.dll"
#load @"GitVersion.fsx"

open Fake
open Fake.MSBuildHelper
open Fake.XUnit2Helper

let nugetApiKey = getBuildParam "NuGetApiKey"
let outputPath = @".\artifacts" |> FullName

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "BuildSolution" (fun () ->
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
                    "OutputPath", @"bin\Release"
                ]
            Verbosity = Some(MSBuildVerbosity.Normal)
            NodeReuse = false
    }
    build setParams slnPath
)

Target "RunTests" (fun () ->
    let setParams (p: XUnit2Params) =
        { p with
            ToolPath = @"BuildTools\lib\xunit.runner.console\tools\xunit.console.exe"
            Parallel = ParallelOption.All
            ErrorLevel = Error
        }
    !! @"**\bin\Release\*.Spec.dll"
    |> xUnit2 setParams
)

Target "CreateAndPublishNuGetPackage" (fun () ->
    let setParams (p: NuGetParams) =
        { p with
            ToolPath = @"BuildTools\lib\NuGet.CommandLine\tools\NuGet.exe"
//            Title = "Migrations.Json.Net"
            Version = GitVersion.Vars.NuGetVersion
            OutputPath = outputPath
            Publish = false 
        }
    NuGet setParams @"NuGet\Weingartner.Json.Migration.Fody.nuspec"
)

Target "Default" DoNothing

"Default" <== [ "CreateAndPublishNuGetPackage" ]
"CreateAndPublishNuGetPackage" <== [ "RunTests" ]
"RunTests" <== [ "BuildSolution" ]
"BuildSolution" <== [ "Clean" ]

RunTargetOrDefault "Default"
