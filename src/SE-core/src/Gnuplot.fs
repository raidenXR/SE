namespace SE.Core
open System
open System.Text
open System.IO
open System.Diagnostics
open System.Runtime.InteropServices

module Plotting =

    type Gnuplot(keep_log: bool, keep_process_alive: bool, path: option<string>) = 
        let sb = StringBuilder(1024)        
        let total_input = StringBuilder(2024)
        let mutable keep_process_alive = keep_process_alive
        let mutable is_running = false
        let mutable process': Process = null

        do
            match path with 
            | Some p -> 
                sb.AppendLine ("set output '" + p + "'") |> ignore 
                keep_process_alive <- false
            | None ->
                keep_process_alive <- true

        
        /// close the process and dispose resources
        let pclose () =
            process'.StandardInput.Close()
            process'.WaitForExit()
            process'.Close()
            process'.Dispose()            
        
        new() = Gnuplot(false, true, None)
        new(path:string) = Gnuplot(false, false, Some path)
            

        member this.writeln (str: string) = 
            match is_running with 
            | true ->
                ignore (total_input.AppendLine(str))
                process'.StandardInput.WriteLine str
            | false ->
                ignore (sb.AppendLine(str))
                ignore (total_input.AppendLine(str))

        member this.IsRunning with get() = is_running

        member this.Run () =
            let input_str = string sb
            let pstr = if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then "gnuplot.exe"
                       elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then "gnuplot"
                       elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then "gnuplot"
                       else failwith "not supported OS"
            let info = ProcessStartInfo(FileName = pstr, UseShellExecute = false, RedirectStandardInput = true)
            process' <- new Process(StartInfo = info)
            process'.Start() |> ignore
            process'.StandardInput.WriteLine input_str       

            keep_process_alive <- if input_str.Contains("set output") then false else keep_process_alive
            if not keep_process_alive then                     
                pclose ()

            match keep_log, path with
                | true, Some p -> 
                    use fs = File.CreateText (p[0..p.Length - (4 + 1)] + ".gnu")
                    fs.WriteLine (input_str)
                | _, _ -> ()
            is_running <- true


        member this.Close () =
            // if keep_process_alive then 
            match keep_log, path with
                | true, Some p -> 
                    use fs = File.CreateText (p[0..p.Length - (4 + 1)] + ".gnu")
                    fs.WriteLine (string total_input)  // FIX this to include all the text written in stdin
                | true, None ->
                    use fs = File.CreateText "output.gnu"
                    fs.WriteLine (string total_input)
                | false, _ -> ()

            pclose ()




    let (|>>) (plt:Gnuplot) (str:string) = plt.writeln str; plt


    module Gnuplot =
        let run (gnu:Gnuplot) = gnu.Run(); gnu

        let close (gnu:Gnuplot) = gnu.Close()

        let datablockX (x:array<float>) (tag:string) (gnu:Gnuplot) = 
            gnu.writeln $"\n${tag} << EOD"
            let l = Array.length x
            for i in 0..l-1 do gnu.writeln $"{x[i]}"
            gnu.writeln "EOD\n"
            gnu

        let datablockN (data:array<array<float>>) (tag:string) (gnu:Gnuplot) =
            let _sb = StringBuilder(1000)
            let I = data.Length
            let J = data[0].Length
            ignore (_sb.AppendLine($"\n${tag} << EOD"))
            for j in 0..J - 1 do
                for i in 0..I - 1 do
                    ignore (_sb.Append($"{data[i][j]}  "))
                ignore (_sb.Append("\n"))
            gnu.writeln(_sb.AppendLine("EOD\n").ToString())        
            gnu


        let datablockXY (x:array<float>) (y:array<float>) (tag:string) (gnu:Gnuplot) =
            gnu.writeln $"\n${tag} << EOD"
            let l = Array.length x
            for i in 0..l-1 do gnu.writeln $"{x[i]}  {y[i]}"
            gnu.writeln "EOD\n"
            gnu
        

        let datablockXYZ (x:array<float>) (y:array<float>) (z:array<float>) (tag:string) (gnu:Gnuplot) =
            gnu.writeln $"\n${tag} << EOD"
            let l = Array.length x
            for i in 0..l-1 do gnu.writeln $"{x[i]}  {y[i]}  {z[i]}"
            gnu.writeln "EOD\n"
            gnu

            
        let datablockXYZW (x:array<float>) (y:array<float>) (z:array<float>) (w:array<float>) (tag:string) (gnu:Gnuplot) =
            gnu.writeln $"\n${tag} << EOD"
            let l = Array.length x
            for i in 0..l-1 do gnu.writeln $"{x[i]}  {y[i]}  {z[i]}  {w[i]}"
            gnu.writeln "EOD\n"
            gnu
