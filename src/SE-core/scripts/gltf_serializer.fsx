open System.Text.Json
open System.Numerics

[<Struct>]
type Vec ={
    x: int
    y: int
    z: int
}

type A = {
    id: int
    name: string
    pos: Vec
}

let a = {id = 4; name = "name_str"; pos = {x = 1; y = 4; z = 6}}
let serialized_obj = JsonSerializer.Serialize(a)

printfn "%s" serialized_obj


let fs = System.IO.File.CreateText "serializer.json"
fs.WriteLine (serialized_obj)
fs.Close()


let deserialized_obj = JsonSerializer.Deserialize<A>(serialized_obj)

printfn "%A" deserialized_obj
