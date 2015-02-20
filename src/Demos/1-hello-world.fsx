#load "helpers.fsx"
#load "credentials.fsx"
open MBrace
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open Helpers

(**
 This demo shows how to send a simple computation to an mbrace cluster

 A guide to creating the cluster is here: https://github.com/elastacloud/mbrace-on-brisk-starter/issues/4.

 Before you create your cluster you will need Azure Cloud Storage and an Azure Service Bus.

 *Make sure you have a queue called "mbraceruntimetaskqueue" in your Azure Service Bus before you create your cluster.*

 Before running, edit credentials.fsx to enter your connection strings.

 **)

// First connect to the cluster using a configuration to bind to your storage and service bus on Azure.
//
// Before running, edit credentials.fsx to enter your connection strings.

let config = 
    { Configuration.Default with
        StorageConnectionString = myStorageConnectionString
        ServiceBusConnectionString = myServiceBusConnectionString }

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
