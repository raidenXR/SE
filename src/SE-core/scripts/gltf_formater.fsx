open System
open System.Text
open System.IO

let peak (src:string) (i:int) (keyword:string) = 
    if i + keyword.Length > src.Length then false
    elif src[i..i + keyword.Length - 1] = keyword then true 
    else
        // printfn "from peak: %s" src[i..i + keyword.Length - 1] 
        false

let findNext (src:string) (i:int) (c:char) (idx:byref<int>) =
    let mutable n = i + 1
    let mutable b = false
    while n < src.Length && not b do
        if src[n] = c then 
            // printfn "%d" n
            idx <- n
            b <- true        
        n <- n + 1        
    b      


let write (indent:string) (pos:int) (src:string) (ms:StreamWriter) = 
    let mutable i = pos
    let mutable indent = indent
    while i < src.Length do
        match src[i] with
        | '[' | '{' -> 
            ms.Write src[i]
            ms.Write '\n'
            indent <- indent + "  "
            ms.Write indent
        | ']' | '}' ->
            ms.Write '\n'     
            indent <- indent[0..indent.Length - 3]
            ms.Write indent     
            ms.Write src[i]
            // if i + 1 < src.Length && src[i + 1] = ',' then
            //     ms.Write src[i + 1]
            //     i <- i + 1
            let next_i = if i + 1 < src.Length then i + 1 else i
            let next_c = src[next_i]
            if (next_c = ',') then 
                ms.Write ",\n"
                i <- i + 1
            else ms.Write '\n'       
            ms.Write indent     
        | ',' ->
            ms.Write src[i]
            ms.Write '\n'
            ms.Write indent
            // if i + i < src.Length && src[i + 1] = '"' then
            //     ms.Write src[i]
            //     ms.Write '\n'
            //     ms.Write indent      
        | 'g' ->
            if peak src i "generator" then
                let mutable c = -1
                match (findNext src i ',' &c) with
                | true -> 
                    // printfn "%s" (src[i..c - 1])
                    ms.Write src[i..c - 1]
                    i <- c - 1
                | false -> 
                    ms.Write src[i]         
        | _ -> 
            ms.Write src[i]
        i <- i + 1

let parse (path:string) =
    let src = File.ReadAllText path
    let sb = StringBuilder()
    use ms = File.CreateText (path[0..path.Length - 6] + "_edited" + path[path.Length - 5..])
    write "" 0 src ms
    ms.Close()
        

parse (Environment.GetCommandLineArgs()[2])
