#I "../../lib/"

#r "MBrace.Core.dll"
#r "MBrace.Library.dll"
#r "FsPickler.dll"
#r "Vagrant.dll"
#r "MBrace.Azure.Runtime.Common.dll"
#r "MBrace.Azure.Runtime.dll"
#r "MBrace.Azure.Client.dll"
#r "Streams.Core.dll"
#r "MBrace.Streams.dll"

let createStorageConnectionString(accountName, key) = sprintf "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s" accountName key
let createServiceBusConnectionString(serviceBusName, key) = sprintf "Endpoint=sb://%s.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=%s" serviceBusName key