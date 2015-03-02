

#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open MBrace.Workflows
open Nessos.Streams

let cluster = Runtime.GetHandle(config)

let ps = 
 [for i in 0 .. 10000 ->
   printfn "starting %d, time = %A" i System.DateTime.Now.TimeOfDay
   cloud { return System.DateTime.Now }
    |> cluster.CreateProcess ]

cluster.ShowProcesses()


