#I @"lib"
#r @"Fake\tools\FakeLib.dll"

open Fake
open Fake.MSBuildHelper
open Fake.Testing
open Fake.DotNetCli

let nugetApiKey = getBuildParam "NuGetApiKey"
let outputPath = @".\artifacts" |> FullName
let buildOutputPath = @"bin\Release"

Target "Clean" (fun _ ->
    CleanDir outputPath
)


Target "BuildSolution" (fun () ->

    //RestoreMSSolutionPackages (fun p->p) "Weingartner.Json.Migration.sln"

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
    
    let projects = !! @"**\*.Spec.csproj"
    for project in projects do
        let setParams (p: TestParams) = { p with Project = project }
        (DotNetCli.Test setParams)
)

let nugetVersion = if buildVersion = "LocalBuild" then ( environVarOrFail "GitVersion_SemVer" ) else buildVersion

Target "NugetMigration" (fun()->

    let buildDir = "Weingartner.Json.Migration" @@ buildOutputPath

    NuGet (fun p -> 
    {p with
        Authors = ["Weingartner Maschinenbau GmbH"]
        Project = "Weingartner.Json.Migration"
        Description = "Assists in migrating serialized JSON.Net objects"
        OutputPath = "./artifacts"
        Dependencies = 
            [ "Newtonsoft.Json", "12.0.3"]
        Files = [ ("Weingartner*.dll", Some "lib/netstandard2.0", None )]
        Summary = "Assists in migrating serialized JSON.Net objects"
        Version = nugetVersion 
        WorkingDir = buildDir
        AccessKey = nugetApiKey
        Publish = false }) 
        "./base.nuspec"
)

Target "NugetAnalyzer" (fun()->

    let buildDir = "Weingartner.Json.Migration.Roslyn" @@ buildOutputPath

    NuGet (fun p -> 
    {p with
        Authors = ["Weingartner Maschinenbau GMBH"]
        Project = "Weingartner.Json.Migration.Analyzer"
        Description = "Assists in migrating serialized JSON.Net objects"
        OutputPath = "./artifacts"
        Dependencies = 
            [ "Weingartner.Json.Migration", nugetVersion
              "Newtonsoft.Json", "12.0.3"
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
