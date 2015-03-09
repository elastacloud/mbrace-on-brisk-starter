#load "credentials.fsx"
#load "lib/sieve.fsx"

open System
open System.IO
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open MBrace.Workflows
open Nessos.Streams

(**
 This tutorial illustrates using the CloudStream programming model that is part of MBrace for cloud-scheduled
 streamed data flow tasks.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// Streaming with Drayad-LINQ-style distributed operations. Note that the default is to partition
// the input work between all available workers.
let streamComputationJob = 
    [| 1..100 |]
    |> CloudStream.ofArray
    |> CloudStream.map (fun num -> num * num)
    |> CloudStream.filter (fun num -> num < 2500)
    |> CloudStream.map (fun num -> if num % 2 = 0 then "Even" else "Odd")
    |> CloudStream.countBy id
    |> CloudStream.toArray
    |> cluster.CreateProcess

// Check progress
streamComputationJob.ShowInfo()

// Look at the result
streamComputationJob.AwaitResult()

(** 

 Do some more serious work. Primes! More Primes!

**)

let numbers = [| for i in 1 .. 30 -> 50000000 |]

let computePrimesJob = 
    numbers
    |> CloudStream.ofArray
    |> CloudStream.map Sieve.getPrimes
    |> CloudStream.map (fun primes -> sprintf "calculated %d primes: %A" primes.Length primes)
    |> CloudStream.toArray
    |> cluster.CreateProcess // alteratively you can block on the result using cluster.Run

// Check if the work is done
computePrimesJob.ShowInfo()

// Wait for the result
let computePrimes = computePrimesJob.AwaitResult()

