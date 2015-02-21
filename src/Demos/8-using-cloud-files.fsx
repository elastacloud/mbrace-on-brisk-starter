#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open System

(**
 This tutorial illustrates creating and using cloud files.
 
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

// Upload the data to a cloud file (held in blob storage) where we give the cloud file a name.
let namedCloudFile = 
    let lines = [for i in 0 .. 1000 -> "Item " + string i + ", " + string (i * 100) ] 
    CloudFile.WriteLines(lines,path=dp + "/file1") |> cluster.Run

let numberOfLinesInNamedFile = 
    cloud { let! data = namedCloudFile.Read CloudFile.ReadAllLines |> Cloud.OfAsync
            return data.Length }
    |> cluster.Run


