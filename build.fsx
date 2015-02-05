﻿// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.ProcessHelper
open System
open Fake.XUnit2Helper
open System.IO
#if MONO
#else
#load "packages/SourceLink.Fake/Tools/Fake.fsx"
open SourceLink
#endif

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Eleven19.Net.Http.Formatting.Csv"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A set of media type formatters."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = summary

// List of author names (for NuGet package)
let authors = [ "Damian Reeves" ]

// Tags for your project (for NuGet package)
let tags = "WebAPI CSV MediaTypeFormatter"

// File system information 
let solutionFile  = "MediaTypeFormatters.sln"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "Eleven19" 
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "MediaTypeFormatters"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/Eleven19/MediaTypeFormatters"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// create an active pattern
let (|Bool|_|) str =
    match System.Boolean.TryParse(str) with
    | (true,bool) -> Some(bool)
    | _ -> None

// Read additional information from the release notes document
let release = 
    let notes = LoadReleaseNotes "RELEASE_NOTES.md"    
    match buildServer with
    | AppVeyor -> ReleaseNotes.New(appVeyorBuildVersion, appVeyorBuildVersion, notes.Notes)
    | _ -> notes
    
let runTests = true
let shouldGenerateDocs =     
    let result = environVarOrDefault "GenerateDocs" "false"
    match result with
    | Bool v -> v && isLocalBuild && not isMono
    | _ -> false

let testResultsDir = ".testResults"

let genFSAssemblyInfo (projectPath) =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let basePath = System.IO.Path.GetDirectoryName(projectPath)
    let fileName = basePath + "/AssemblyInfo.fs"
    // Remove readonly flag
    !! fileName
    |> SetReadOnly false

    CreateFSharpAssemblyInfo fileName
      [ Attribute.Title (projectName)
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ]

let genCSAssemblyInfo (projectPath) =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let basePath = System.IO.Path.GetDirectoryName(projectPath)
    let fileName = basePath + "/AssemblyInfo.cs"
    
    // Remove readonly flag
    !! fileName
    |> SetReadOnly false

    CreateCSharpAssemblyInfo fileName
      [ Attribute.Title (projectName)
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ]

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fsProjs =  !! "src/**/*.fsproj"
  let csProjs = !! "src/**/*.csproj"
  fsProjs |> Seq.iter genFSAssemblyInfo
  csProjs |> Seq.iter genCSAssemblyInfo
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"; testResultsDir]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "Run xUnit Tests" (fun _ ->

    // Since we don't have SSIS setup on AppVeyor skip the tests requiring the DB
    // This is unfortunate because that is quite a bit of tests
    let excludeTraits =
        match buildServer with
        | AppVeyor -> Some ("Category", "SSISDB")
        | _ -> None

    !! testAssemblies
    |> xUnit2 (fun p ->
        { p with
            ShadowCopy = false
            HtmlOutput = true;
            XmlOutput = true;
            ExcludeTraits = excludeTraits;
            OutputDir = testResultsDir})
)

Target "Run NUnit Tests" (fun _ ->
    !! testAssemblies
    |> NUnit (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

Target "RunTests" DoNothing

// --------------------------------------------------------------------------------------
// Build deployables

let copyProjectOutputs rootOutputPath projectPath =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let projectDir = System.IO.Path.GetDirectoryName(projectPath)
    let srcDir = projectDir @@ "bin/Release"    
    let destDir = rootOutputPath @@ projectName

    sprintf "Cleaning dir: %s" destDir |> log
    CleanDir destDir

    sprintf "Copying deployables for %s" projectName |> log
    let logCopy file =
        sprintf ">>>>%s" file |> log

    CopyRecursive srcDir destDir true
    |> Seq.iter logCopy


Target "CopyDeployables" (fun _ ->
    let fsProjs =  
        !! "src/**/*.fsproj"
            -- "tests/**/*Test*.fsproj"

    let csProjs = 
        !! "src/**/*.csproj"
            -- "tests/**/*Test*.csproj"
    
    let cpy = copyProjectOutputs "deployables"

    fsProjs |> Seq.iter cpy
    csProjs |> Seq.iter cpy
)

#if MONO
#else
// --------------------------------------------------------------------------------------
// SourceLink allows Source Indexing on the PDB generated by the compiler, this allows
// the ability to step through the source code of external libraries https://github.com/ctaggart/SourceLink

Target "SourceLink" (fun _ ->
    let baseUrl = sprintf "%s/%s/{0}/%%var2%%" gitRaw (project.ToLower())
    use repo = new GitRepo(__SOURCE_DIRECTORY__)
    !! "src/**/*.fsproj"
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- "**/AssemblyInfo.fs"
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv baseUrl repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    )
)
#endif

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        ("nuget/" + project + ".nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateReferenceDocs" (fun _ ->
    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:REFERENCE"] [] then
      failwith "generating reference documentation failed"
)

let generateHelp' fail debug =
    let args =
        if debug then ["--define:HELP"]
        else ["--define:RELEASE"; "--define:HELP"]
    if executeFSIWithArgs "docs/tools" "generate.fsx" args [] then
        traceImportant "Help generated"
    else
        if fail then
            failwith "generating help documentation failed"
        else
            traceImportant "generating help documentation failed"

let generateHelp fail =
    generateHelp' fail false

Target "GenerateHelp" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp true
)

Target "GenerateHelpDebug" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp' true true
)

