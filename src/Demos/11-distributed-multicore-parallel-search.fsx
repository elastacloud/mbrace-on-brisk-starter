#load "credentials.fsx"
#load "lib/collections.fsx"
#load "lib/mersenne.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open System

(**
 In this tutorial you learn how to use Cloud.Choice to do a nondeterministic parallel computation using 
 Mersenne prime number searches.
  
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

/// Distributed tryFind combinator with multicore balancing.
///
/// Searches the given array non-deterministically using divide-and-conquer,
/// first dividing according to the number of available workers, and then
/// according to the number of available cores, and then performing sequential
/// search on each machine.
let rec distributedMultiCoreTryFind (predicate : 'T -> bool) (ts : 'T []) = 
  cloud {

    // GetSchedulingContext returns a union representing the current 
    // parallelism semantics for executing workflow.
    let! schedCtx = Cloud.GetSchedulingContext()

    // Depending on the scheduling context we divide-and-conquer in different ways
    match schedCtx with
    | _ when ts.Length <= 1 -> return Array.tryFind predicate ts
    | Sequential -> 
        // Perform a sequential search
        return Array.tryFind predicate ts

    | ThreadParallel ->
        // Divide inputs by processor count and evaluate with sequential semantics
        let tss = Array.splitInto System.Environment.ProcessorCount ts
        return!
            tss
            |> Array.map (distributedMultiCoreTryFind predicate >> Cloud.ToSequential)
            |> Cloud.Choice

    | Distributed ->
        // Divide inputs by cluster size and evaluate with local parallelism semantics
        let! clusterSize = Cloud.GetWorkerCount()
        let tss = Array.splitInto clusterSize ts
        return!
            tss
            |> Array.map (distributedMultiCoreTryFind predicate >> Cloud.ToLocal)
            |> Cloud.Choice
}

#time

/// Known Mersenne exponents : 9,689 and 9,941
let exponentRange = [| 9000 .. 10000 |]

/// Sequential Mersenne prime search
let tryFindMersenneLocal ts = Array.tryFind Primality.isMersennePrime ts

// Execution time = 00:05:46.615, sample local machine
tryFindMersenneLocal exponentRange

/// MBrace distributed, multi-core, nondeterministic Mersenne prime search
let tryFindMersenneCloud ts = distributedMultiCoreTryFind Primality.isMersennePrime ts

// ExecutionTime = 00:00:38.2472020, 3 small instance cluster
let searchJob = tryFindMersenneCloud exponentRange |> cluster.CreateProcess

searchJob.ShowInfo()

searchJob.AwaitResult()


