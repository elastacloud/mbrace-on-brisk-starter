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

let streamComputationProcess = 
    [| 1..100 |]
    |> CloudStream.ofArray
    |> CloudStream.map (fun num -> num * num)
    |> CloudStream.filter (fun num -> num < 2500)
    |> CloudStream.map (fun num -> if num % 2 = 0 then "Even" else "Odd")
    |> CloudStream.countBy id
    |> CloudStream.toArray
    |> cluster.CreateProcess

// Check progress
streamComputationProcess.ShowInfo()

// Look at the result
streamComputationProcess.AwaitResult()

(** 

 Do some more serious work. Primes! More Primes!

**)

let numbers = [| for i in 1 .. 30 -> 50000000 |]

let computePrimesProcess = 
    numbers
    |> CloudStream.ofArray
    |> CloudStream.map Sieve.getPrimes
    |> CloudStream.map (fun primes -> sprintf "calculated %d primes: %A" primes.Length primes)
    |> CloudStream.toArray
    |> cluster.CreateProcess // alteratively you can block on the result using cluster.Run

// Check if the work is done
computePrimesProcess.ShowInfo()

// Wait for the result
let computePrimes = computePrimesProcess.AwaitResult()

