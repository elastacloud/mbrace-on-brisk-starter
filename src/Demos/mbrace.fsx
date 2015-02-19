#load "addreferences.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System.Threading
open System

// Connect to mbrace
let config = 
    { Configuration.Default with
        StorageConnectionString = "put your storage connection string here"
        ServiceBusConnectionString = "put your service bus connection string here" }

let runtime = Runtime.GetHandle(config)

runtime.ShowWorkers()
runtime.ShowProcesses()
runtime.ShowLogs()

runtime.ClientLogger.Attach(Common.ConsoleLogger())






let numbers = [| 1 .. 100 |]

let getThread() = Thread.CurrentThread.ManagedThreadId

// single threaded
numbers |> Array.map(fun num -> sprintf "calculated %d squared is %d on thread %d" num (num * num) (getThread()))

// multi threaded
numbers |> Array.Parallel.map(fun num -> sprintf "calculated %d squared is %d on thread %d" num (num * num) (getThread()))

// distributed
let work =
    numbers
    |> Array.map(fun num -> cloud { return sprintf "calculated %d squared is %d on machine %s and thread %d" num (num * num) Environment.MachineName (getThread()) })
    |> Cloud.Parallel
    |> runtime.Run




// streaming with LINQ-style distributed operations
open Nessos.Streams
open MBrace.Streams

[| 1..100 |]
|> CloudStream.ofArray
|> CloudStream.map (fun num -> num * num)
|> CloudStream.filter(fun num -> num < 2500)
|> CloudStream.map(fun num -> if num % 2 = 0 then "Even" else "Odd")
|> CloudStream.countBy id
|> CloudStream.toArray
|> runtime.Run
