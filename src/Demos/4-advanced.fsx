#load "helpers.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System
open Helpers

// Connect to mbrace

// First connect to the cluster
let config = 
    { Configuration.Default with
        StorageConnectionString = createStorageConnectionString("storageAccount", "key")
        ServiceBusConnectionString = createServiceBusConnectionString("serviceBus", "key") }
let cluster = Runtime.GetHandle(config)


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
|> cluster.Run



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

let files = 
    urls 
    |> Array.map download
    |> Cloud.Parallel
    |> cluster.Run


let read (file: MBrace.CloudFile) = cloud {
    let! text = CloudFile.Read(file, CloudFile.ReadAllText)
    return (file.FileName, text.Length)
}

let proc' = 
    files
    |> Array.map read
    |> Cloud.Parallel
    |> cluster.Run
