#r "../bin/Debug/net10.0/SE-core.dll"

open System
open SE.Plotting

let den = 390.0
let ten = 180.0

let u = Array3D.zeroCreate<double> 101 101 3
let incrx = Math.PI / 100.
let incry = Math.PI / 100.
let c = sqrt (ten/den)

let cprime = c
let covercp = c / cprime
let ratio = 0.5 * covercp * covercp

let mutable x = 0.
let mutable y = 0.

let sb = System.Text.StringBuilder(1024*1024)

for j in 0..100 do
    x <- 0.
    for i in 0..100 do
        u[i,j,0] <- sin (2.0*x) * sin(y)
        x <- x + incrx
    y <- y + incry

for j in 1..99 do
    for i in 1..99 do
        u[i,j,1] <- u[i,j,0] + 0.5*ratio*(u[i+1,j,0] + u[i-1,j,0] + u[i,j+1,0] + u[i,j-1,0] - 4.*u[i,j,0])

for k in 1..45 do
    for j in 1..99 do
        for i in 1..99 do
            u[i,j,2] <- 2.*u[i,j,1] - u[i,j,0] + ratio*(u[i+1,j,1] + u[i-1,j,1] + u[i,j+1,1] + u[i,j-1,1] - 4.*u[i,j,1])

    for j in 0..100 do
        for i in 0..100 do
            u[i,j,0] <- u[i,j,1]
            u[i,j,1] <- u[i,j,2]

    if k = 45 then
        for j in 0..2..100 do
            for  i in 0..2..100 do
                let posx = string j
                let posy = string i
                sb.AppendLine(posx + "  " + posy + "  " + string u[i,j,2]) |> ignore
            sb.AppendLine("\n") |> ignore


Gnuplot()
|> Gnuplot.datablockString (string sb) "surface"
|>> "splot $surface with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()

