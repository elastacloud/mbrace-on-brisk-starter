#load "credentials.fsx"
#load "helpers.fsx"
#load "collections.fsx"
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
let config = 
    { Configuration.Default with
        StorageConnectionString = myStorageConnectionString
        ServiceBusConnectionString = myServiceBusConnectionString }

let cluster = Runtime.GetHandle(config)

//---------------------------------------------------------------------------
// Specify some work 

#time "on"

// Get the current thread
let getThread() = System.Threading.Thread.CurrentThread.ManagedThreadId

// Specifies 30 jobs, each computing all the primes up to 5million 
let numbers = [| for i in 1 .. 30 -> 50000000 |]

let getPrimes nmax =
    let sieve = new BitArray((nmax/2) + 1, true)
    let result = new ResizeArray<int>(nmax / 10)
    let upper = int (sqrt (float nmax))   
    
    if nmax > 1 then result.Add(2) 

    let mutable m = 1
    while 2 * m + 1 <= nmax do
       if sieve.[m] then
           let n = 2 * m + 1
           if n <= upper then 
               let mutable i = m
               while 2 * i < nmax do sieve.[i] <- false; i <- i + n
           result.Add n
       m <- m + 1
    
    result |> Seq.toArray

//---------------------------------------------------------------------------
// Runn this work in different ways on the local machine and cluster

// Calculate a whole bunch of primes on a single thread on your PC.  Performance will depend on the
// spec of your PC. Note that it is possible that your machine is more efficient than each 
// individual machine in the cluster.
let localMachineSingleThreadedAnswer =
    numbers
    |> Array.map(fun num -> 
         let primes = getPrimes num
         sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()))


// Run in the cluster, single threaded, on a single random worker.
// This doesn't exploit the multiple worker nor multiple cores in the cluster. Performance
// will depend on the specification of your machines in the cluster.
//
// Sample time: Real: 00:00:42.830, CPU: 00:00:03.843, GC gen0: 72, gen1: 11, gen2: 0
let clusterAnswerSingleMachineSingleThreaded =
    cloud { 
     return numbers |> Array.map(fun n -> 
             let primes = getPrimes n 
             sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()))
     } |>  cluster.Run


// Run in the cluster, on a single randome worker, multi-threaded. This exploits the
// mutli-core nature of a single random machine in the cluster.  Performance
// will depend on the specification of your machines in the cluster.
//
// Sample time: Real: 00:00:24.236, CPU: 00:00:03.000, GC gen0: 53, gen1: 10, gen2: 0
let clusterAnswerSingleWorkerMultiThreaded =
    cloud { 
     return 
       numbers
       |> Array.divideBy System.Environment.ProcessorCount
       |> Array.Parallel.collect(fun nums -> 
         [| for n in nums do 
             let primes = getPrimes n 
             yield sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()) |])
     } |>  cluster.Run


// Run in the cluster, on multiple workers, each single-threaded. This exploits the
// the multiple machines (workers) in the cluster but each worker is running single-threaded.
//
// Sample time: Real: 00:00:16.269, CPU: 00:00:02.906, GC gen0: 47, gen1: 44, gen2: 1
let clusterAnswerMultiWorkerSingleThreaded =
    numbers
    |> Array.map(fun num -> 
         cloud { let primes = getPrimes num
                 return sprintf "calculated %d primes %A on machine '%s' thread %d" primes.Length primes Environment.MachineName (getThread()) })
    |> Cloud.Parallel
    |> cluster.Run

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
let clusterAnswerMultiWorkerMultiThreaded =
    numbers
    |> Array.divideBy clusterWorkerCount
    |> Array.map(fun nums -> 
         cloud { 
           return
               nums
               |> Array.divideBy System.Environment.ProcessorCount
               |> Array.Parallel.collect(fun nums -> 
                 [| for n in nums do 
                     let primes = getPrimes n 
                     yield sprintf "calculated %d primes: %A on thread %d" primes.Length primes (getThread()) |])
          })
    |> Cloud.Parallel
    |> cluster.Run
// Sample time: Real: 00:00:12.500, CPU: 00:00:02.078, GC gen0: 33, gen1: 4, gen2: 0


(*
Some useful process control tips:
cluster.ShowProcesses()
cluster.GetWorkers()
cluster.GetProcess("5b729a7db6784dc191f645b14beede23").Kill()


*)
