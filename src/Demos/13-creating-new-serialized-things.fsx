﻿#load "credentials.fsx"
#r "System.Runtime.Serialization"

open System
open System.IO
open System.Runtime.Serialization

open MBrace
open MBrace.Azure

(** 

This tutorial shows you how to build a new serializable abstraction for another cloud
asset referred to by name.  In this case we build an abstraction over an Azure storage queue (not
an Azure service bus queue, which is a different beast).  Similar techniques can be used for
any cloud or web asset that can be referred to by name.


**)


[<AutoSerializable(true) ; Sealed; DataContract>]
/// An abstract item that you want to be transparently serializable to the cluster
type SerializableThing (data:string) =
    
    [<DataMember(Name = "Data")>]
    // The core data of the item
    let data = data

    [<IgnoreDataMember>]
    // Some derived data for the item
    let mutable derivedData = data.Length

    [<OnDeserialized>]
    let _onDeserialized (_ : StreamingContext) =
        // Re-establish the derived data on de-serialization
        derivedData <- data.Length

    /// Access the core data
    member __.Data = data

    /// Access the derived data
    member __.DerivedData = derivedData


//---------------------------------------------------------------------------
// Now use the serialiable thing on the cluster

// First connect to the cluster
let cluster = MBrace.Azure.Client.Runtime.GetHandle(config)


// The values to serialize
let data1 = SerializableThing("hello")
let data2 = SerializableThing("goodbye")

let job = 
  cloud { do! Cloud.Sleep 1000
          return data1.Data, data1.DerivedData, data2 }
   |> cluster.CreateProcess
     

job.ShowInfo()
job.Completed
job.AwaitResult()
