
= Octree

== Numerical methods Central-difference

=== 3D Laplace Equation on a Non-Uniform Grid
The Laplace equation in 3D is: \
$
  nabla^2 T = (partial^2 T)/(partial x^2) + (partial^2 T)/(partial y^2) + (partial^2 T)/(partial z^2)    
$

For a non-uniform grid, the discretized form (using central differences) is:
$
  (T_(i+1,j,k) - T_(i,j,k))/(Delta x_i) - (T_(i,j,k) - T_(i-1,j,k))/(Delta x_(i-1)) dot.c (2)/(Delta x_i + Delta x_(i-1)) +
  (T_(i,j+1,k) - T_(i,j,k))/(Delta y_j) - (T_(i,j,k) - T_(i,j-1,k))/(Delta y_(j-1)) dot.c (2)/(Delta y_j + Delta y_(j-1)) +  \
  (T_(i,j,k+1) - T_(i,j,k))/(Delta z_k) - (T_(i,j,k) - T_(i,j,k-1))/(Delta z_(k-1)) dot.c (2)/(Delta z_k + Delta z_(k-1)) = 0    
$

#pagebreak()


#page(
  paper: "a4",
  flipped: true,
  // orientation: "portrait",
  numbering: "1",
  header: align(right)[notes] + line(length: 100%))[        
```cs
// Solve using Gauss-Seidel iteration
public void Solve() {
    SetBoundaryConditions();
    double maxError = double.MaxValue;
    int iteration = 0;

    while (maxError > tolerance && iteration < maxIterations) {
        maxError = 0.0;
        for (int i = 1; i < x.Length - 1; i++) {
            for (int j = 1; j < y.Length - 1; j++) {
                for (int k = 1; k < z.Length - 1; k++) {
                    // Compute coefficients for non-uniform grid
                    double alpha_x = 2.0 / (dx[i - 1][j - 1][k - 1] * (dx[i - 1][j - 1][k - 1] + dx[i][j - 1][k - 1]));
                    double alpha_y = 2.0 / (dy[i - 1][j - 1][k - 1] * (dy[i - 1][j - 1][k - 1] + dy[i - 1][j][k - 1]));
                    double alpha_z = 2.0 / (dz[i - 1][j - 1][k - 1] * (dz[i - 1][j - 1][k - 1] + dz[i - 1][j - 1][k]));

                    // Gauss-Seidel update
                    double TNew = (alpha_x * (T[i + 1][j][k] * dx[i][j - 1][k - 1] + T[i - 1][j][k] * dx[i - 1][j - 1][k - 1]) +
                                  alpha_y * (T[i][j + 1][k] * dy[i - 1][j][k - 1] + T[i][j - 1][k] * dy[i - 1][j - 1][k - 1]) +
                                  alpha_z * (T[i][j][k + 1] * dz[i - 1][j - 1][k] + T[i][j][k - 1] * dz[i - 1][j - 1][k - 1])) /
                                 (alpha_x * (dx[i][j - 1][k - 1] + dx[i - 1][j - 1][k - 1]) +
                                  alpha_y * (dy[i - 1][j][k - 1] + dy[i - 1][j - 1][k - 1]) +
                                  alpha_z * (dz[i - 1][j - 1][k] + dz[i - 1][j - 1][k - 1]));

                    double error = Math.Abs(TNew - T[i][j][k]);
                    if (error > maxError) {
                        maxError = error;
                    }
                    T[i][j][k] = TNew;
                }
            }
        }
        iteration++;
    }
    // Console.WriteLine($"Converged in {iteration} iterations. Max error: {maxError:E2}");
}
```
]    


#pagebreak()

== Derivatives
```
        double h1 = x[i] - x[i - 1];
        double h2 = x[i + 1] - x[i];
        // Second-order central difference for second derivative on non-uniform grid
        return (2.0 / (h1 * (h1 + h2))) * f[i - 1] -
                (2.0 / (h1 * h2)) * f[i] +
                (2.0 / (h2 * (h1 + h2))) * f[i + 1];
```

