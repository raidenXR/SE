ReactionStep
#table(
  columns: 2,
    [reactants],[Map string int]
)

```rs
/// Represents a chemical species with thermodynamic properties
type Species = {
    Name: string
    MolecularWeight: float  // g/mol
    HeatOfFormation: float  // J/mol
    HeatCapacity:    float     // J/mol·K
}

/// Represents an elementary reaction step
type ReactionStep = {
    Reactants:            Map<string, int>
    Products:             Map<string, int>
    PreExponentialFactor: float  // A (temperature-dependent units)
    ActivationEnergy:     float       // J/mol
    ReactionOrder:        int
}

/// Chemical kinetics model state
type KineticsState = {
    Species:      Species list
    SpeciesIndex: Map<string, int>
    Reactions:    ReactionStep list
    Temperature:  float
    Pressure:     float
}

/// Results from kinetics simulation
type SimulationResult = {
    Time:           float array
    Concentrations: float array array
    SpeciesNames:   string list
}

```

#pagebreak()

+ create state
+ add species
+ add reaction
+ calculate Arrhenius rate
+ calculate reaction rate
+ calculate derivatives
+ Runge-Kutta step -> solve ODEs
+ solve

```rs
/// Create an empty kinetics state
let createState temperature pressure : KineticsState = {

/// Add a species to the kinetics model
let addSpecies name mw hf cp (state: KineticsState) : KineticsState =

/// Add a reaction to the mechanism
let addReaction reactants products a ea order (state: KineticsState) : KineticsState =

/// Calculate rate constant using Arrhenius equation: k = A * exp(-Ea / R*T)
let arrheniusRate temperature a ea : float =

/// Calculate the rate of a specific reaction
let calculateReactionRate temperature concentrations reactionIdx (state: KineticsState) : float =

/// Calculate derivatives: dC/dt = f(t, C, T)
let calculateDerivatives temperature concentrations (state: KineticsState) : float array =
    
/// Single step of 4th-order Runge-Kutta integration
let rungeKuttaStep dt time concentration temperature (state: KineticsState) : float array =

/// Solve the kinetics system using RK4 integration
let solve (initialConcentrations: Map<string, float>) (timeSpan: float array) 
        (temperatureProfile: float option) (state: KineticsState) : SimulationResult =

```

= Kinetics of Chemical Reactions

== 2. Chemical Reactions and Complexity
