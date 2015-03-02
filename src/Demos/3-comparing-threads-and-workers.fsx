#load "credentials.fsx"
#load "lib/collections.fsx"
#load "lib/sieve.fsx"

open System
open System.Collections
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

(**
 This demo illustrates the different way of calculating scalable workloads

 Before running, edit credentials.fsx to enter your connection strings.
**)


// First connect to the cluster
let cluster = Runtime.GetHandle(config)

//---------------------------------------------------------------------------
// Specify some work 

#time "on"

// Get the current thread
let getThread() = System.Threading.Thread.CurrentThread.ManagedThreadId

// Specifies 30 jobs, each computing all the primes up to 5million 
let numbers = [| for i in 1 .. 30 -> 50000000 |]


(**

 Now run this work in different ways on the local machine and cluster

 In each case, you calculate a whole bunch of primes.

**)
// Run this work in different ways on the local machine and cluster

// Run on your local machine, single-threaded.
//
// Performance will depend on the spec of your machine. Note that it is possible that 
// your machine is more efficient than each individual machine in the cluster.
let localMachineSingleThreaded =
    numbers
    |> Array.map(fun num -> 
         let primes = Sieve.getPrimes num
         sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()))

// Run in parallel on the cluster, on multiple workers, each single-threaded. This exploits the
// the multiple machines (workers) in the cluster but each worker is running single-threaded.
//
// Sample time: Real: 00:00:16.269, CPU: 00:00:02.906, GC gen0: 47, gen1: 44, gen2: 1
let clusterMultiWorkerSingleThreaded =
    numbers
    |> Array.map(fun num -> 
         cloud { let primes = Sieve.getPrimes num
                 return sprintf "calculated %d primes %A on machine '%s' thread %d" primes.Length primes Environment.MachineName (getThread()) })
    |> Cloud.Parallel
    |> cluster.Run


(**

 More advanced comparisons.

 In each case, you still calculate a whole bunch of primes.

**)

// Run in the cluster, single threaded, on a single random worker.
//
// This doesn't exploit the multiple worker nor multiple cores in the cluster, but gives you an
// idea of the raw performance of the machines in the  cluster. Performance
// will depend on the specification of your machines in the cluster.
//
// Sample time: Real: 00:00:42.830, CPU: 00:00:03.843, GC gen0: 72, gen1: 11, gen2: 0
let clusterSingleMachineSingleThreaded =
    cloud { 
     return numbers |> Array.map(fun n -> 
             let primes = Sieve.getPrimes n 
             sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()))
     } |>  cluster.Run


// Run in the cluster, on a single randome worker, multi-threaded. This exploits the
// mutli-core nature of a single random machine in the cluster.  Performance
// will depend on the specification of your machines in the cluster.
//
// Sample time: Real: 00:00:24.236, CPU: 00:00:03.000, GC gen0: 53, gen1: 10, gen2: 0
let clusterSingleWorkerMultiThreaded =
    cloud { 
     return 
       numbers
       |> Array.splitInto System.Environment.ProcessorCount
       |> Array.Parallel.collect(fun nums -> 
         [| for n in nums do 
             let primes = Sieve.getPrimes n 
             yield sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()) |])
     } |>  cluster.Run

// Check how many cores one of the machines in the cluster has
let clusterProcesorCountOfRandomWorker =
    cloud { return System.Environment.ProcessorCount } |>  cluster.Run

// Check how many workers there are
let clusterWorkerCount = cluster.GetWorkers() |> Seq.length

// Run in the cluster, on multiple workers, each multi-threaded. This exploits the
// the multiple machines (workers) in the cluster and each worker is running multi-threaded.
//
// We do the partitioning up-front.  
//
// Sample time: Real: 00:00:11.475, CPU: 00:00:01.921, GC gen0: 22, gen1: 12, gen2: 0
let clusterMultiWorkerMultiThreaded =
    numbers
    |> Array.splitInto clusterWorkerCount
    |> Array.map(fun nums -> 
         cloud { 
           return
               nums
               |> Array.splitInto System.Environment.ProcessorCount
               |> Array.Parallel.collect(fun nums -> 
                 [| for n in nums do 
                     let primes = Sieve.getPrimes n 
                     yield sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()) |])
          })
    |> Cloud.Parallel
    |> cluster.Run

// To complete the picture, you can also use a CloudStream programming model, see "5-cloud-streams.fsx"


(*
Some useful process control tips:

cluster.ShowProcesses()
cluster.ShowWorkers()
cluster.GetProcess("5b729a7db6784dc191f645b14beede23").Kill()
*)
