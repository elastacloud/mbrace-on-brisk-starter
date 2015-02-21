# mbrace-on-brisk-starter
Contains a set of scripts and demos to get you up and running with MBrace on Brisk.

# Get started With Brisk

Steps I used to create an [MBrace](http://www.m-brace.net/) Cluster on [Azure](https://windowsazure.com) using [Elastacloud Brisk Engine](https://www.briskengine.com/#/dash) as of 20/02/2015. See [the announcement of the pre-release availability](http://blog.brisk.elastatools.com/2015/02/19/adding-support-for-mbrace-f-and-net-on-brisk/) of this service.

Assumes you have an Azure account with at least 6 cores spare (there is a 20 core limit on some free or trial Azure accounts).

1.	Created an account with [Brisk](https://www.briskengine.com/), including entering my Azure account connection token details. 

2.	Created new Service Bus namespace in [Azure Console](https://manage.windowsazure.com) (messaging not notification hub). You can use any name but you will need to refer to it later. It's possible the Brisk guys will optionally automate this step as part of provisioning.

  ![pic1b](https://cloud.githubusercontent.com/assets/7204669/6285597/62a9e218-b8f5-11e4-830f-6c7cd7d7b02c.PNG)   ![pic1](https://cloud.githubusercontent.com/assets/7204669/6285347/a0571138-b8f2-11e4-97dd-364ff7e6bf0c.jpg)

3.	Created a new queue called *mbraceruntimetaskqueue* in that Service Bus namespace in the [Azure console](https://manage.windowsazure.com). It's possible the Brisk guys will optionally automate this step as part of provisioning.

  ![pic2](https://cloud.githubusercontent.com/assets/7204669/6285349/a480035a-b8f2-11e4-8dd3-dfdad4a01c42.jpg)

4.	Created new storage in the [Azure Console](https://manage.windowsazure.com) (any name). It's possible the Brisk guys will optionally automate this step as part of provisioning.

  ![pic3](https://cloud.githubusercontent.com/assets/7204669/6285351/a8257724-b8f2-11e4-9955-ceb19c53b7b4.jpg)

5.	Created a new MBrace cluster in [Brisk console](https://www.briskengine.com/#/dash) (any name), specifying the right data centre, and the appropriate service bus and storage.

  ![pic4](https://cloud.githubusercontent.com/assets/7204669/6285354/b0620876-b8f2-11e4-84c9-58e7acee52ab.jpg)

  ![pic4b](https://cloud.githubusercontent.com/assets/7204669/6285356/b53f71c6-b8f2-11e4-964a-c3b89d17cf3e.png)

  ![pic4c](https://cloud.githubusercontent.com/assets/7204669/6285357/b55bcf4c-b8f2-11e4-905c-b782ae7b9c6a.png)


6.	Creating the cluster will have created a "Cloud Service" in your Azure account.  Go back to the Azure console and get the connection strings from the portal for the created Cloud Service under "configure" tab.  The Brisk guys are working on automating this step.

  ![pic5](https://cloud.githubusercontent.com/assets/7204669/6285362/c6b7d04c-b8f2-11e4-8527-25b37e466e81.png)

7. Open Visual Studio, reset F# Interactive, enter the connection strings into the starter script:

    ```fsharp
    let config = 
        { Configuration.Default with
            StorageConnectionString = "DefaultEndpointsProtocol=  copy the rest here"
            ServiceBusConnectionString = "Endpoint=sb://mbr  copy the rest here" }
    ```

8. Called runtime.GetHandle(config):
    ```fsharp
    let runtime = Runtime.GetHandle(config)
    ```
   giving
   ```
   Binding session to 'C:\Users\dsyme\Documents\MBraceOnBriskStarter\MBraceOnBriskStarter\src\Demos\../../lib/Microsoft.Data.Edm.dll'...
   Binding session to 'C:\Users\dsyme\Documents\MBraceOnBriskStarter\MBraceOnBriskStarter\src\Demos\../../lib/Microsoft.Data.Services.Client.dll'...
   Binding session to 'C:\Users\dsyme\Documents\MBraceOnBriskStarter\MBraceOnBriskStarter\src\Demos\../../lib/Microsoft.Data.OData.dll'...
   Binding session to 'C:\Users\dsyme\Documents\MBraceOnBriskStarter\MBraceOnBriskStarter\src\Demos\../../lib/Newtonsoft.Json.dll'...

   val runtime : Runtime
    ```

9. Called runtime.ShowWorkers():
    ```
    Workers                                                                                                        

    Id                     Hostname        % CPU / Cores  % Memory / Total(MB)  Network(ul/dl : kbps)  Tasks  Process Id  Initialization Time         Heartbeat                  
    --                     --------        -------------  --------------------  ---------------------  -----  ----------  -------------------         ---------                  
    MBraceWorkerRole_IN_0  RD0003FF5507E5    1.78 / 2       22.08 / 3583.00         38.26 / 19.33      0 / 2        3316  20/02/2015 10:40:13 +00:00  20/02/2015 10:59:10 +00:00 
    MBraceWorkerRole_IN_2  RD0003FF550024    2.11 / 2       22.10 / 3583.00         45.91 / 23.32      0 / 2        3204  20/02/2015 10:40:15 +00:00  20/02/2015 10:59:09 +00:00 
    MBraceWorkerRole_IN_1  RD0003FF552704    2.43 / 2       22.19 / 3583.00         38.02 / 21.79      0 / 2         268  20/02/2015 10:40:18 +00:00  20/02/2015 10:59:10 +00:00 
    ```
    Called runtime.ShowProcesses():
    ```
    Processes                                                                                                   

    Name  Process Id  State  Completed  Execution Time  Tasks  Result Type  Start Time  Completion Time 
    ----  ----------  -----  ---------  --------------  -----  -----------  ----------  --------------- 

    Tasks : Active / Faulted / Completed / Total
    ```

10. Attach logger:
    ```fsharp
    runtime.ClientLogger.Attach(Common.ConsoleLogger())
    ```

11.	Create a cloud computation:
    ```fsharp
    let getThread() = Thread.CurrentThread.ManagedThreadId

    let work0 =
        cloud { return sprintf "run in the cloud on '%s' thread '%d'" Environment.MachineName (getThread()) }
        |> runtime.CreateProcess
    ```
   giving:
    ```
    20022015 11:00:30.715 +00:00 : Creating process 6c31512670884c9eaa5655dc53e5cde1 
    20022015 11:00:30.719 +00:00 : Uploading dependencies
    20022015 11:00:30.726 +00:00 : FSI-ASSEMBLY_4f4cf9fc-e870-4681-9b03-bea847eff8c0_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
    20022015 11:00:31.611 +00:00 : Creating DistributedCancellationToken
    20022015 11:00:31.846 +00:00 : Starting process 6c31512670884c9eaa5655dc53e5cde1
    20022015 11:00:33.547 +00:00 : Created process 6c31512670884c9eaa5655dc53e5cde1

    val work0 : Process<string>
    ```

12. Check status:
    ```
    Process                                                                                                                                                                            

    Name                        Process Id      State  Completed  Execution Time            Tasks          Result Type  Start Time                  Completion Time            
    ----                        ----------      -----  ---------  --------------            -----          -----------  ----------                  ---------------            
          6c31512670884c9eaa5655dc53e5cde1  Completed  True       00:00:00.8355016    0 /   0 /   1 /   1  string       20/02/2015 11:00:35 +00:00  02/20/2015 11:00:36 +00:00 

    Tasks : Active / Faulted / Completed / Total
    ```

13. Check result:
  ```fsharp
  work0.IsCompleted
  ```
  giving
  ```fsharp
  true
  ```
   and
    ```fsharp
    work0.AwaitResultAsync() |> Async.RunSynchronously

    val it : string = "run in the cloud on 'RD0003FF550024' thread '9'"
    ```
Awesome.
