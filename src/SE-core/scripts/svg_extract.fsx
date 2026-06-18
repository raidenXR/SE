#r "nuget: SkiaSharp, 2.88.6"

open System
open System.IO
open SkiaSharp

let input = Environment.GetCommandLineArgs()[2]
let svg = File.ReadAllText(input)
let fs = File.CreateText(input[0..input.Length-5] + ".dat")
let elements = svg.Split([|'<';'>';'/'|])
let paths =
    elements
    |> Array.filter (fun x -> x.Length > 1)
    |> Array.filter (fun x -> x.Contains("path"))



let transform (s:string) =
    let a = s.IndexOf("translate")
    let b = s.IndexOf("fill")
    let t = s[a..b]
    let t_values = t[t.IndexOf('(')+1..t.IndexOf(')')-1].Split(", ")
    let t_x = float32 t_values[0]
    let t_y = float32 t_values[1]
    (t_x,t_y)
    
let curve (s:string) =
    let a = s.IndexOf("bevel")
    let b = s.IndexOf("sodi") 
    s[a+10..b-3]


for pth in paths do
    let path = "<" + pth + "/>"
    let sk_path = SKPath.ParseSvgPathData(curve path)
    if sk_path = null then
        printfn "%s" (curve path)        
        failwith "sk_path is null"
        
    let (tx,ty) = transform path
    let points = sk_path.Points
    for i in 0..points.Length-1 do
        let p = points[i]
        try
            let line = sprintf "%d,%g,%g" i (p.X + tx) (p.Y + ty)
            fs.WriteLine(line)       
        with
        | _ ->
            fs.Close()
            printfn "%s" (curve path)
            failwith "failed"
    fs.WriteLine("\n\n")       
            

    

fs.Close()

