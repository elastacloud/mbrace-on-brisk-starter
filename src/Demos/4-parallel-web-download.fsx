#load "helpers.fsx"
#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System

(**
 This demo illustrates doing I/O tasks in parallel using the workers in the cluster
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let config = 
    { Configuration.Default with
        StorageConnectionString = myStorageConnectionString
        ServiceBusConnectionString = myServiceBusConnectionString }

let cluster = Runtime.GetHandle(config)



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
