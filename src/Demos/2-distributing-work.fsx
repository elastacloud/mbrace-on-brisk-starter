#load "helpers.fsx"
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open Helpers

(**
 This demo shows how to start performing distributed workloads on MBrace clusters.
 **)

// First connect to the cluster
let config = 
    { Configuration.Default with
        StorageConnectionString = createStorageConnectionString("storageAccount", "key")
        ServiceBusConnectionString = createServiceBusConnectionString("serviceBus", "key") }
let cluster = Runtime.GetHandle(config)

// create two jobs (but don't exeute them)
let jobA = cloud { return "hello world from A" }
let jobB = cloud { return 50 }

// Compose both jobs into one
let combinedJob = jobA <||> jobB

// Submit both jobs and get the answer of both as a tuple of (string * int)
let a, b = combinedJob |> cluster.Run

// Now we can make many jobs
let lotsOfJobs = [ 1 .. 50 ] |> List.map(fun number -> cloud { return sprintf "i'm job %d" number })

// compose them all in parallel - this is analogous to Async.Parallel
let jobOfLotsOfWork = lotsOfJobs |> Cloud.Parallel

// Get the results
let results = jobOfLotsOfWork |> cluster.Run

// Again, in shorthand
let quickResults =
    [ 1 .. 50 ]
    |> List.map(fun number -> cloud { return sprintf "i'm job %d" number })
    |> Cloud.Parallel
    |> cluster.Run