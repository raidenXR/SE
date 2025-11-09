#nowarn "9"
#nowarn "51"
namespace SE.FEM
open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open SE.Numerics
open SE.Numerics.PtrOperations
open FSharp.NativeInterop


type FEM_GaussQuadratureOrder = | TwoPoints | ThreePoints

type [<Struct>] Vector3 = {x:float; y:float; z:float} with
    member v.Item 
        with inline get(i:int) = NativePtr.get (&&v.x) i
        and inline set(i:int) value = NativePtr.set (&&v.x) i value

// type [<Struct>] Node = {
//     xyz_init: nativeptr<Vector3>
//     xyz: nativeptr<Vector3>
//     global_index: nativeptr<int64>
// }

type [<Struct>] Node = {
    xyz_init: nativeptr<float>
    xyz: nativeptr<float>
    global_index: nativeptr<int64>
}

module Vector3 =
    let sub (a:Vector3) (b:Vector3) =
        {x = a[0] - b[0]; y = a[1] - b[1]; z = a[2] - b[2]}
        
    let mult (a:Vector3) (b:Vector3) =
        let x = a[1] * b[2] - a[2] * b[1]
        let y = a[2] * b[0] - a[0] * b[2]
        let z = a[0] * b[1] - a[1] * b[0]
        {x = x; y = y; z = z}

    let mult_scalar (s:float) (v:Vector3) =
        {x = s * v[0]; y = s * v[1]; z = s * v[2]}

    let mult_matrix (m:Matrix) (v:Vector3) =
        let x = m[1,1] * v[0] + m[1,2] * v[1] + m[1,3] * v[2]
        let y = m[2,1] * v[0] + m[2,2] * v[1] + m[2,3] * v[2]
        let z = m[3,1] * v[0] + m[3,2] * v[1] + m[3,3] * v[2]
        {x = x; y = y; z = z}

    let mult_norm (a:Vector3) (b:Vector3) =
        let data = {
            x = a[1] + b[2] - a[2] * b[1]
            y = a[2] * b[0] - a[0] * b[2]
            z = a[0] * b[1] - a[1] * b[0]
        }
        let norm = sqrt(data[0] * data[0] + data[1] * data[1] + data[2] * data[2])
        mult_scalar (1.0 / norm) data

    
type GaussQuadraturePoints(order:FEM_GaussQuadratureOrder) =
    let n = match order with | TwoPoints -> 2 * 2 * 2 | ThreePoints -> 3 * 3 * 3
    let p = Array.zeroCreate<float> (n * 3)   // use OpenGL AttribPtr style
    let w = Array.zeroCreate<float> n

    do
        match order with
        | TwoPoints ->
            use point = new Vector([|-1. / sqrt(3.); 1. / sqrt(3.)|])
            let mutable I = 0            
            for i in 1..2 do
                for j in 1..2 do
                    for k in 1..2 do
                        I <- 4 * (i - 1) + 2 * (j - 1) + k - 1  // range 0..7
                        p[I * 3 + 0] <- point[i]
                        p[I * 3 + 1] <- point[j]
                        p[I * 3 + 2] <- point[k]
                        w[I] <- 1.0 
        | ThreePoints ->
            use point = new Vector([|-sqrt(0.6); 0.0; sqrt(0.6)|])
            use weight = new Vector([|5. / 9.; 8. / 9.; 5. / 9.|])
            let mutable I = 0
            for i in 1..3 do
                for j in 1..3 do
                    for k in 1..3 do
                        I <- 9 * (i - 1) + 3 * (j - 1) + k - 1   // range 0..26                        
                        p[I * 3 + 0] <- point[i]
                        p[I * 3 + 1] <- point[j]
                        p[I * 3 + 2] <- point[k]
                        w[I] <- weight[i] * weight[j] * weight[k]


    member this.N = n
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>] member this.weights(i:int) = w[i]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>] member this.points(i:int) = {x = p[(i - 1) * 3 + 0]; y = p[(i - 1) * 3 + 1]; z = p[(i - 1) * 3 + 2]}


