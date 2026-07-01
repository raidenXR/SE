#r "../bin/Debug/net10.0/SE-core.dll"

open System
open SE.Plotting

let D = 201
let u = Array3D.zeroCreate<double> (D+1) (D+1) 4
let sb = System.Text.StringBuilder(1024*1024)

let initial (u:double array3d) =
    let dx = 14. / 200.
    let dy = dx
    let dt = dx / sqrt(2.)    
    let dts  = (dt/dx) * (dt/dx)
    let mutable yy = -7.
    let time = 0.
    for i in 0..D-1 do
        let mutable xx = -7.
        for j in 0..D-1 do
            let tmp = 3. - sqrt(xx*xx + yy*yy)
            u[i,j,0] <- 4. * atan tmp
            xx <- xx + dx
        yy <- yy + dy


let solution (u:double array3d) nint =
    let dx = 14./200.
    let dy = dx
    let dt = dx / sqrt(2.)
    let mutable time = 0.
    time <- time + dt
    let dts = (dt/dx) * (dt/dx)
    let mutable tmp = 0.

    for m in 1..D-2 do
        for l in 1..D-2 do
            let a2 = u[m+1,1,0] + u[m-1,1,0] + u[m,l+1,0] + u[m,l-1,0]
            let tmp = 0.25*a2
            u[m,l,1] <- 0.5 * (dts*a2 - dt*dt*(sin tmp))

    for mm in 1..D-2 do
        u[mm,0,1] <- u[mm,1,1]
        u[mm,D-1,1] <- u[mm,D-2,1]
        u[0,mm,1] <- u[1,mm,1]
        u[D-1,mm,1] <- u[D-2,mm,1]

    u[0,0,1] <- u[1,0,1]
    u[D-1,0,1] <- u[D-2,0,1]
    u[0,D-1,1] <- u[1,D-1,1]
    u[D-1,D-1,1] <- u[D-2,D-1,1]
    tmp <- 0.
    
    for k in 0..nint do
        for m in 1..D-2 do
            for l in 1..D-2 do
                let a1 = u[m+1,l,1] + u[m-1,l,1] + u[m,l+1,1] + u[m,l-1,1]
                tmp <- 0.25 * a1
                u[m,l,2] <- -u[m,l,0] + dts*a1 - dt*dt*(sin tmp)
                u[m,0,2] <- u[m,1,2]
                u[m,D-1,2] <- u[m,D-2,2]

        for mm in 1..D-2 do
            u[mm,0,2] <- u[mm,1,2]
            u[mm,D-1,2] <- u[mm,D-2,2]
            u[0,mm,2] <- u[1,mm,2]
            u[D-1,mm,2] <- u[D-2,mm,2]

        u[0,0,2] <- u[1,0,2]
        u[D-1,0,2] <- u[D-2,0,2]
        u[0,D-1,2] <- u[1,D-1,2]
        u[D-1,D-1,2] <- u[D-2,D-1,2]
        for l in 0..D-1 do
            for m in 0..D-1 do
                u[l,m,0] <- u[l,m,1]
                u[l,m,1] <- u[l,m,2]

        if k = nint then
            for i in 0..5..D-1 do
                for j in 0..5..D-1 do
                    let posx = string i
                    let posy = string j
                    let uval = string ((sin u[i,j,2]) / 2.)
                    sb.AppendLine(posx + "  " + posy + "  " + uval) |> ignore
                sb.AppendLine("\n") |> ignore
        time <- time + dt


initial u 
solution u 9

Gnuplot()
|> Gnuplot.datablockString (string sb) "surface"
|>> "splot $surface with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()

                    
