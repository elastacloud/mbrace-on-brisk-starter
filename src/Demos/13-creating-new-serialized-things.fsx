#load "credentials.fsx"
#r "System.Runtime.Serialization"

open System
open System.IO
open System.Runtime.Serialization

open MBrace
open MBrace.Azure

// First connect to the cluster
let cluster = MBrace.Azure.Client.Runtime.GetHandle(config)


[<AutoSerializable(true) ; Sealed; DataContract>]
type SerializableThing (data:string) =
    
    [<DataMember(Name = "Data")>]
    let data = data

    [<IgnoreDataMember>]
    let mutable derivedData = data.Length

    [<OnDeserialized>]
    let _onDeserialized (_ : StreamingContext) =
        derivedData <- data.Length

    member __.Data = data
    member __.DerivedData = derivedData

let data = SerializableThing("hello")

let job = 
  cloud { do! Cloud.Sleep 1000
          return data.Data, data.DerivedData }
   |> cluster.CreateProcess
     

job.ShowInfo()
job.Completed
job.AwaitResult()
