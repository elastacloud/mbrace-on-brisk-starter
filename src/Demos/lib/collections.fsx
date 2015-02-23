namespace global

module Array = 
    let chunkBy (n:int) (numbers: 'T[])  = 
        [| for i in 1 .. numbers.Length / n  do 
            yield [| for j in ((i-1) * n) .. (i * n - 1) do 
                       yield numbers.[j] |] 
           if numbers.Length % n <> 0 then 
            yield [| for j in (numbers.Length / n) * n .. numbers.Length - 1 do 
                       yield numbers.[j] |] |] 

    let divideBy (n:int) (numbers: 'T[])  = chunkBy (numbers.Length / n) numbers

module List = 
    let chunkBy (n:int) (numbers: 'T list)  =  numbers |> List.toArray |> Array.chunkBy n |> Array.toList |> List.map Array.toList
    let divideBy (n:int) (numbers: 'T list)  =  numbers |> List.toArray |> Array.divideBy n |> Array.toList |> List.map Array.toList
