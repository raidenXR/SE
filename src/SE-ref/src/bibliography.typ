
#show link: set text(blue)

= Bibliography

// quadtrees
- Terrain Decimation through Quadtree Morphing
- #link("https://ciphrd.com/articles/building-a-quadtree-filter-in-glsl-using-a-probabilistic-approach/")[quadtree morphing]
- Collision Detection in Interactive 3D Enviroments

// raycasting - filling rays
- #link("https://en.wikipedia.org/wiki/Ray_casting")[stockastic filter]
- #link("https://www.geeksforgeeks.org/c/scan-line-polygon-filling-using-opengl-c/")[scan-line filling algorithm]

// Wiley
- Computational Physics (Landau - 2010)
- Kinetics of Chemical Reactions - Decoding Complexity (2019)

// βιβλιοθήκη
- Computational Methods in Chemical Engineering
- Εισαγωγή στην Υπολογιστική Ρευστοδυναμική: Η Μέθοδος των Πεπερασμένων Όγκων

// βιβλιογραφία/fuels/
- Combustion - 2013 - Lackner
- Handbook of Fuels
- Handbook of Combustion vol I.
- Handbook of Combustion vol III.

// ECS
- Exploring the Theory and Practice of Concurrency in the Entity-Component-System Pattern
- HECATE: An ECS-based Framework for Teaching and Developing Multi-Agent Systems
- Project Elements: A computational entity-component-system in a scene-graph pythonic framework, for a neural, geometric computer graphics curriculum
- The Essence of Entity Component System

// Rotary engine
- CFD simulation methodology for Wankel engines
- Rotary Engine by Kenichi Yamamoto
- Renesis Rotary Engine Fundamentals
- Internal Combusiton Engines (Kirkpatrick)

// articles
- A comprehensive review of detailed kinetics models for ethanol combustion chemistry


#pagebreak()


1. Key Physical and Chemical Processes

The combustion process in an ICE involves:

+ Turbulent flow of the gas-air mixture.
+ Chemical reactions (oxidation of fuel).
+ Heat release and temperature rise.
+ Pressure and volume changes (work done).
+ Species transport (fuel, air, combustion products).

2. Governing Equations

The combustion process can be modeled using the following conservation equations:

 A. Mass Conservation (Continuity Equation)
$
  (partial rho) / (partial t) = nabla dot.c (rho bold(u)) = 0
$
where:

    ρρ = density of the gas mixture,  \
    uu = velocity vector,  \
    tt = time.  \

B. Momentum Conservation (Navier-Stokes Equations)
$
  (partial (rho u))/(partial t) + nabla dot.c (rho bold(u) bold(u)) = -nabla p + nabla tau + bold(F)
$
where:

    pp = pressure,  \
    ττ = viscous stress tensor,  \
    FF = body forces (e.g., gravity, but often negligible in ICE).  \

C. Energy Conservation
$
  (partial rho E)/(partial t) + nabla dot.c (rho E bold(u)) = -nabla dot.c (p bold(u)) + nabla dot.c (tau dot.c bold(u)) - nabla dot.c bold(q) + dot(Q)
$
where
where:

    EE = total energy (internal + kinetic), \
    qq = heat flux (Fourier’s law: q=−k∇Tq=−k∇T),  \
    Q˙Q˙​ = heat release rate from combustion.  \

D. Species Conservation (for each species)
$
  (partial (rho Y_i))/(partial t) + nabla dot.c (rho Y_i bold(u)) = nabla dot.c (rho D_i nabla Y_i) + dot(omega_i)  
$

where:

    YiYi​ = mass fraction of species ii, \
    DiDi​ = diffusion coefficient, \
    ω˙iω˙i​ = net production rate of species ii (from chemical reactions). \

E. Equation of State (Ideal Gas Law)
$
  p = rho R T sum_i (Y_i)/(W_i)
$

where:

    RR = universal gas constant, \
    WiWi​ = molecular weight of species ii, \
    TT = temperature. \

3. Chemical Kinetics (Reaction Rates)

The combustion process involves a series of chemical reactions. The rate of reaction for each species is given by:
$
  dot(omega_i) = W_i sum_(j=1)^N_r (v_(i j) - v'_(i j))r_j
$
where:

    NrNr​ = number of reactions, \
    νijνij​ = stoichiometric coefficient of species ii in reaction jj (products), \
    νij′νij′​ = stoichiometric coefficient of species ii in reaction jj (reactants), \
    rjrj​ = reaction rate of reaction jj. \

The reaction rate rjrj​ is typically modeled using the Arrhenius equation:
$
  r_j = k_j product_(i=1)^N_s [X_i]^(v'_(i j))
$
where:

    kj=AjTbjexp⁡(−Ea,j/(RT))kj​=Aj​Tbj​exp(−Ea,j​/(RT)) = rate constant, \
    [Xi][Xi​] = molar concentration of species ii,  \
    AjAj​ = pre-exponential factor,  \
    bjbj​ = temperature exponent,  \
    Ea,jEa,j​ = activation energy.  \

4. Turbulence Modeling
5. Combustion Models
6. Numerical Methods

To solve the governing equations, numerical methods are required:

- Discretization: Finite Volume Method (FVM) is commonly used. 
- Time Integration: Implicit or explicit schemes (e.g., Crank-Nicolson, Runge-Kutta). 
- Pressure-Velocity Coupling: SIMPLE, PISO, or SIMPLEC algorithms. 
- Solver: OpenFOAM, ANSYS Fluent, or Cantera (for 0D/1D models). 

7. Boundary and Initial Conditions
- Inlet: Specify velocity, temperature, and species mass fractions (e.g., ϕ=1.0ϕ=1.0 for stoichiometric mixture).
- Outlet: Pressure boundary condition (e.g., atmospheric pressure).
- Walls: No-slip condition for velocity, specified temperature or heat flux.
- Initial Conditions: Uniform or stratified mixture in the combustion chamber.

10. Validation and Post-Processing

- Validation: Compare results with experimental data or literature (e.g., pressure traces, flame speed).
- Post-Processing: Use ParaView or Python (Matplotlib) to visualize:
   - - Temperature contours,
   - - Species mass fractions,
   - - Pressure and velocity fields.

11. Advanced Topics

- Knocking: Model auto-ignition using reduced mechanisms (e.g., Shell model).
- Cycle-to-Cycle Variations: Use LES or stochastic models.
- Spray Combustion: For direct-injection engines, model fuel spray breakup and evaporation.
- Pollutant Formation: Model NOx, soot, and CO using additional sub-models.

13. Challenges and Considerations

- Computational Cost: Detailed chemistry and turbulence models are expensive.
- Numerical Stability: Stiff ODEs from chemical kinetics require implicit solvers.
- Boundary Conditions: Accurate inlet/outlet conditions are critical.
- Validation: Experimental data is needed for model validation.
 