```cs
using System;
using System.Collections.Generic;

public class CentralDifferenceNonUniform
{
    /// <summary>
    /// Computes the central difference approximation of the first derivative
    /// for a non-uniform grid using second-order accuracy.
    /// </summary>
    /// <param name="x">Array of non-uniform grid points</param>
    /// <param name="f">Function values at grid points</param>
    /// <param name="i">Index at which to compute the derivative</param>
    /// <returns>The first derivative at point i</returns>
    public static double ComputeFirstDerivative(double[] x, double[] f, int i)
    {
        if (x == null || f == null)
            throw new ArgumentNullException("Input arrays cannot be null");
        if (x.Length != f.Length)
            throw new ArgumentException("x and f arrays must have the same length");
        if (i <= 0 || i >= x.Length - 1)
            throw new ArgumentOutOfRangeException("Index must be between 1 and length-2");

        double h1 = x[i] - x[i - 1];
        double h2 = x[i + 1] - x[i];

        // Second-order central difference for non-uniform grid
        return ((h2 / (h1 * (h1 + h2))) * f[i - 1]) +
               (((h2 - h1) / (h1 * h2)) * f[i]) -
               ((h1 / (h2 * (h1 + h2))) * f[i + 1]);
    }

    /// <summary>
    /// Computes the second derivative using central difference for non-uniform grid.
    /// </summary>
    /// <param name="x">Array of non-uniform grid points</param>
    /// <param name="f">Function values at grid points</param>
    /// <param name="i">Index at which to compute the derivative</param>
    /// <returns>The second derivative at point i</returns>
    public static double ComputeSecondDerivative(double[] x, double[] f, int i)
    {
        if (x == null || f == null)
            throw new ArgumentNullException("Input arrays cannot be null");
        if (x.Length != f.Length)
            throw new ArgumentException("x and f arrays must have the same length");
        if (i <= 0 || i >= x.Length - 1)
            throw new ArgumentOutOfRangeException("Index must be between 1 and length-2");

        double h1 = x[i] - x[i - 1];
        double h2 = x[i + 1] - x[i];

        // Second-order central difference for second derivative on non-uniform grid
        return (2.0 / (h1 * (h1 + h2))) * f[i - 1] -
               (2.0 / (h1 * h2)) * f[i] +
               (2.0 / (h2 * (h1 + h2))) * f[i + 1];
    }

    /// <summary>
    /// Computes all first derivatives for the entire grid.
    /// </summary>
    /// <param name="x">Array of non-uniform grid points</param>
    /// <param name="f">Function values at grid points</param>
    /// <returns>Array of first derivatives</returns>
    public static double[] ComputeFirstDerivatives(double[] x, double[] f)
    {
        if (x == null || f == null)
            throw new ArgumentNullException("Input arrays cannot be null");
        if (x.Length != f.Length)
            throw new ArgumentException("x and f arrays must have the same length");
        if (x.Length < 3)
            throw new ArgumentException("Array must have at least 3 points");

        double[] derivatives = new double[x.Length];

        // Compute derivatives for interior points
        for (int i = 1; i < x.Length - 1; i++)
        {
            derivatives[i] = ComputeFirstDerivative(x, f, i);
        }

        // Handle boundary points (using one-sided differences)
        derivatives[0] = (f[1] - f[0]) / (x[1] - x[0]); // Forward difference
        derivatives[x.Length - 1] = (f[x.Length - 1] - f[x.Length - 2]) /
                                   (x[x.Length - 1] - x[x.Length - 2]); // Backward difference

        return derivatives;
    }

    /// <summary>
    /// Computes all second derivatives for the entire grid.
    /// </summary>
    /// <param name="x">Array of non-uniform grid points</param>
    /// <param name="f">Function values at grid points</param>
    /// <returns>Array of second derivatives</returns>
    public static double[] ComputeSecondDerivatives(double[] x, double[] f)
    {
        if (x == null || f == null)
            throw new ArgumentNullException("Input arrays cannot be null");
        if (x.Length != f.Length)
            throw new ArgumentException("x and f arrays must have the same length");
        if (x.Length < 3)
            throw new ArgumentException("Array must have at least 3 points");

        double[] derivatives = new double[x.Length];

        // Compute derivatives for interior points
        for (int i = 1; i < x.Length - 1; i++)
        {
            derivatives[i] = ComputeSecondDerivative(x, f, i);
        }

        // Handle boundary points (using one-sided differences)
        derivatives[0] = 2.0 * ((f[2] - f[1]) / (x[2] - x[1]) -
                               (f[1] - f[0]) / (x[1] - x[0])) /
                       (x[2] - x[0]); // Second-order forward difference

        derivatives[x.Length - 1] = 2.0 * ((f[x.Length - 1] - f[x.Length - 2]) /
                                          (x[x.Length - 1] - x[x.Length - 2]) -
                                          (f[x.Length - 2] - f[x.Length - 3]) /
                                          (x[x.Length - 2] - x[x.Length - 3])) /
                                   (x[x.Length - 1] - x[x.Length - 3]); // Second-order backward difference

        return derivatives;
    }

    /// <summary>
    /// Example usage of the central difference methods.
    /// </summary>
    public static void ExampleUsage()
    {
        // Create a non-uniform grid
        double[] x = { 0.0, 0.1, 0.3, 0.6, 1.0, 1.5, 2.1 };
        double[] f = new double[x.Length];

        // Fill f with some function values (e.g., f(x) = x^2)
        for (int i = 0; i < x.Length; i++)
        {
            f[i] = x[i] * x[i];
        }

        // Compute first derivatives
        double[] firstDerivatives = ComputeFirstDerivatives(x, f);
        Console.WriteLine("First derivatives:");
        for (int i = 0; i < x.Length; i++)
        {
            Console.WriteLine($"x={x[i]:F2}, f'={firstDerivatives[i]:F6}");
        }

        // Compute second derivatives
        double[] secondDerivatives = ComputeSecondDerivatives(x, f);
        Console.WriteLine("\nSecond derivatives:");
        for (int i = 0; i < x.Length; i++)
        {
            Console.WriteLine($"x={x[i]:F2}, f''={secondDerivatives[i]:F6}");
        }

        // Verify against analytical derivatives (f(x) = x^2)
        // f'(x) = 2x, f''(x) = 2
        Console.WriteLine("\nVerification (should be close to 2x and 2):");
        for (int i = 0; i < x.Length; i++)
        {
            Console.WriteLine($"x={x[i]:F2}, analytical f'={2*x[i]:F6}, analytical f''=2.000000");
        }
    }
}

// Example usage
public class Program
{
    public static void Main()
    {
        CentralDifferenceNonUniform.ExampleUsage();
    }
}

```

