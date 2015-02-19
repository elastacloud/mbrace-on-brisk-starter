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



// Cloud parallel url-downloader
open System.IO
open System.Net
open MBrace.Streams

let urls = [| ("bing", "http://bing.com"); ("yahoo", "http://yahoo.com"); 
              ("google", "http://google.com"); ("msn", "http://msn.com") |]

let write (text: string) (stream: Stream) = async { 
    use writer = new StreamWriter(stream)
    writer.Write(text)
    return () 
}


let download (name: string, uri: string) = cloud {
    let webClient = new WebClient()
    let! text = Cloud.OfAsync <| webClient.AsyncDownloadString(Uri(uri))
    let! file = CloudFile.New(write text, sprintf "pages/%s.html" name)
    return file
}

let proc = 
    urls 
    |> Array.map download
    |> Cloud.Parallel
    |> runtime.CreateProcess

let files = proc.AwaitResult()


let read (file: MBrace.CloudFile) = cloud {
    let! text = CloudFile.Read(file, CloudFile.ReadAllText)
    return (file.FileName, text.Length)
}

let proc' = 
    files
    |> Array.map read
    |> Cloud.Parallel
    |> runtime.CreateProcess

let filesizes = proc'.AwaitResult()

