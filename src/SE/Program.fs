module SESample =
    open SE.Core
    open LibSErenderer

    // printfn "lib is found: %A" (System.IO.File.Exists libname)
    
    // ***********************************************************
    // COPY libSDL3.so to local directory for the libSE-rendere.so to find it. It is dynamically linked !!!    
    // ***********************************************************


    let [<Literal>] vertex_len = 100
    let positions = Array.zeroCreate<Position> vertex_len
    let colors = Array.zeroCreate<Color> vertex_len

    for i in 0..3.. vertex_len - 3 - 1 do
        let n = float32 i
        positions[i + 0] <- {X = n + 10f; Y = n + 10f / 2f; Z = n - 6f}
        positions[i + 1] <- {X = n - 10f; Y = n  - 10f / 2f; Z = 6f}
        positions[i + 2] <- {X = n + 10f; Y = n / 2f; Z = n + 6f}

        colors[i + 0] <- {V = 0xAA00BBFFu}
        colors[i + 1] <- {V = 0xAA00BBFFu}
        colors[i + 2] <- {V = 0xAA00BBFFu}

    init(16u, uint vertex_len, positions, colors)
    update()
    draw(uint vertex_len)
    quit()
    

