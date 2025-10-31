open System

// accept as input the size of the grid when running the script on terminal
let args = System.Environment.GetCommandLineArgs()
let NEx = if args.Length >= 4 then Int32.Parse (args[2]) else 2   // number of elements on each row
let NEy = if args.Length >= 4 then Int32.Parse (args[3]) else 3   // number of elements on each column
let NE = NEx * NEy    // total number of elements in the grid
let NNx = NEx + 1
let NNy = NEy + 1    // number of nodes on each row
let NN = NNx * NNy   // number of adjacent nodes on each element

// practically use a jagged array (int[][]), to mimic a tree-data-structure
// the specific problem is more similar to traversing a tree, than indexing a matrix(/array)
// the following couple lines, just instanciates a int[][]
let elements = Array.init NE (fun x -> Array.zeroCreate<int> 4)

// the following block of code is computing the node indices of each element
// and caching the results into that jagged array, as a memoization technique.
let mutable n = 1
for i in 1..NE do
    elements[i - 1][0] <- n
    elements[i - 1][1] <- n + 1
    elements[i - 1][2] <- n + NNx
    elements[i - 1][3] <- n + NNx + 1
    n <- if i % NEx = 0 then n + 2 else n + 1
    printfn "NE(%d): %A" i elements[i - 1]

printfn "generate grid with NE: %d" NE

// all the pairs of (i,j) are cached. so accessing a node via a (i,j) pair
// is a matter of indexing the jagged array.
let NOP i j (nodes:array<array<int>>) =
    nodes[i - 1][j - 1]          // convert 1-based to 0-based

// print all pairs to validate the results
// run this block to evaluate that the algorithm is correct indeed.
// If redundant, comment-out.
// for a in 1..NE do         // iterate every element
//     for b in 1..4 do      // iterate every adjacent node to the element
//         let idx = NOP a b elements
//         printfn "(%d, %d) = %d " a b idx

// this block is receiving input (i,j) from the user while running.
// It is the requirement of the assignment.
// to exit the program, press CTRL + C on terminal.
// while true do
//     printfn "\ninsert pair of (i,j) as: i j"
//     let str = Console.ReadLine()
//     let i = str.Split(' ')[0] |> Int32.Parse
//     let j = str.Split(' ')[1] |> Int32.Parse
//     let idx = NOP i j elements
//     printfn "(%d, %d) = %d " i j idx
