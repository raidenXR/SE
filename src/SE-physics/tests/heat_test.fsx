#r "../bin/Debug/net10.0/SE-core.dll"

open System
open SE.Plotting

let Nx = 11
let Nt = 300
let Dx = 0.01
let Dt = 0.1
let KAPPA = 210.
let SPH = 900.
let RHO = 2700.

let mutable ix = 0
let mutable t = 0

let T = Array2D.zeroCreate<double> Nx 2
let mutable cons = 0.

let sb = System.Text.StringBuilder(1024*1024)

for ix in 0..Nx-2 do
    T[ix,0] <- 100.

T[0,0] <- 0.
T[0,1] <- 0.
T[Nx-1,0] <- 0.
T[Nx-1,1] <- 0.
cons <- KAPPA / (SPH*RHO) * Dt / (Dx*Dx)

for t in 1..Nt do
    for ix in 1..Nx-2 do
        T[ix,1] <- T[ix,0] + cons * (T[ix+1,0] + T[ix-1,0] - 2.*T[ix,0])

    if t % 10 = 0 || t = 1 then
        for ix in 0..Nx-1 do
            let posx = string ix
            let temp = string T[ix,1]
            let time = string t
            sb.AppendLine(posx + "  "+ temp + "  " + time) |> ignore
        sb.AppendLine("\n") |> ignore

    for ix in 1..Nx-2 do
        T[ix,0] <- T[ix,1]


Gnuplot()
|> Gnuplot.datablockString (string sb) "surface"
|>> "set samples 300"
|>> "splot $surface with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore


printfn "%s" (string sb)
Console.ReadKey()


