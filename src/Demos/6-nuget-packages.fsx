#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

(**
 This tutorial illustrates using other nuget packages.  You download and reference the packages as normal
 in your F# scripting, and the DLLs for the packages are automatically uploaded to the cloud workers
 as needed.

 In this sample, we use paket (http://fsprojects.fsharp.io/paket) as the tool to fetch packages from NuGet.
 You can alternatively just reference any DLLs you like using normal nuget commands.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)


//------------------------------------------
// Step 0. Get the package bootstrap. This is standard F# boiler plate for scripts that also get packages.

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

if not (System.IO.File.Exists "paket.exe") then
    let url = "https://github.com/fsprojects/Paket/releases/download/0.27.2/paket.exe" in use wc = new System.Net.WebClient() in let tmp = System.IO.Path.GetTempFileName() in wc.DownloadFile(url, tmp); System.IO.File.Move(tmp,"paket.exe");;

//------------------------------------------
// Step 1. Resolve and install the Math.NET Numerics packages. You 
// can add any additional packages you like to this step.

#r "paket.exe"

Paket.Dependencies.Install """
    source https://nuget.org/api/v2
    nuget MathNet.Numerics
    nuget MathNet.Numerics.FSharp
""";;


//------------------------------------------
// Step 2. Reference and use the packages on the local machine

#load @"packages/MathNet.Numerics.FSharp/MathNet.Numerics.fsx"

open MathNet.Numerics
open MathNet.Numerics.LinearAlgebra

let m1 = Matrix<double>.Build.Random(1000,1000)
let v1 = Vector<double>.Build.Random(1000)

v1 * m1 

//------------------------------------------
// Step 3. Run the code on MBrace. Note that the DLLs from the packages are uploaded
// automatically.

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

cluster.ShowProcesses()

let invertRandomMatricesJob = 
    [ for i in 1 .. 200 -> 
        cloud { 
             let m = Matrix<double>.Build.Random(100,100) 
             let x = (m * m.Inverse()).L1Norm()
             return x } ]
    |> Cloud.Parallel
    |> cluster.CreateProcess

// Show the progress
invertRandomMatricesJob.ShowInfo()

// Await the result
let invertRandomMatrices = invertRandomMatricesJob.AwaitResult()

