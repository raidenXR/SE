open System
open System.Text
open System.IO

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
            ms.Write '\n'       
            ms.Write indent     
        | ',' ->
            ms.Write src[i]
            ms.Write '\n'
            ms.Write indent
            // if i + i < src.Length && src[i + 1] = '"' then
            //     ms.Write src[i]
            //     ms.Write '\n'
            //     ms.Write indent                
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
