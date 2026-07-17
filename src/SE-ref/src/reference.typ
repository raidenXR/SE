
= Computational Physics

== Differentiation

=== Central Differences Algorithm with Non-Uniform Node Spacing
The central differences method can be extended to handle non-uniform node spacing,
though it requires a more careful derivation than the standard uniform-spacing case.

=== Standard Central Differences (Uniform Spacing)
When nodes are equally spaced with distance $h$, the second derivative approximation is:
$
  f''(x_i) approx (f(x_(i+1)) - 2f(x_i) + f(x_(i-1)))/(h^2)
$
This has *O($h^2$)* accuracy.

=== Non-Uniform Node Spacing
When spacing is non-uniform, denote:
- $h_1 = x_i - x_(i-1)$ (distance to left neighbor)  
- $h_2 = x_(i+1) - x_i$ (distance to right neighbor)

The generalized second derivative formula becomes:
$
  f''(x_i) approx (2)/(h_1 (h_1 + h_2)) f(x_i - 1) - (2)/(h_1 h_2) f(x_i) + (2)/(h_2 (h_1 + h_2)) f(x_(i+1))
$

This maintains *O(h)* accuracy (first-order) in the non-uniform case, unless the spacing variation is carefully controlled.

=== First Derivative with Non-Uniform Spacing
For the first derivative, you can use:

$
  f'(x_i) approx (h^2_2 f(x_(i-1)) - (h^2_2 - h^2_1)f(x_i) - h_1^2f(x_(i+1))) / (h_1 h_2 (h_1 + h_2))
$

=== Practical Implementation Tips
Derive locally: For each node $i$, use Taylor series expansions around $x_i$ to derive the coefficients. This ensures accuracy for whatever the local spacing happens to be.
Higher accuracy variants: If you want to maintain O(h²) accuracy with non-uniform nodes, you may need to use more points (4-point or 5-point stencils) or accept reduced accuracy.
Quasi-uniform grids: If your nodes are "nearly uniform" (spacing variations are small), the non-uniform formula above works well. If spacing is highly irregular, consider remeshing to a more regular grid if possible.
Boundary handling: Non-uniform spacing makes boundary approximations trickier—you'll need one-sided or skewed stencils at edges, derived similarly using Taylor series.


== 24 Heat Flow
A basic fact of nature is that heat flows from hot to cold, that is, from regions of
high temperature to regions of low temperature. We give these words math-
ematical expression by stating that the rate of heat flow *H* through a material
is proportional to the gradient of the temperature T within that material:

$
  H = - K nabla T (bold(x), t)
$

where K is the thermal conductivity of the material. The total amount of heat
Q(t) in the material at any one time is proportional to the integral of the tem-
perature over the volume of the material:

$
  Q(t) = integral d bold(x) C rho(bold(x)) T(bold(x),t)
$

where C is the specific heat of the material and ρ its density. Because energy
is conserved, the rate of decrease of Q with time must equal the amount of
heat flowing out of the material. When this energy balance is struck, and the
divergence theorem applied, the heat equation results:

$
  (partial T(bold(x), t)) / (partial t) = (K)/(C rho) nabla^2 T(bold(x),t)
$

The heat equation (24.3) is a parabolic PDE with space and time as inde-
pendent variables. The specification of this problem implies that there is no
temperature variation in directions perpendicular to the bar (y and z), and so
we have only one spatial coordinate in our PDE:

$
  (partial T(x,t)) / (partial t) = (K) / (C rho) (partial^2 T(x,t)) / (partial x^2)
$

=== 24.3 Solution: Finite Time Stepping (Leap Frog)
As we did with Laplace’s equation, the numerical solution is based on con-
verting the differential equation into a finite-difference (“difference”) equa-
tion. We discretize space and time on a lattice (Fig. 24.2), and look for a so-
lution along the nodes. The horizontal nodes with white centers correspond
to the known values of the temperature for the initial time, while the vertical
white nodes correspond to the fixed temperature along the boundaries. If we
also knew the temperature for times along the bottom row, then we could use
a relaxation algorithm, as we did for Laplace’s equation. However, with only
the top row known, we shall end up with an algorithm that steps forward in
time, one row at a time, as in the children’s game leapfrog.
The algorithm is customized for the equation being solved and for the con-
straints imposed by the particular set of initial and boundary conditions. With
only one row of times to start with, we use a forward-difference approxima-
tion for the time derivative of the temperature:
$
  (partial T(x,t)) / (partial t) approx (T(x,t+Delta t) - T(x,t)) / (Delta t)
$

#figure(
  image("../images/img_24_2.png"),
  caption: [
    The algorithm for the heat equation in which the temperature at the location
x = iΔx and time t = ( j + 1) Δt is computed from the temperature values at three points
of an earlier time. The nodes with white centers correspond to known initial and boundary con-
ditions. (The boundaries are placed artificially close for illustrative purposes.)
  ]
)

Because we know the spatial variation of the temperature along the entire top
row, as well as along the left and right sides, we are not as constrained with the
space derivative as with the time derivative. Consequently, as we did with the
Laplace equation, we use the more-accurate central-difference approximation
for the (second) space derivative:

When we substitute these approximations for the derivatives into the heat
equation (24.4), we obtain the heat difference equation:
$
  (partial T(x,t + Delta t)) / (Delta t) = (K)/(C rho) (T(x+Delta x, t) + T(x - Delta x, t) - 2T(x,t)) / (Delta x^2)
$

We reorder this equation to a form in which the solution can be stepped for-
ward in time:

$
  T_(i,j+1) = T_(i,j) + eta [T_(i+1,j) + T_(i-1,j) - 2T_(i,j)] \
  eta = (K Delta t) / (C rho Delta x^2)
$
where $x = i Delta x$ and $t = j Delta t$. This algorithm is called explicit because it provides
a solution in terms of known values of the temperature. If we tried to solve for
the temperature at all lattice sites simultaneously, then we would have an im-
plicit algorithm that requires us to solve equations involving unknown values
of the temperature (Fig. 24.2). We see that the temperature at space–time point
$(i, j + 1)$ is computed from the three temperature values at an earlier time j,
and at adjacent space values $i plus.minus 1$, i. We start the solution at the top row, mov-
ing it forward in time for as long as we want, keeping the temperature along
the ends fixed at $0^o C$. Fig. 24.3 shows the solution so obtained.


=== 24.4 von Neumann Stability Assessment

$
  dots.c
$

Radiating bar (Newton’s cooling): Imagine now, that instead of being
insulated along its length, a bar is in contact with an environment at a
temperature Te . Newton’s law of cooling (radiation) says that the rate of
temperature change due to radiation is
$
  (partial T) / (partial t) = -h (T - T_e)
$

where h is positive constant. This leads at the modified heat equation
$
  (partial T(x,t)) / (partial t) = (K)/(C rho) (partial^2 T)/(partial x^2) - h T(x,t) 
$
Modify the algorithm to include Newton's cooling, and compare the cooling of this bar with that
of the insulated bar.

#pagebreak()
