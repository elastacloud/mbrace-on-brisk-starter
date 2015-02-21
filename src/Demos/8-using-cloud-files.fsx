#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open System

(**
 This tutorial illustrates creating and using cloud files, and then processing them using cloud streams.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// Here's some data
let smallData = "Some data" 

// Upload the data to a cloud file (held in blob storage). A fresh name is generated 
// for the could file.
let anonCloudFile = ["Hello World"; "How are you" ] |> CloudFile.WriteLines |> cluster.Run

// Run a cloud job which reads all the lines of a cloud file:
let numberOfLinesInFile = 
    cloud { let! data = anonCloudFile.Read CloudFile.ReadAllLines |> Cloud.OfAsync
            return data.Length }
    |> cluster.Run

// Get all the directories in the cloud file system
let directories = cluster.StoreClient.CloudFile.EnumerateDirectories() |> Async.RunSynchronously

// Create a directory in the cloud file system
let dp = cluster.StoreClient.CloudFile.CreateUniqueDirectoryPath() |> Async.RunSynchronously
cluster.StoreClient.CloudFile.CreateDirectory(dp) |> Async.RunSynchronously

// Upload data to a cloud file (held in blob storage) where we give the cloud file a name.
let namedCloudFile = 
    let lines = [for i in 0 .. 1000 -> "Item " + string i + ", " + string (i * 100) ] 
    CloudFile.WriteLines(lines,path=dp + "/file1") |> cluster.Run

// Access the cloud file as part of a cloud job
let numberOfLinesInNamedFile = 
    cloud { let! data = CloudFile.Read(namedCloudFile, CloudFile.ReadAllLines) 
            return data.Length }
    |> cluster.Run


(** 

Now we generate a collection of cloud files and process them using cloud streams.

**)

// Generate 100 cloud files in the cloud storage
let namedCloudFilesProcess = 
    [ for i in 1 .. 100 do 
        // Note that we generate the contents of the files in the cloud - this cloud
        // computation below only captures and sends an integer.
        yield cloud { let lines = [for j in 1 .. 100 -> "File " + string i + ", Item " + string (i * 100 + j) + ", " + string (j + i * 100) ] 
                      let! cloudFile =  CloudFile.WriteLines(lines,path=dp + "/file" + string i) 
                      return cloudFile } ]
   |> Cloud.Parallel 
   |> cluster.CreateProcess

// Check progress
namedCloudFilesProcess.ShowInfo()

// Get the result
let namedCloudFiles = namedCloudFilesProcess.AwaitResult()

let sumOfLengthsOfLinesProcess = 
    namedCloudFiles
    |> CloudStream.ofCloudFiles CloudFile.ReadAllLines
    |> CloudStream.map (fun lines -> lines |> Array.sumBy (fun line -> line.Length))
    |> CloudStream.toArray
    |> cluster.CreateProcess // alteratively you can block on the result using cluster.Run


// Check progress
cluster.ShowProcesses()

// Check progress
sumOfLengthsOfLinesProcess.Completed

// Get the result
let sumOfLengthsOfLines = sumOfLengthsOfLinesProcess.AwaitResult()