Target "KeepRunning" (fun _ ->    
    use watcher = new FileSystemWatcher(DirectoryInfo("docs/content").FullName,"*.*")
    watcher.EnableRaisingEvents <- true
    watcher.Changed.Add(fun e -> generateHelp false)
    watcher.Created.Add(fun e -> generateHelp false)
    watcher.Renamed.Add(fun e -> generateHelp false)
    watcher.Deleted.Add(fun e -> generateHelp false)

    traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.EnableRaisingEvents <- false
    watcher.Dispose()
)

Target "GenerateDocs" DoNothing

let createIndexFsx lang =
    let content = """(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../../bin"

(**
F# Project Scaffold ({0})
=========================
*)
"""
    let targetDir = "docs/content" @@ lang
    let targetFile = targetDir @@ "index.fsx"
    ensureDirectory targetDir
    System.IO.File.WriteAllText(targetFile, System.String.Format(content, lang))

Target "AddLangDocs" (fun _ ->
    let args = System.Environment.GetCommandLineArgs()
    if args.Length < 4 then
        failwith "Language not specified."

    args.[3..]
    |> Seq.iter (fun lang ->
        if lang.Length <> 2 && lang.Length <> 3 then
            failwithf "Language must be 2 or 3 characters (ex. 'de', 'fr', 'ja', 'gsw', etc.): %s" lang

        let templateFileName = "template.cshtml"
        let templateDir = "docs/tools/templates"
        let langTemplateDir = templateDir @@ lang
        let langTemplateFileName = langTemplateDir @@ templateFileName

        if System.IO.File.Exists(langTemplateFileName) then
            failwithf "Documents for specified language '%s' have already been added." lang

        ensureDirectory langTemplateDir
        Copy langTemplateDir [ templateDir @@ templateFileName ]

        createIndexFsx lang)
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Git.Commit.Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

#load "paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
    
    // release on github
    createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes 
    // TODO: |> uploadFile "PATH_TO_FILE"    
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Paket targets
Target "Install-Dependencies" (fun _ ->
    let cfg (info: System.Diagnostics.ProcessStartInfo) =
        info.FileName <- __SOURCE_DIRECTORY__ @@ ".paket\paket.exe"
        info.WorkingDirectory <- __SOURCE_DIRECTORY__
        info.Arguments <- "install"

    match directExec cfg with
    | true -> log "Installation of dependencies succeeded!"
    | false -> log "Installation of dependencies failed!"
)

Target "Restore-Dependencies" (fun _ ->
    let cfg (info: System.Diagnostics.ProcessStartInfo) =
        info.FileName <- __SOURCE_DIRECTORY__ @@ ".paket\paket.exe"
        info.WorkingDirectory <- __SOURCE_DIRECTORY__
        info.Arguments <- "restore"

    match directExec cfg with
    | true -> log "Restoration of dependencies succeeded!"
    | false -> log "Restoration of dependencies failed!"
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "CopyDeployables"
  ==> "RunTests"
  =?> ("GenerateReferenceDocs",shouldGenerateDocs)
  =?> ("GenerateDocs",shouldGenerateDocs)
  ==> "All"
  =?> ("ReleaseDocs",shouldGenerateDocs)

"All" 
#if MONO
#else
  =?> ("SourceLink", Pdbstr.tryFind().IsSome )
#endif
  ==> "NuGet"
  ==> "BuildPackage"

"CleanDocs"
  ==> "GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"

"CleanDocs"
  ==> "GenerateHelpDebug"

"GenerateHelp"
  ==> "KeepRunning"
    
"ReleaseDocs"
  ==> "Release"

"BuildPackage"
  ==> "Release"

"Run xUnit Tests"
  ==> "RunTests"

//"Run NUnit Tests"
// ==> "RunTests"


RunTargetOrDefault "All"
