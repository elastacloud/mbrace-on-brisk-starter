#load "credentials.fsx"
#load "sieve.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System

(**
 This tutorial illustrates using the CloudStream programming model that is part of MBrace for cloud-scheduled
 streamed data flow tasks.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// streaming with LINQ-style distributed operations
open Nessos.Streams
open MBrace.Streams

let streamTask = 
    [| 1..100 |]
    |> CloudStream.ofArray
    |> CloudStream.map (fun num -> num * num)
    |> CloudStream.filter(fun num -> num < 2500)
    |> CloudStream.map(fun num -> if num % 2 = 0 then "Even" else "Odd")
    |> CloudStream.countBy id
    |> CloudStream.toArray
    |> cluster.RunAsTask

cluster.ShowProcesses()

// Check if the work is done
streamTask.IsCompleted

// Look at the result
streamTask.Result

(** 

 Do some more serious work. Primes! More Primes!

**)

let numbers = [| for i in 1 .. 30 -> 50000000 |]

let streamComputeTask = 
    numbers
    |> CloudStream.ofArray
    |> CloudStream.map Sieve.getPrimes
    |> CloudStream.map (fun primes -> sprintf "calculated %d primes: %A" primes.Length primes)
    |> CloudStream.toArray
    |> cluster.RunAsTask // alteratively you can block on the result using cluster.Run

// Check if the work is done
streamComputeTask.IsCompleted

// Look at it running
cluster.ShowProcesses()

// Wait for the result
streamComputeTask.Result

