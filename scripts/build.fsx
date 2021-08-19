#r "paket: 
nuget FSharp.Core 4.7.2
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.NuGet
nuget Fake.Tools.GitVersion //"
#load @".\.fake\build.fsx\intellisense.fsx"

open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.DotNet.NuGet

Target.initEnvironment()

// Properties
let baseDir = __SOURCE_DIRECTORY__ @@ ".."
let buildOutputPath = @".\bin\Release"
let artifactPath = baseDir @@ "artifacts"
let slnPath = @".\Weingartner.Json.Migration.sln"
let nugetVersion = if BuildServer.isLocalBuild 
                    then (
                        let gitVersion = Fake.Tools.GitVersion.generateProperties (fun paras -> { 
                            paras with 
                                ToolPath = Environment.environVarOrFail "ChocolateyInstall" @@ @"lib\GitVersion.Portable\tools\gitversion.exe"
                            })
                        gitVersion.SemVer
                    )
                    else BuildServer.buildVersion

let msbuild target = (
    let setParams (defaults:MSBuildParams) = {
        defaults with
            Verbosity = Some(MSBuildVerbosity.Minimal)
            ToolsVersion = (Some "Current")
            Targets = [ target ]
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
            NodeReuse = false
    }
    MSBuild.build setParams slnPath
)

// Targets
Target.create "Clean" (fun _ ->
    Trace.trace "########################################### clean outputdir ###########################################"
    Fake.IO.Shell.cleanDir buildOutputPath
    Fake.IO.Shell.cleanDir artifactPath
)

Target.create "BuildSolution" (fun _ ->
    Trace.trace "########################################### msbuild restore ###########################################"
    msbuild "restore"
    Trace.trace "########################################### msbuild rebuild ###########################################"
    msbuild "rebuild"
)

let dotnet cmd workingDir = 
    let result = DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

Target.create "RunTests" (fun _ ->
    Trace.trace "########################################### run tests ###########################################"
    !! @"**\*.Spec.csproj"
    |> Seq.map Fake.IO.Path.getDirectory
    |> Seq.iter (fun dir -> dotnet "test" dir)
)

Target.create "PackNugetMigration" (fun _ ->

    let workingDir = @".\Weingartner.Json.Migration" @@ buildOutputPath
    NuGet.NuGetPack (fun p -> {
        p with
            Authors = ["Weingartner Maschinenbau GmbH"]
            Project = "Weingartner.Json.Migration"
            Description = "Assists in migrating serialized JSON.Net objects"
            OutputPath = artifactPath
            Dependencies = [ "Newtonsoft.Json", "13.0.1" ]
            Files = [ ("Weingartner*.dll", Some("lib/netstandard2.0"), None )]
            Summary = "Assists in migrating serialized JSON.Net objects"
            Version = nugetVersion 
            WorkingDir = workingDir
            Publish = false
        }) 
        "./base.nuspec"
)

Target.create "PackNugetAnalyzer" (fun _ ->

    let workingDir = @".\Weingartner.Json.Migration.Roslyn" @@ buildOutputPath

    NuGet.NuGetPack (fun p -> 
    {p with
        Authors = ["Weingartner Maschinenbau GMBH"]
        Project = "Weingartner.Json.Migration.Analyzer"
        Description = "Assists in migrating serialized JSON.Net objects"
        OutputPath = artifactPath
        Dependencies = 
            [ "Weingartner.Json.Migration", nugetVersion
              "Newtonsoft.Json", "13.0.1"
            ]
        Summary = "Assists in migrating serialized JSON.Net objects"
        Version = nugetVersion
        WorkingDir = workingDir
        Files = [ ( "Weingartner.Json.Migration.Roslyn.dll", Some "analyzers/dotnet/cs", Some "**/Microsoft.*;**/System.*") 
                  ( "tools/**/*.*", None, None)
                ]
        Publish = false 
        }) 
        "./base.nuspec"
)

Target.create "Default" (fun _ ->
    Trace.trace "Finished"
)


// Dependencies
"Clean"
    ==> "BuildSolution"
    ==> "RunTests"
    ==> "PackNugetMigration"
    ==> "PackNugetAnalyzer"
    ==> "Default"

Target.runOrDefault "Default"