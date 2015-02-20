#load "helpers.fsx"

open System
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open Helpers

(**
 This demo illustrates the different way of calculating scalable workloads.
**)


// First connect to the cluster
let config = 
    { Configuration.Default with
        StorageConnectionString = createStorageConnectionString("storageAccount", "key")
        ServiceBusConnectionString = createServiceBusConnectionString("serviceBus", "key") }
let cluster = Runtime.GetHandle(config)






// 100 numbers!!
let numbers = [| 1 .. 100 |]

// Get the current thread
let getThread() = System.Threading.Thread.CurrentThread.ManagedThreadId


// Calculate the sum of squares on a single thread on your PC.
let singleThreadedAnswer =
    numbers
    |> Array.map(fun num -> sprintf "calculated %d squared is %d on thread %d" num (num * num) (getThread()))

// Calculate the sum of squares on many threads on your PC.
let multithreadedAnswer =
    numbers
    |> Array.Parallel.map(fun num -> sprintf "calculated %d squared is %d on thread %d" num (num * num) (getThread()))

// Calculate the sum of squares on many threads on many workers.
let clusterAnswer =
    numbers
    |> Array.map(fun num -> cloud { return sprintf "calculated %d squared is %d on machine %s and thread %d" num (num * num) Environment.MachineName (getThread()) })
    |> Cloud.Parallel
    |> cluster.Run