type Parameters() =
    let _E = Math.Pow(10.0, 7.0)
    let _poisson = 0.25
    let _C = new Matrix(6,6)
    let _ni= 50
    let _nj = 2
    let _nk = 2
    let _l = 0.11
    let _beam_width = 0.00275
    let _p_up = 50.0
    do
        let CO = _E / (1.0 + _poisson) / (1.0 - 2.0 * _poisson)
        for i in 1..3 do
            _C[i,i] <- CO * (1.0 - _poisson)
            _C[i + 3, i + 3] <- CO * (0.5 - _poisson)
        _C.SetSymmetric(1,2, CO * _poisson)
        _C.SetSymmetric(1,3, CO * _poisson)
        _C.SetSymmetric(2,3, CO * _poisson)

    member val ni = _ni with get,set
    member val nj = _nj with get,set
    member val nk = _nk with get,set
    member val l = _l with get,set
    member val beam_width = _beam_width with get,set
    member val length_i = _l / float (_ni) with get,set
    member val length_j = _beam_width / float (_nj) with get,set
    member val lenght_k = _beam_width / float (_nk) with get,set
    member val p_up = _p_up with get,set
    member val N_FEM_iterations = 10 with get,set  // Newtown-Raphson iterations
    member val s_opt = 0 with get,set
    member val convergence_epsilon = Math.Pow(10.0, -9.0) with get,set
    member val E = _E with get,set
    member val poisson = _poisson with get,set
    member val C: Matrix = _C with get
    member val llarge_deformations = true with get,set
    member val GQorder = TwoPoints with get,set
    
    

type NodeArray(par:Parameters) =
    let n = (par.ni + 1) * (par.nj + 1) * (par.nk + 1)  // use OpenGL AttribPtr style
    let xyz_init = NativeMemory.AllocZeroed(unativeint (n * 3 * sizeof<float>)) |> NativePtr.ofVoidPtr<float>
    let xyz = NativeMemory.AllocZeroed(unativeint (n * 3 * sizeof<float>)) |> NativePtr.ofVoidPtr<float>
    let global_index = NativeMemory.AllocZeroed(unativeint (n * 3 * sizeof<int64>)) |> NativePtr.ofVoidPtr<int64>
    let mutable _N = 0
    let mutable is_disposed = false

    let dispose () =
        if not is_disposed then
            NativeMemory.Free(~~xyz_init)
            NativeMemory.Free(~~xyz)
            NativeMemory.Free(~~global_index)
        is_disposed <- true
        
    interface IDisposable with
        member this.Dispose() = dispose ()

    member this.N with get() = _N

    member this.Item
        with get(i:int, j:int, k:int) = 
            let idx = (i - 1) * 3 * par.nj + (j - 1) * 3 * par.nk + (k - 1) * 3
            {
                xyz_init = (xyz_init ++ idx)
                xyz = (xyz ++ idx)
                global_index = (global_index ++ idx)
            }
    
    member this.Dispose() = dispose ()
    
    member this.FillGlobalIndexes() =
        for i = 1 to par.ni do
            for j = 1 to par.nj do
                for k = 1 to par.nk do
                    let idx = (i - 1) * 3 * par.nj + (j - 1) * 3 * par.nk + (k - 1) * 3
                    // printfn "idx: %d, n: %d, ni: %d, nj: %d, dk: %d" idx n par.ni par.nj par.nk
                    _N <- _N + 1
                    global_index[idx + 0] <- _N
                    _N <- _N + 1
                    global_index[idx + 1] <- _N
                    _N <- _N + 1
                    global_index[idx + 2] <- _N
                        
    member this.SetInitialLocation(i:int, j:int, k:int, x:float, y:float, z:float) =
        let idx = (i - 1) * 3 * par.nj + (j - 1) * 3 * par.nk + (k - 1) * 3
        xyz_init[idx + 0] <- x
        xyz_init[idx + 1] <- y
        xyz_init[idx + 2] <- z
        xyz[idx + 0] <- x
        xyz[idx + 1] <- y
        xyz[idx + 2] <- z

    member this.SetZeroBoundaryCondition(i:int, j:int, k:int) =
        let idx = (i - 1) * 3 * par.nj + (j - 1) * 3 * par.nk + (k - 1) * 3
        global_index[idx + 0] <- -1
        global_index[idx + 1] <- -1
        global_index[idx + 2] <- -1


        
