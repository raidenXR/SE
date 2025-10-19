#load "../src/gnuplot.fs"

open System
open SE.Plotting

let a = [| 1.0;2.0;3.0;4.0;6.0; 9.0; 10.0 |]
let b = [| 4.0;4.0;4.0;3.0;6.0; 9.0; 10.0 |]
let c = [| 1.0;6.0;9.0;1.0;6.0; 9.0; 10.0 |]
let d = [| 0.0;4.0;5.0;8.0;6.0; 9.0; 10.0 |]
let e = [| 4.0;1.0;9.0;3.0;7.0; 9.0; 10.0 |]
let data = [|a;b;c;d;e|] 

// let gnuplot = Gnuplot(true, true, None)
Gnuplot()
|>> "set title 'Title'"
|> Gnuplot.datablockN data "Input"
|>> "plot for [i = 1:5] $Input using i title 'line '.i w lines lw 2"
|> Gnuplot.run
// |> Gnuplot.close

Console.ReadKey()

