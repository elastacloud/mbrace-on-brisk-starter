#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open MBrace.Workflows

(**
 This tutorial illustrates creating and using cloud channels, which allow you to send messages between
 cloud workflows.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)

// Create an anonymous cloud channel
let send1,recv1 = CloudChannel.New<string>() |> cluster.Run

// Send to the channel by scheduling a cloud process to do the send 
CloudChannel.Send (send1, "hello") |> cluster.Run

// Receive from the channel by scheduling a cloud process to do the receive 
let msg = CloudChannel.Receive(recv1) |> cluster.Run

cloud { for i in [ 0 .. 99 ] do 
            do! send1 <-- sprintf "hello%d" i }
 |> cluster.Run


// Await for the 100 messages.  
cloud { let results = ResizeArray()
        for i in [ 0 .. 99 ] do 
           let! msg = CloudChannel.Receive(recv1)
           results.Add msg
        return results.ToArray() }
 |> cluster.Run


