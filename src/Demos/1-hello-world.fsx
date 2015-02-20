#load "helpers.fsx"
open MBrace
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open Helpers

(**
 This demo shows how to send a simple computation to an mbrace cluster

 A guide to creating the cluster is here: https://github.com/elastacloud/mbrace-on-brisk-starter/issues/4.

 Before you create your cluster you will need Azure Cloud Storage and an Azure Service Bus.

 *Make sure you have a queue called "mbraceruntimetaskqueue" in your Azure Service Bus before you create your cluster.*

 **)

// First connect to the cluster using a configuration to bind to your storage and service bus on Azure.
//
// The connection strings can be found under "Cloud Service" --> "Configure" --> scroll down to "MBraceWorkerRole"
//
// The storage connection string is of the form 
//    DefaultEndpointsProtocol=https;AccountName=myAccount;AccountKey=myKey
//
// The service bus connection string can also be found in the Azure management portal under
// "Manage Connection Strings" for the service bus

// The helper functions createStorageConnectionString and createServiceBusConnectionString are used to correctly form a connection strings.

let config = 
    { Configuration.Default with
        StorageConnectionString = createStorageConnectionString("storageAccount", "key")
        ServiceBusConnectionString = createServiceBusConnectionString("serviceBus", "key") }
let cluster = Runtime.GetHandle(config)

// We can connect to the cluster and get details of the workers in the pool etc.
cluster.ShowWorkers()

// Create a cloud workflow, don't execute it
let workflow = cloud { return "Hello world!" }

// Actually execute the workflow and get a handle to the overall job
let job = workflow |> cluster.CreateProcess

// You can evaluate helloWorldProcess to get details on it
let isJobComplete = job.Completed

// Block until the result is computed by the cluster
let text = job.AwaitResult()

// Alternatively we can do this all in one line
let quickText = cloud { return "Hello world!" } |> cluster.Run
