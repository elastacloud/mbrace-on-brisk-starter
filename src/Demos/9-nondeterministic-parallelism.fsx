#load "credentials.fsx"
#load "collections.fsx"
#load "primality.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open System

(**
 This tutorial illustrated nondeterministic parallel computation using Mersenne prime number searches
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

/// Distributed tryFind combinator with multicore balancing
let rec tryFind (predicate : 'T -> bool) (ts : 'T []) = cloud {
    let! schedCtx = Cloud.GetSchedulingContext() // returns a union representing the current 
                                                 // parallelism semantics for executing workflow
    match schedCtx with
    | _ when ts.Length <= 1 -> return Array.tryFind predicate ts
    | Sequential -> return Array.tryFind predicate ts
    | ThreadParallel ->
        // divide inputs by processor count and evaluate with sequential semantics
        let tss = Array.divideBy System.Environment.ProcessorCount ts
        return!
            tss
            |> Array.map (tryFind predicate >> Cloud.ToSequential)
            |> Cloud.Choice

    | Distributed ->
        // divide inputs by cluster size and evaluate with local parallelism semantics
        let! clusterSize = Cloud.GetWorkerCount()
        let tss = Array.divideBy clusterSize ts
        return!
            tss
            |> Array.map (tryFind predicate >> Cloud.ToLocal)
            |> Cloud.Choice
}

#time

// known Mersenne exponents : 9,689 and 9,941
let exponentRange = [| 9000 .. 10000 |]

/// sequential Mersenne prime search
let tryFindMersenneSeq ts = Array.tryFind Primality.isMersennePrime ts

// Real: 00:05:46.615, CPU: 00:05:48.484, GC gen0: 3192, gen1: 39, gen2: 4
tryFindMersenneSeq exponentRange

/// MBrace nondeterministic Mersenne prime search
let tryFindMersenneCloud ts = tryFind Primality.isMersennePrime ts

// ExecutionTime = 00:00:38.2472020, 3 small instance cluster
let proc = cluster.CreateProcess(tryFindMersenneCloud exponentRange, name = "LucasLehmerTest")
proc.AwaitResult()
proc.ShowInfo()