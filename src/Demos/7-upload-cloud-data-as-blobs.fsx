#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open System

(**
 This tutorial illustrates uploading data to Azure Blob Storage using CloudRef and CloudArray and then using the data.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)
 
// Here's some data
let smallData = "Some data" 

// Upload the data to blob storage
let handleToSmallDataInBlob = smallData |> CloudRef.New |> cluster.Run

// Run a cloud job which reads the blob and processes the data
let lengthOfData = 
    cloud { let! data = CloudRef.Read handleToSmallDataInBlob 
            return data.Length }
    |> cluster.Run


(**
 Next we upload an array of data (each an array of tuples) as a CloudArray
 
**)

// Here is the data we're going to upload
let arrayOfData = [| for i in 0 .. 10 -> [| for j in 0 .. 1000 -> (i,j) |] |] 

// Upload it as a CloudArray
let arrayOfDataInCloud = arrayOfData |> CloudArray.New |> cluster.Run

// Now process the cloud array
let countTask = 
    arrayOfDataInCloud
    |> CloudStream.ofCloudArray
    |> CloudStream.map (fun n -> n.Length)
    |> CloudStream.toArray
    |> cluster.RunAsTask

// Check progress
cluster.ShowProcesses()

// Check progress
countTask.IsCompleted

// Acccess the result
countTask.Result

// Now process the cloud array again, using CloudStream.
// We process each element of the cloud array (each of which is itself an array).
// We then sort the results and take the top 10 elements
let sumAndSortTask = 
    arrayOfDataInCloud
    |> CloudStream.ofCloudArray
    |> CloudStream.map (Array.sumBy (fun (i,j) -> i+j))
    |> CloudStream.sortBy (fun n -> n) 10
    |> CloudStream.toArray
    |> cluster.RunAsTask

// Check progress
cluster.ShowProcesses()

// Check progress
sumAndSortTask.IsCompleted

// Acccess the result
sumAndSortTask.Result


