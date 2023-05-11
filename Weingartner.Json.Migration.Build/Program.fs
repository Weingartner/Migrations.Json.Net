open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.IO
open Fake.DotNet
open Fake.Tools    


// Properties
let baseDir = (__SOURCE_DIRECTORY__ @@ "..") |> Path.GetFullPath
let buildOutputs = !! @"**\bin"
let artifactPath = baseDir @@ "artifacts"
let slnPath = baseDir @@ "Weingartner.Json.Migration.sln"
let migrationProject = baseDir @@ "Weingartner.Json.Migration" @@ "Weingartner.Json.Migration.csproj"
let analyzerProject = baseDir @@ "Weingartner.Json.Migration.Roslyn" @@ "Weingartner.Json.Migration.Roslyn.csproj"
let gitVersion = GitVersion.generateProperties(id)
let assemblySemVer = gitVersion.AssemblySemVer

let initTargets () =
    //-----------------------------------------------------------------------------
    // Target Declaration
    //-----------------------------------------------------------------------------
    Target.create "Clean" (fun _ ->
        Trace.trace "########################################### clean outputdir ###########################################"
        buildOutputs |> Seq.iter Trace.trace
        //Shell.cleanDirs buildOutputs
        Shell.cleanDir artifactPath
        Trace.log ("####################Build Version#######################: " + assemblySemVer)
    )

    // build
    let dotnetBuild proj =
        DotNet.build (fun defaults -> {
            defaults with
                Configuration = DotNet.BuildConfiguration.Release
                MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog = true }
            }) proj

    Target.create "BuildSolution" (fun _ ->
      Trace.log " -------------- restore --------------"
      !! @"**\*.csproj"
      |> Seq.filter (fun p -> not (p.Contains(".Vsix.csproj")))
      |> Seq.iter (fun p -> dotnetBuild p)
    )

    // testing
    let dotnetTest proj =
        DotNet.test (fun defaults -> {
            defaults with
                Configuration = DotNet.BuildConfiguration.Release
                MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog = true }
                NoRestore = true
                NoBuild = true
        }) proj
        
    Target.create "RunTests" (fun _ ->
        Trace.trace "########################################### run tests ###########################################"
        !! @"**\*.Spec.csproj"
        |> Seq.iter dotnetTest
    )

    // nuget packaging
    let packMSBuildParams = {
        MSBuild.CliArguments.Create() with
                            DisableInternalBinLog = true 
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

//-----------------------------------------------------------------------------
// Target Start
//-----------------------------------------------------------------------------
[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    initTargets () |> ignore
    Target.runOrDefault "Default"

    0 // return an integer exit code
