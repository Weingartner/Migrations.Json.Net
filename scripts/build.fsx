#I @"..\BuildTools\scripts"
#r @"..\lib\Fake\tools\FakeLib.dll"

open Fake
open Fake.MSBuildHelper
open Fake.Testing

let nugetApiKey = getBuildParam "NuGetApiKey"
let outputPath = @".\artifacts" |> FullName
let buildOutputPath = @"bin\Release"

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "RestoreDependencies" <| fun () ->
    RestorePackages()

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
                    "OutputPath", buildOutputPath
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
            Parallel = ParallelMode.All
            ErrorLevel = Error
        }
    !! @"**\bin\Release\*.Spec.dll"
    |> xUnit2 setParams
)

Target "CreateAndPublishNuGetPackage" (fun () ->
    !! "Weingartner.Json.Migration.*.nupkg"
    |> SetBaseDir ("Weingartner.Json.Migration.NuGet" @@ buildOutputPath)
    |> CopyFiles outputPath

    !! "Weingartner.Json.Migration.Analyzer.*.nupkg"
    |> SetBaseDir ("Weingartner.Json.Migration.Roslyn.NuGet" @@ buildOutputPath)
    |> CopyFiles outputPath
)

Target "Default" DoNothing

"Default" <== [ "CreateAndPublishNuGetPackage" ]
"CreateAndPublishNuGetPackage" <== [ "RunTests" ]
"RunTests" <== [ "BuildSolution" ]
"BuildSolution" <== [ "RestoreDependencies" ]
"RestoreDependencies" <== [ "Clean" ]

RunTargetOrDefault "Default"
