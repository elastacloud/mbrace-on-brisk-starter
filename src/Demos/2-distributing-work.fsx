#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

(**
 This demo shows how to start performing distributed workloads on MBrace clusters.

 Before running, edit credentials.fsx to enter your connection strings.
 **)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// create two jobs (but don't exeute them)
let workflowA = cloud { return "hello world from A" }
let workflowB = cloud { return 50 }

// Compose both jobs into one
let combinedWorkflow = workflowA <||> workflowB

// Submit both jobs and get the answer of both as a tuple of (string * int)
let a, b = combinedWorkflow |> cluster.Run

// Now we can make many jobs
let lotsOfWorkflows = [ 1 .. 50 ] |> List.map(fun number -> cloud { return sprintf "i'm job %d" number })

// compose them all in parallel - this is analogous to Async.Parallel
let lotsOfWorkAsOneWorkflow = lotsOfWorkflows |> Cloud.Parallel

// Start the work as a cloud process
let resultsJob = lotsOfWorkAsOneWorkflow |> cluster.CreateProcess

// Get the results
let results = resultsJob.AwaitResult()

// Again, in shorthand
let quickResults =
    [ 1 .. 50 ]
    |> List.map(fun number -> cloud { return sprintf "i'm job %d" number })
    |> Cloud.Parallel
    |> cluster.Run