type Element(par:Parameters, nodearray:NodeArray, GQ:GaussQuadraturePoints, local_i:int, local_j:int, local_k:int) =
    let mutable first_method_call = false
    // for every Gauss point initialize one
    let q = new Vector(24)
    let dq = new Vector(24)
    let detJ = Array.zeroCreate<float> (GQ.N) 
    let DaDX123 = Array2D.zeroCreate<Vector3> GQ.N 8 
    let s = Array.zeroCreate<Vector> (GQ.N)
    let BL = Array.zeroCreate<Matrix> (GQ.N)

    let get_node i_from_1_to_8 =
        match i_from_1_to_8 with
        | 1 -> nodearray[local_i, local_j, local_k]
        | 2 -> nodearray[local_i + 1, local_j, local_k]
        | 3 -> nodearray[local_i + 1, local_j + 1, local_k]
        | 4 -> nodearray[local_i, local_j + 1, local_k]
        | 5 -> nodearray[local_i, local_j, local_k + 1]
        | 6 -> nodearray[local_i + 1, local_j, local_k + 1]
        | 7 -> nodearray[local_i, local_j + 1, local_k + 1]
        | _ -> nodearray[local_i, local_j, local_k + 1]

    let get_initial_position i_from_1_to_8 xyz_123 =
        (get_node i_from_1_to_8).xyz_init[xyz_123 - 1]
        
    let get_global_index i_from_1_to_24 =
        let xyz_123 = (i_from_1_to_24 - 1) / 8 + 1  // 1..3
        let i_from_1_to_8 = i_from_1_to_24 - (xyz_123 - 1) * 8  // 1..8
        (get_node i_from_1_to_8).global_index[xyz_123 - 1]
    
    let get_deformed_point i_from_1_to_8 =
        let node = get_node i_from_1_to_8
        let x = node.xyz_init[0] + q[i_from_1_to_8]
        let y = node.xyz_init[1] + q[i_from_1_to_8 + 8]
        let z = node.xyz_init[2] + q[i_from_1_to_8 + 16]
        {x = x; y = y; z = z}        
    
    let get_DaDr i_from_1_to_8 (rst:Vector3) =
        match i_from_1_to_8 with
        | 1 -> -(1. - rst[2]) * (1. - rst[3]) / 8.
        | 2 -> +(1. - rst[2]) * (1. - rst[3]) / 8.
        | 3 -> +(1. + rst[2]) * (1. - rst[3]) / 8.
        | 4 -> -(1. + rst[2]) * (1. - rst[3]) / 8.
        | 5 -> -(1. - rst[2]) * (1. + rst[3]) / 8.
        | 6 -> +(1. - rst[2]) * (1. + rst[3]) / 8.
        | 7 -> +(1. + rst[2]) * (1. + rst[3]) / 8.
        | 8 -> -(1. + rst[2]) * (1. + rst[3]) / 8.
        | _ -> failwith "wrong value"

    let get_DaDs i_from_1_to_8 (rst:Vector3) =
        match i_from_1_to_8 with
        | 1 -> -(1. - rst[1]) * (1. - rst[3]) / 8.
        | 2 -> -(1. + rst[1]) * (1. - rst[3]) / 8.
        | 3 -> +(1. + rst[1]) * (1. - rst[3]) / 8.
        | 4 -> +(1. - rst[1]) * (1. - rst[3]) / 8.
        | 5 -> -(1. - rst[1]) * (1. + rst[3]) / 8.
        | 6 -> -(1. + rst[1]) * (1. + rst[3]) / 8.
        | 7 -> +(1. + rst[1]) * (1. + rst[3]) / 8.
        | 8 -> +(1. - rst[1]) * (1. + rst[3]) / 8.
        | _ -> failwith "wrong value"

    let get_DaDt i_from_1_to_8 (rst:Vector3) =
        match i_from_1_to_8 with
        | 1 -> -(1. - rst[1]) * (1. - rst[2]) / 8.
        | 2 -> -(1. + rst[1]) * (1. - rst[2]) / 8.
        | 3 -> -(1. + rst[1]) * (1. + rst[2]) / 8.
        | 4 -> -(1. - rst[1]) * (1. + rst[2]) / 8.
        | 5 -> +(1. - rst[1]) * (1. - rst[2]) / 8.
        | 6 -> +(1. + rst[1]) * (1. - rst[2]) / 8.
        | 7 -> +(1. + rst[1]) * (1. + rst[2]) / 8.
        | 8 -> +(1. - rst[1]) * (1. + rst[2]) / 8.
        | _ -> failwith "wrong value"

    do
        for k = 0 to GQ.N - 1 do
            BL[k] <- new Matrix(6,24)
            for i = 1 to 8 do
                DaDX123[k, i - 1] <- {x = 0.; y = 0.; z = 0.}
            s[k] <- new Vector(6)
        first_method_call <- true
        

    member this.Dq = q

    member this.GetGlobalIndex(i_from_1_to_24:int) = get_global_index i_from_1_to_24

    member this.GetDeformedPoint(i_from_1_to_8:int) = get_deformed_point i_from_1_to_8

    member this.Initialize() =
        use J_inv = new Matrix(3, 3)
        for k = 1 to GQ.N do
            J_inv.Clear() 
            // 1.0 J_inv a detJ
            for i = 1 to 8 do
                let p = GQ.points(k)
                J_inv[1,1] <- (get_DaDr i p) * (get_initial_position i 1)
                J_inv[2,1] <- (get_DaDs i p) * (get_initial_position i 1)
                J_inv[3,1] <- (get_DaDt i p) * (get_initial_position i 1)
                J_inv[1,2] <- (get_DaDr i p) * (get_initial_position i 2)
                J_inv[2,2] <- (get_DaDs i p) * (get_initial_position i 2)
                J_inv[3,2] <- (get_DaDt i p) * (get_initial_position i 2)
                J_inv[1,3] <- (get_DaDr i p) * (get_initial_position i 3)
                J_inv[2,3] <- (get_DaDs i p) * (get_initial_position i 3)
                J_inv[3,3] <- (get_DaDt i p) * (get_initial_position i 3)
            detJ[k - 1] <- Matrix.determinant J_inv
            printfn "determinant: %g\n%s" (Matrix.determinant J_inv) (string J_inv)
            use J_inv_t = Matrix.inverse J_inv
            // 2.0 DaDxyz[i] (lokalni)
            for i = 1 to 8 do
                let p = GQ.points(k)
                let DaDrst = {x = get_DaDr i p; y = get_DaDs i p; z = get_DaDt i p}
                DaDX123[k - 1, i - 1] <- Vector3.mult_matrix J_inv_t DaDrst
        first_method_call <- true
    
    member this.Do(linearsolver: Matrix * Vector * Vector) =
        use Klocal = new Matrix(24,24)
        use flocal = new Vector(24)
        use Z = new Matrix(3,3)
        let mutable _val, integration_coefficient = 0.,0.

        // 1.0 Update q
        if (not first_method_call) then q += dq

        // Update r
        if local_k = par.nk - 1 then
            let u1 = Vector3.sub (get_deformed_point 6) (get_deformed_point 5)
            let u2 = Vector3.sub (get_deformed_point 8) (get_deformed_point 5)
            let n = Vector3.mult_scalar (-0.25 * par.p_up) (Vector3.mult u1 u2)
            for K = 0 to 2 do
                // left points with respect to i
                flocal[5 + 8 * K] <- n[K]
                flocal[8 + 8 * K] <- n[K]
                // right points with respect to i
                flocal[6 + 8 * K] <- n[K]
                flocal[7 + 8 * K] <- n[K]
        for k = 1 to GQ.N do
            // 3.0 Update s
            if (not first_method_call) then
                s[k - 1] <- s[k - 1] + par.C * BL[k - 1] * dq

            // 4.0 Compute Z, update BL
            if par.llarge_deformations then
                for i = 1 to 3 do
                    for j = 1 to 3 do
                        let mutable _val = 0.0
                        for s = 1 to 8 do
                            _val <- _val + DaDX123[k - 1, s - 1][j] * q[s + 8 * (i - 1)]
                        Z[i,j] <- _val

            BL[k - 1].Clear()
            for s = 1 to 8 do
                let Da = DaDX123[k - 1, s - 1]
                BL[k - 1][1,s] <- (1.0 + Z[1,1]) * Da[1]
                BL[k - 1][1, 8 + s] <- (Z[2,1]) * Da[1]
                BL[k - 1][1,16 + s] <- (Z[3,1]) * Da[1]
                BL[k - 1][2,s] <- (Z[1,2]) * Da[2]
                BL[k - 1][2, 8 + s] <- (1.0 + Z[2,2]) * Da[2]
                BL[k - 1][2,16 + s] <- (Z[3,2]) * Da[2]
                BL[k - 1][3,s] <- (Z[1,3]) * Da[3]
                BL[k - 1][3, 8 + s] <- (Z[2,3]) * Da[3]
                BL[k - 1][3,16 + s] <- (1.0 + Z[3,3]) * Da[3]
                BL[k - 1][4,s] <- (Z[1,2]) * Da[1] + (1.0 + Z[1,1] * Da[2])
                BL[k - 1][4, 8 + s] <- (1.0 + Z[2,2]) * Da[1] + (1.0 + Z[2,1] * Da[2])
                BL[k - 1][4,16 + s] <- (Z[3,2]) * Da[1] + (1.0 + Z[2,1] * Da[2])
                BL[k - 1][5, s] <- Z[1, 3] * Da[2] + Z[1, 2] * Da[3];
                BL[k - 1][5, 8 + s] <- Z[2, 3] * Da[2] + (1.0 + Z[2, 2]) * Da[3];
                BL[k - 1][5, 16 + s] <- (1.0 + Z[3, 3]) * Da[2] + Z[3, 2] * Da[3];
                BL[k - 1][6, s] <- Z[1, 3] * Da[1] + (1.0 + Z[1, 1]) * Da[3];
                BL[k - 1][6, 8 + s] <- Z[2, 3] * Da[1] + Z[2, 1] * Da[3];
                BL[k - 1][6, 16 + s] <- (1.0 + Z[3, 3]) * Da[1] + Z[3, 1] * Da[3];
                
            // 5.0 Add all to Klocal, flocal
            //5.1 (BN^T)S(BN)
            integration_coefficient <- GQ.weights(k) * detJ[k - 1];
            if (not first_method_call) then
                if par.llarge_deformations then
                    for i = 1 to 8 do            
                        for j = 1 to 8 do
                            _val <- s[k - 1][1] * DaDX123[k - 1, i - 1][1] * DaDX123[k - 1, j - 1][1] +
                                        s[k - 1][2] * DaDX123[k - 1, i - 1][2] * DaDX123[k - 1, j - 1][2] +
                                        s[k - 1][3] * DaDX123[k - 1, i - 1][3] * DaDX123[k - 1, j - 1][3] +
                                        s[k - 1][4] * (DaDX123[k - 1, i - 1][1] * DaDX123[k - 1, j - 1][2] +
                                        DaDX123[k - 1, i - 1][2] * DaDX123[k - 1, j - 1][1]) +
                                        s[k - 1][5] * (DaDX123[k - 1, i - 1][2] * DaDX123[k - 1, j - 1][3] +
                                        DaDX123[k - 1, i - 1][3] * DaDX123[k - 1, j - 1][2]) +
                                        s[k - 1][6] * (DaDX123[k - 1, i - 1][3] * DaDX123[k - 1, j - 1][1] +
                                        DaDX123[k - 1, i - 1][1] * DaDX123[k - 1, j - 1][3])
                            Klocal[i, j] <- _val
                            Klocal[i + 8, j + 8] <- _val
                            Klocal[i + 16, j + 16] <- _val
                flocal += ((BL[k - 1] % s[k - 1]) * -integration_coefficient)
            Klocal += (integration_coefficient * ((BL[k - 1] % par.C) * BL[k - 1]))
                            
        //6.0 Add to solver
        let (solution_A,_,_) = linearsolver
        for r = 1 to 24 do            
            let index1 = int32 (get_global_index r)
            if index1 > -1 then 
                _val <- flocal[r];
                if _val <> 0.0 then
                    solution_A[index1, 0] <- _val
                //Klocal
                for s = 1 to 24 do
                    let index2 = int32 (get_global_index s)
                    if index2 > -1 then
                        _val <- Klocal[r, s]
                        if _val <> 0.0 then
                            solution_A[index1, index2] <- _val
        first_method_call <- false;                                
                    
                    
