#load "credentials.fsx"

open MBrace
open MBrace.Workflows
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

(**
 This tutorial illustrates creating and using cloud files, and then processing them using cloud streams.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// Here's some data
let linesOfFile = ["Hello World"; "How are you" ] 

// Upload the data to a cloud file (held in blob storage). A fresh name is generated 
// for the could file.
let anonCloudFile = linesOfFile |> CloudFile.WriteAllLines |> cluster.Run

// Run a cloud job which reads all the lines of a cloud file:
let numberOfLinesInFile = 
    cloud { let! data = CloudFile.ReadAllLines anonCloudFile
            return data.Length }
    |> cluster.Run

// Get all the directories in the cloud file system
let directories = cluster.DefaultStoreClient.FileStore.Directory.Enumerate()

// Create a directory in the cloud file system
let dp = cluster.DefaultStoreClient.FileStore.Directory.Create()

// Upload data to a cloud file (held in blob storage) where we give the cloud file a name.
let namedCloudFile = 
    let lines = [for i in 0 .. 1000 -> "Item " + string i + ", " + string (i * 100) ] 
    CloudFile.WriteAllLines(lines, path = dp.Path + "/file1") |> cluster.Run

// Access the cloud file as part of a cloud job
let numberOfLinesInNamedFile = 
    cloud { let! data = CloudFile.ReadAllLines namedCloudFile 
            return data.Length }
    |> cluster.Run


(** 

Now we generate a collection of cloud files and process them using cloud streams.

**)

// Generate 100 cloud files in the cloud storage
let namedCloudFilesJob = 
    [ for i in 1 .. 100 do 
        // Note that we generate the contents of the files in the cloud - this cloud
        // computation below only captures and sends an integer.
        yield cloud { let lines = [for j in 1 .. 100 -> "File " + string i + ", Item " + string (i * 100 + j) + ", " + string (j + i * 100) ] 
                      let! cloudFile =  CloudFile.WriteAllLines(lines,path=dp.Path + "/file" + string i) 
                      return cloudFile } ]
   |> Cloud.Parallel 
   |> cluster.CreateProcess

// Check progress
namedCloudFilesJob.ShowInfo()

// Get the result
let namedCloudFiles = namedCloudFilesJob.AwaitResult()

// Compute 
let sumOfLengthsOfLinesJob =
    let getLineCount (file : CloudFile) = local { let! lines = CloudFile.ReadAllLines file in return lines.Length }
    let combineLineCounts c1 c2 = local { return c1 + c2 }
    namedCloudFiles 
    |> Distributed.mapReduce getLineCount combineLineCounts 0
    |> cluster.CreateProcess


// Check progress
sumOfLengthsOfLinesJob.ShowInfo()

// Get the result
let sumOfLengthsOfLines = sumOfLengthsOfLinesJob.AwaitResult()


