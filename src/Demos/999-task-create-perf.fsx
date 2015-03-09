

#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open MBrace.Workflows
open Nessos.Streams

let cluster = Runtime.GetHandle(config)

cloud { return "" } |> cluster.Run


let  ps() = 
 cloud { let tasks = new ResizeArray<_>()
         let! worker = Cloud.CurrentWorker
         for i in [ 0 .. 200 ] do 
             let! x = Cloud.StartAsCloudTask (cloud { do! Cloud.Sleep 10000; 
                                                      return 1 }, target=worker)
             tasks.Add x
         for t in tasks.ToArray() do 
             let! res = t.AwaitResult()
             ()
        }

let job = 
   cloud { return! ps() }
     |> cluster.CreateProcess


job.ShowInfo()
cluster.ShowWorkers()

cluster.GetProcess("69eb7a0faf854f6186fef069c5059a97").Kill()


//154595 jobs local in 10sec

cluster.ShowProcesses()

// Tasks: run a public-facing web server in the cluster
// Tasks: run 10 public-facing web server(s) in the cluster
// Tasks: run an incremental computation server in the cluster. You can feed it inputs rapidly, it produces as synthesis

let webServer = 
    cloud { 
        let ws1,ws2 = starWebServerAsync(webServerConfig) 
        let! started = Cloud.OfAsync ws1

        }

