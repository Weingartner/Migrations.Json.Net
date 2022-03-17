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
let baseDir = (__SOURCE_DIRECTORY__ @@ "..") |> Path.GetFullPath
let buildOutputs = !! @"**\bin"
let artifactPath = baseDir @@ "artifacts"
let slnPath = baseDir @@ "Weingartner.Json.Migration.sln"
let migrationProject = baseDir @@ "Weingartner.Json.Migration" @@ "Weingartner.Json.Migration.csproj"
let analyzerProject = baseDir @@ "Weingartner.Json.Migration.Roslyn" @@ "Weingartner.Json.Migration.Roslyn.csproj"

let gitVersion = Fake.Tools.GitVersion.generateProperties(id)

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
                    "Version", gitVersion.AssemblySemVer
                ]
            NodeReuse = false
    }
    MSBuild.build setParams slnPath
)

// Targets
Target.create "Clean" (fun _ ->
    Trace.trace "########################################### clean outputdir ###########################################"
    buildOutputs |> Seq.iter Trace.trace
    Fake.IO.Shell.cleanDirs buildOutputs
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

let packMSBuildParams = {
    MSBuild.CliArguments.Create() with
                        ToolsVersion = (Some "Current")
                        Properties =
                            [
                                "Configuration", "Release"                                
                                "Version", gitVersion.SemVer
                            ]
                        NodeReuse = false
}

Target.create "PackNugetMigration" (fun _ ->
    DotNet.pack (fun defaults -> {
        defaults with
            OutputPath = Some artifactPath
            MSBuildParams = packMSBuildParams
            NoRestore = true
            NoBuild = true
        })
        migrationProject
)

Target.create "PackNugetAnalyzer" (fun _ ->
    DotNet.pack (fun defaults -> {
        defaults with
            OutputPath = Some artifactPath
            MSBuildParams = packMSBuildParams
            NoRestore = true
            NoBuild = true
        })
        analyzerProject
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