type ElementArray(par:Parameters, nodearray:NodeArray) =
    let GQ = GaussQuadraturePoints(par.GQorder)
    let elems = Array3D.zeroCreate<Element> par.ni par.nj par.nk

    let get_local_node_number i_left_point j_left_point k_left_point =
        if i_left_point then
            if j_left_point then
                if k_left_point then 1 else 5
            else
                if k_left_point then 4 else 8
        else
            if j_left_point then
                if k_left_point then 2 else 6
            else
                if k_left_point then 3 else 7

    let get_deformed_point i_0_ni j_0_nj k_0_nk =
        let mutable i_left_point, j_left_point, k_left_point = false,false,false
        let mutable i_elem, j_elem, k_elem = -1,-1,-1
        let mutable local_index = 1
        if i_0_ni = 0 then
            i_left_point <- true
            i_elem <- i_0_ni
        else
            i_left_point <- false
            i_elem <- i_0_ni - 1
        if j_0_nj = 0 then
            j_left_point <- true
            j_elem <- 0
        else
            j_left_point <- false
            j_elem <- j_0_nj - 1
        if k_0_nk = 0 then
            k_left_point <- true
            k_elem <- k_0_nk
        else
            k_left_point <- false
            k_elem <- k_0_nk - 1
        // 2.0 local node number
        local_index <- get_local_node_number i_left_point j_left_point k_left_point
        elems[i_elem, j_elem, k_elem].GetDeformedPoint(local_index)

    do
        for i = 0 to par.ni - 1 do
            for j = 0 to par.nj - 1 do
                for k = 0 to par.nk - 1 do
                    elems[i,j,k] <- Element(par, nodearray, GQ, i, j, k)

    member this.Elems with get() = elems

    member this.InitializeAllElements() =
        for i = 0 to par.ni - 1 do
            for j = 0 to par.nj - 1 do
                for k = 0 to par.nk - 1 do
                    elems[i,j,k].Initialize()


    member this.DoAllElements(linearsolver:Matrix * Vector * Vector) =
        for i = 0 to par.ni - 1 do
            for j = 0 to par.nj - 1 do
                for k = 0 to par.nk - 1 do
                    elems[i,j,k].Do(linearsolver)        
            
    member this.InsertDataToNodeArray(nodearray:NodeArray) =
        for i = 0 to par.ni - 1 do
            for j = 0 to par.nj - 1 do
                for k = 0 to par.nk - 1 do
                    let _point = get_deformed_point i j k 
                    nodearray[i,j,k].xyz[0] <- _point.x
                    nodearray[i,j,k].xyz[1] <- _point.y
                    nodearray[i,j,k].xyz[2] <- _point.z
            
            