== DU (Discriminated Union approach)
```
type Node<'T> =
    | Node of parent:Node<'T> * children:Node<'T>[] * idx:int * level:int * v_min:Vector3 * v_max:Vector3
    | Leaf of parent:Node<'T> * value:ref<ValueOption<'T>> * idx:int * level:int * v_min:Vector3 * v_max:Vector3 
    | Empty
```

=== Multi-threading in node traversal - tree ("for loop" iteration)
```
| Method            | Mean      | Error    | StdDev    | Median    |
|------------------ |----------:|---------:|----------:|----------:|
| Iter2             | 107.78 ms | 4.632 ms | 13.586 ms | 103.99 ms |
| Iter4             |  82.19 ms | 1.628 ms |  3.774 ms |  82.06 ms |
| IterExperimental2 | 315.35 ms | 8.388 ms | 24.200 ms | 312.33 ms |
| IterExperimental4 | 281.09 ms | 6.896 ms | 19.675 ms | 277.93 ms |
```
\*\* Running with 2 threads and 4 threads respectively


== Class (regular class approach)
```
[<System.Flags>]
type Flag =
    | Empty = 0uy
    | Node  = 1uy
    | Leaf  = 2uy

[<AllowNullLiteral>]
type Node<'T>() =
    [<DefaultValue>] val mutable flag:   Flag
    [<DefaultValue>] val mutable idx:    byte
    [<DefaultValue>] val mutable level:  byte
    [<DefaultValue>] val mutable v_min:  Vector3
    [<DefaultValue>] val mutable v_max:  Vector3

    [<DefaultValue>] val mutable value:  ValueOption<'T>

    [<DefaultValue>] val mutable parent: Node<'T> 
    let mutable children = InlineArray8<Node<'T>>()

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (index: int) =
            Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), index)

        and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (index: int) (value: Node<'T>) = 
            Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), index) <- value

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (index: byte) =
            Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), int index)

        and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (index: byte) (value: Node<'T>) = 
            Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), int index) <- value


let (|Node|Leaf|Empty|) (node:Node<'T>) =
    if node = null then Empty
    elif node.flag = Flag.Node then Node
    elif node.flag = Flag.Leaf then Leaf
    else Empty    
```


```
| Method            | Mean      | Error    | StdDev    | Median    |
|------------------ |----------:|---------:|----------:|----------:|
| Iter2             | 107.78 ms | 4.632 ms | 13.586 ms | 103.99 ms |
| Iter4             |  82.19 ms | 1.628 ms |  3.774 ms |  82.06 ms |
| IterExperimental2 | 315.35 ms | 8.388 ms | 24.200 ms | 312.33 ms |
| IterExperimental4 | 281.09 ms | 6.896 ms | 19.675 ms | 277.93 ms |
```

== SOA (struct of arrays approach)

*TODO:* this is not yet implemented... to experiment with this alternative
in the future. Since, it is mentioned in many performant sensitive applications.


