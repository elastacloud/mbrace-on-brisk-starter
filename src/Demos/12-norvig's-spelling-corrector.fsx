#load "credentials.fsx"
#load "lib/collections.fsx"

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Streams
open Nessos.Streams

(**
 This tutorial demonstrates a word count example via Norvig's Spelling Corrector (http://norvig.com/spell-correct.html)
 
 Before running, edit credentials.fsx to enter your connection strings.
**)

// First connect to the cluster
let cluster = Runtime.GetHandle(config)



/// helper : write all text to provided stream
let write (text: string) (stream: Stream) = async { 
    use writer = new StreamWriter(stream)
    writer.Write(text)
    return () 
}

/// Step 1: download text file from source, 
/// saving it to blob storage chunked into smaller files of 10000 lines each
let download (uri: string) = cloud {
    let webClient = new WebClient()
    do! Cloud.Log "Begin file download" 
    let! text = Cloud.OfAsync <| webClient.AsyncDownloadString(Uri(uri))
    do! Cloud.Log "file downloaded" 
    // Partition the big text into smaller files 
    let! files = 
        text.Split('\n')
        |> Array.chunkBy 10000
        |> Array.mapi (fun index strings -> CloudFile.New(write <| String.Concat(strings), sprintf "text/%d.txt" index))
        |> Cloud.Parallel
        |> Cloud.ToLocal
    return files
}

let proc = download "http://norvig.com/big.txt" |> cluster.CreateProcess

cluster.ShowProcesses()

let files = proc.AwaitResult()

// Step 2. Use MBrace.Streams to perform a parallel word 
// frequency count on the stored text
let regex = Regex("[a-zA-Z]+", RegexOptions.Compiled)
let proc' = 
    files
    |> CloudStream.ofCloudFiles CloudFile.ReadAllText
    |> CloudStream.collect (fun text -> regex.Matches(text) |> Seq.cast |> Stream.ofSeq)
    |> CloudStream.map (fun (m:Match) -> m.Value.ToLower()) 
    |> CloudStream.countBy id 
    |> CloudStream.toArray
    |> cluster.CreateProcess

cluster.ShowProcesses()

// Step 3. Use calculated frequency counts to compute
// suggested spelling corrections in the local process
let NWORDS = proc'.AwaitResult() |> Map.ofArray

let edits1 (word: string) = 
    let splits = [for i in 0 .. word.Length do yield (word.[0..i-1], word.[i..])]
    let deletes = [for a, b in splits do if b <> "" then yield a + b.[1..]]
    let transposes = [for a, b in splits do if b.Length > 1 then yield a + string b.[1] + string b.[0] + b.[2..]]
    let replaces = [for a, b in splits do for c in 'a'..'z' do if b <> "" then yield a + string c + b.[1..]]
    let inserts = [for a, b in splits do for c in 'a'..'z' do yield a + string c + b]
    deletes @ transposes @ replaces @ inserts |> Set.ofList

let (|KnownEdits2|_|) word = 
    let result = [for e1 in edits1(word) do for e2 in edits1(e1) do if Map.containsKey e2 NWORDS then yield e2] |> Set.ofList
    if not (Set.isEmpty result) then Some result else None

let (|KnownEdits1|_|) word = 
    let result = [for w in edits1(word) do if Map.containsKey w NWORDS then yield w] |> Set.ofList
    if not (Set.isEmpty result) then Some result else None

let (|Known|_|) word = 
    let result = [for w in [word] do if Map.containsKey w NWORDS then yield w] |> Set.ofList
    if not (Set.isEmpty result) then Some result else None

let correct (word: string) = 
    let words = 
        match word with
        | Known words -> words
        | KnownEdits1 words -> words
        | KnownEdits2 words -> words
        | _ -> Set.singleton word
    words |> Seq.sortBy (fun w -> -NWORDS.[w]) |> Seq.head

// Examples
correct "speling"
correct "korrecter"

