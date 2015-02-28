#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams

(**
 This tutorial illustrates creating and using cloud atoms, which allow you to store data transactionally
 in cloud storage.
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)


/// Create an anoymous cloud atom with an initial value
let atom = CloudAtom.New(100) |> cluster.Run

// Check the unique ID of the atom
atom.Id

// Get the value of the atom.
//
// Note, in the February 2015 Brisk preview this operation does not always successfully complete, 
// depending on your version of Visual Studio.
let atomValue = atom  |> CloudAtom.Read |> cluster.Run

// Transactionally update the value of the atom and return a result
let atomUpdateResult = atom  |> CloudAtom.Transact (fun x -> string x,x*x) |> cluster.Run

// Have all workers atomically increment the counter in parallel
cloud {
    let! clusterSize = Cloud.GetWorkerCount()
    let updater _ = cloud { return! CloudAtom.Update (fun i -> i + 1) atom }
    do!
        Seq.init clusterSize updater
        |> Cloud.Parallel
        |> Cloud.Ignore

    return! CloudAtom.Read atom
} |> cluster.Run

// Delete the cloud atom
atom  |> CloudAtom.Delete |> cluster.Run

cluster.ShowProcesses()

cluster.ShowLogs()
