namespace SE.Serializers
open System
open System.Text
open System.IO
open System.Runtime.InteropServices
open System.Diagnostics


module Ascii = 
    type AsciiBuilder() =
        let sb = StringBuilder(1024)

        member x.writeln (s:string) = ignore (sb.AppendLine s); x

        member x.write_ignore (s:string) = ignore (sb.Append s)
        
        member x.writeln_ignore (s:string) = ignore (sb.AppendLine s)

        override _.ToString() = string sb


    let header (h:string) (ascii:AsciiBuilder) =
        let l = max 30 h.Length
        ascii.writeln_ignore ("".PadRight(l, '#'))
        ascii.writeln_ignore h
        ascii.writeln ("".PadRight(l, '#'))        

    let olist (items:seq<_>) (ascii: AsciiBuilder) = 
        items |> Seq.iteri (fun i x  -> ascii.writeln_ignore $"{i}. {string x}")
        ascii.writeln ""

    let ulist (items:seq<_>) (ascii: AsciiBuilder) =
        items |> Seq.iter (fun x -> ascii.writeln_ignore $"- {string x}")
        ascii.writeln ""

    let line (s:string) (ascii:AsciiBuilder) = ascii.writeln s        


    let table (h: seq<string>) (content: list<list<'T>>) (ascii:AsciiBuilder) =
        let t = if (Seq.length h) > 0 then content |> Seq.map (Seq.map (fun x -> string x)) |> List.ofSeq else []
        let ht = h::t |> Array.ofList |> Array.map (fun x -> Array.ofSeq x)
        let lens = ht |> Array.transpose |> Array.map (Array.max) |> Array.map (fun w -> w.Length + 3)

        let I = lens.Length - 1
        let J = ht.Length - 1
        let hl = Seq.length h
        let ndash = (Array.sum lens) + 2 + (hl * 2)    // padding of '| ' and '|'
        let padline () = ascii.writeln_ignore (String.Format("-").PadRight(ndash, '-'))

        padline ()
        ascii.write_ignore "| "
        for i in 0..I do 
            ascii.write_ignore (String.Format(ht[0][i]).PadRight(lens[i]))     
            ascii.write_ignore " |"
        ascii.writeln_ignore ""
        padline ()
            
        for j in 1..J do
            ascii.write_ignore "| "
            for i in 0..I do
                ascii.write_ignore (String.Format(ht[j][i]).PadRight(lens[i]))     
                ascii.write_ignore " |"
            ascii.writeln_ignore ""
        padline ()
        ascii
        // ascii.writeln "\n"


    let close path (ascii:AsciiBuilder) = 
        use fs = File.CreateText path
        fs.WriteLine (string ascii)
        fs.Close()


module Html =   
    type HtmlBuilder() = 
        let sb = StringBuilder(1024)
        do sb.AppendLine ("""
                <!DOCTYPE html>
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.2/dist/katex.min.css" integrity="sha384-bYdxxUwYipFNohQlHt0bjN/LCpueqWz13HufFEV1SUatKs1cm4L6fFgCi1jT643X" crossorigin="anonymous">
                <script defer src="https://cdn.jsdelivr.net/npm/katex@0.16.2/dist/katex.min.js" integrity="sha384-Qsn9KnoKISj6dI8g7p1HBlNpVx0I8p1SvlwOldgi3IorMle61nQy4zEahWYtljaz" crossorigin="anonymous"></script>
                <script defer src="https://cdn.jsdelivr.net/npm/katex@0.16.2/dist/contrib/auto-render.min.js" integrity="sha384-+VBxd3r6XgURycqtZ117nYw44OOcIax56Z4dCRWbxyPt0Koah1uHoK0o4+/RRE05" crossorigin="anonymous"></script>
                <script>
                    document.addEventListener("DOMContentLoaded", function() {
                        renderMathInElement(document.body, {
                        // customised options
                        // • auto-render specific keys, e.g.:
                        delimiters: [
                            {left: '$$', right: '$$', display: true},
                            {left: '$', right: '$', display: false},
                            {left: '\\(', right: '\\)', display: false},
                            {left: '\\[', right: '\\]', display: true}
                        ],
                        // • rendering keys, e.g.:
                        throwOnError : false
                        });
                    });
                </script>
                <head>
                    <style>
                    :root {
                        font-size: 1em;
                        --ui: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
                        --mono: "Source Code Pro", monospace;
                        --tx-color: #141414;
                        --bg-color: #ffffff;
                        --link-color: #2A6286;
                        --sidebar-sh-color: rgba(0, 0, 0, 0.09);
                        --sidebar-mod-bg-color: #f1f1f1;
                        --sidebar-modlnk-tx-color: #141414;
                        --sidebar-modlnk-tx-color-hover: #fff;
                        --sidebar-modlnk-tx-color-active: #000;
                        --sidebar-modlnk-bg-color: transparent;
                        --sidebar-modlnk-bg-color-hover: #555;
                        --sidebar-modlnk-bg-color-active: #FFBB4D;
                        --search-bg-color: #f3f3f3;
                        --search-bg-color-focus: #ffffff;
                        --search-sh-color: rgba(0, 0, 0, 0.18);
                        --search-other-results-color: rgb(100, 100, 100);
                        --modal-sh-color: rgba(0, 0, 0, 0.75);
                        --modal-bg-color: #aaa;
                        --warning-popover-bg-color: #ff4747;
                    }
                    code {
                        font-family: var(--mono);
                        font-weight: bold;
                        font-size: 1em;
                        <!-- background: whitesmoke;
                        color: #000;
                        padding: 10px;
                        line-height: 10px; -->       
                    }
                    body {
                        /* A4 page size (the content of the page will be centered)*/    
                        height: 297mm;
                        width: 210mm;
                        /* to centre page on screen*/
                        margin-left: auto;
                        margin-right: auto;
                    }
                    table{
                        border-collapse: collapse;
                    }
                    table :last-child >:last-child > td {
                    border-bottom: 0.55mm solid black;
                    }  
                    th, td {
                        padding-left: 2mm;
                        text-align: left;        
                    }
                    th{
                    border-top: 0.55mm solid black;
                    border-bottom: 0.55mm solid black;
                    }           
                    </style>
                </head>
                <html lang="en">
                    <body>
        """) |> ignore


        member x.writeln (s:string) = ignore (sb.AppendLine s); x

        member x.writeln_ignore (s:string) = ignore (sb.AppendLine s)

        override x.ToString() = string sb


    let header lv str (html:HtmlBuilder) =
        match lv with 
        | n when n <= 1 -> $"<h1>{str}</h1>"
        | 2 -> $"<h2>{str}</h2>"
        | 3 -> $"<h3>{str}</h3>"
        | 4 -> $"<h4>{str}</h4>"
        | n when n >= 5 -> $"<h5>{str}</h5>"
        | _ -> failwith "not implemented"
        |> html.writeln


    let image img (html:HtmlBuilder) =
        $"<img src=\"{img}\">" |> html.writeln


    let olist (items:seq<_>) (html:HtmlBuilder) = 
        html.writeln_ignore "<ol>"
        items |> Seq.iter (fun x -> html.writeln_ignore $"<li>{string x}</li>")
        html.writeln_ignore "</ol>"
        html 

    let olisti (dx:int) (items:seq<_>) (html:HtmlBuilder) = 
        html.writeln_ignore $"<ol start=\"{dx}\">"
        items |> Seq.iteri (fun i x -> html.writeln_ignore $"<li>{string x}</li>")
        html.writeln_ignore "</ol>"
        html 
            
    let ulist (items:seq<_>) (html:HtmlBuilder) = 
        html.writeln_ignore "<ul>"
        items |> Seq.iter (fun x -> html.writeln_ignore $"<li>{string x}</li>")
        html.writeln_ignore "</ul>"
        html 

    let latexfunctionln fn (html:HtmlBuilder) = html.writeln $"${fn}$"

    let latexfunction fn (html:HtmlBuilder) = html.writeln $"$${fn}$$"

    let line s (html:HtmlBuilder) = html.writeln $"{s} <br>"

    let code s (html:HtmlBuilder) = html.writeln $"<br><code>\n{s}\n</code><br>\n" 

    let table (caption: option<string>) (header: seq<string>) (footer: seq<string>) (content: list<list<'T>>) (html:HtmlBuilder) =
        html.writeln_ignore "\n<br>\n<table>"

        match caption with
        | Some s -> html.writeln_ignore $"  <caption>{s}</caption>"
        | None -> ()

        if (Seq.length header) > 0 then
            html.writeln_ignore "<thead>\n  <tr>"
            Seq.iter (fun hi -> html.writeln_ignore $"<th>{hi}</th>") header
            html.writeln_ignore "</tr>\n<thead>"

        if (Seq.length footer) > 0 then
            html.writeln_ignore "<tfoot>\n  <tr>"
            Seq.iter (fun fi -> html.writeln_ignore $"<th>{fi}</th>") footer

        html.writeln_ignore "<tbody>"
        for line in content do
            html.writeln_ignore "  <tr>"
            for item in line do html.writeln_ignore $"<td>{string item}</td>"
            html.writeln_ignore "  </tr>"

        html.writeln_ignore "</tbody>\n"
        html.writeln "</table>\n<br>"


    let close path (html:HtmlBuilder) =
        use fs = File.CreateText path
        fs.WriteLine (string html)
        fs.Close ()

        
    let openbrowser (url:string) =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then Process.Start(ProcessStartInfo("cmd", $"/c start {url}"))
            elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then Process.Start("xdg-open", url)
            elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then Process.Start("open", url)
            else failwith "not supported OS"
        |> ignore