type Container() =
    let par = Parameters()
    let nodearray = new NodeArray(par)
    let elementarray = new ElementArray(par, nodearray)
    let mutable linearsolver: Matrix * Vector * Vector = (Matrix.empty, Vector.empty, Vector.empty)
    let mutable is_disposed = false

    let dispose () =
        if not is_disposed then
            nodearray.Dispose()
        is_disposed <- true

    let get_convergence_criterion () =
        let element = elementarray.Elems[par.ni / 2, 0, 0]
        let TP = element.GetDeformedPoint(3)
        abs (TP[2])
        
    let solver_result_to_elementarray () =
        let (_,_,solution_b) = linearsolver
        for i = 0 to par.ni - 1 do
            for j = 0 to par.nj - 1 do
                for k = 0 to par.nk - 1 do
                    let local_element = elementarray.Elems[i,j,k]
                    for r = 1 to 24 do
                        let index = int32 (local_element.GetGlobalIndex(r))
                        if index > -1 then
                            local_element.Dq[r] <- solution_b[index]
        

    interface IDisposable with
        member this.Dispose() = dispose ()

    member this.Initialize() =
        nodearray.FillGlobalIndexes()
        linearsolver <- (new Matrix(nodearray.N, nodearray.N), new Vector(nodearray.N), new Vector(nodearray.N))
        elementarray.InitializeAllElements()

    member this.Dispose() = dispose ()

    member this.SetInitialLocation(i:int, j:int, k:int, x:float, y:float, z:float) =
        nodearray.SetInitialLocation(i, j, k, x, y, z)

    member this.SetZeroBoundaryCondition(i:int, j:int, k:int) =
        nodearray.SetZeroBoundaryCondition(i, j, k)

    abstract member Solve_FEM: unit -> unit

    default this.Solve_FEM() =
        let mutable (A, x, b) = linearsolver        
        let mutable solution_x = new Vector([||])   // empty vector
        let mutable converged = false
        let mutable k = 0
        while k < par.N_FEM_iterations && not converged do
            A.Clear()
            elementarray.DoAllElements(linearsolver)
            x <- Solvers.GaussElimination A b            
            solver_result_to_elementarray ()

            let deltaqmax = Vector.Linf b
            let max_decay = get_convergence_criterion()
            if abs (deltaqmax) < par.convergence_epsilon && k > 1 then
                converged <- true
            k <- k + 1
        elementarray.InsertDataToNodeArray(nodearray)
            

    /// prints to very simple text format, readable
    member this.GetActualSolution(fs:System.IO.StreamWriter) =
        for i = 0 to par.ni do
            for j = 0 to par.nj do
                for k = 1 to par.nk do
                    let r = !!(cast<Vector3> ~~nodearray[i,j,k].xyz)
                    fs.WriteLine($"NOP({i},{j},{k}): {r.x}, {r.y}, {r.z} ")
            
    /// Prints to simple `1format appropriate for gnuplot
    member this.GetViewSolution(fs:System.IO.StreamWriter) =
        for i = 0 to par.ni do
            for j = 0 to par.nj do
                for k = 1 to par.nk do
                    let r = !!(cast<Vector3> ~~nodearray[i,j,k].xyz)
                    fs.WriteLine($"NOP({r.x}   {r.y}   {r.z} ")
