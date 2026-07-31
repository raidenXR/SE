#set page(numbering: "1" )

= 1 Introduction

== 1.3 Three Types of Chemical Kinetics
Presently, chemical kinetics is an area comprising challenges and adventures, in
which at least four sciences overlap: chemistry, physics, chemical engineering,
and mathematics. In fact, contemporary chemical kinetics itself is a complex
combination of diﬀerent areas. Depending on the goal of a kinetic analysis, one
may distinguish between applied kinetics, detailed kinetics, and mathematical
kinetics.

=== 1.3.1 Applied Kinetics
The goal of applied kinetics is to obtain kinetic dependences for the design of
eﬃcient catalytic processes and reactors. Kinetic dependences are dependences
of the rates of chemical transformations on reaction conditions, that is, tem-
perature, pressure, concentrations, and so on. When expressed mathematically,
these dependences are called kinetic models. A kinetic model is the basis of the
mathematical simulation of a chemical process. A series of models needs to be
developed for the simulation of a catalytic reactor: kinetic model → model of cata-
lyst pellet → model of catalyst bed → model of reactor. In this hierarchy of models,
introduced by Boreskov and Slin’ko [3], the kinetic model represents the initial
level, the foundation. No technologically interesting description of a chemical
reactor can be given without reference to a kinetic model. Applied kinetic mod-
els are, as a rule, stationary; they are based on kinetic data obtained at steady-state
conditions.
During the past 25 years, a lot of attention has been paid to the problem of
selecting the best catalyst via so-called “combinatorial catalysis” procedures,
which involve simultaneous steady-state testing of many diﬀerent catalyst
samples. However, the technique and methodology for precise kinetic catalyst
characterization is still far from being complete, in particular for catalyst char-
acterization at non-steady-state conditions. Such characterization is a critical
issue in the design of a new generation of catalysts.

=== 1.3.2 Detailed Kinetics
The study of detailed kinetics is aimed at reconstructing the detailed mechanism
of a reaction, based on kinetic and non-kinetic (adsorption, desorption, spectro-
metric, etc.) data. The concept of a detailed mechanism may be used in a broad as
well as a narrow sense. In its application to catalytic reactions, one should specify
reactants, products, intermediates, reaction steps, surface properties, adsorption
patterns, and so on.
In the practice of chemical kinetics, detailed kinetics is often used in a more
narrow sense, as a set of elementary reaction steps. Each elementary step consists
of a forward and a reverse elementary reaction, whose kinetic dependences are
governed by the mass-action law.

=== 1.3.3 Mathematical Kinetics
Mathematical kinetics deals with the analysis of various mathematical models
that are used in chemical kinetics. As a rule, these are deterministic models con-
sisting of a set of algebraic, ordinary diﬀerential or partial diﬀerential equations.
There are also stochastic models that are based on Monte Carlo methods, for
modeling adsorption or surface-catalytic reactions, reaction–diﬀusion processes
in the catalyst pellet or in the catalyst bed, and so on.
Problems related to mathematical kinetics may be either direct kinetic prob-
lems or inverse kinetic problems. A direct kinetic problem requires the analysis of
a given kinetic model, either steady-state or non-steady-state, with known kinetic
parameters. In contrast, solving an inverse kinetic problem involves reconstruct-
ing the kinetic dependences and estimating their parameters based on experi-
mental kinetic data, either steady-state or non-steady-state.

\
Chemical kinetics is certainly an intrinsic area of chemistry. However, it can also
be viewed as a bridge between chemistry, chemical engineering, and physics on
one side and mathematics on the other. That is why we have presented several
mathematical concepts, the understanding of which is absolutely necessary for
the contemporary researcher working or going to work in this area. These con-
cepts include “linear algebra” (Chapter 4), “graph theory” (Chapters 5 and 6),
“ordinary diﬀerential equations” and “stability theory” (Chapters 7 and 8), “al-
gebraic models” (Chapter 8) and “partial diﬀerential equations” (Chapter 10).

= 2 Chemical Reactions and Complexity

== 2.1 Introduction
It is a well-established fact that most chemical reactions are of a complex nature.
For example, the oxidation of hydrogen is typically described by the simple overall
reaction
$
    2 H_2 + O_2 arrows.rl 2 H_2O
$
but in accordance with contemporary knowledge, the detailed mechanism of this
reaction [1, 2] consists of many steps, referred to as elementary steps the following table.
  
#figure(
    table(
      columns: (auto, 1fr, auto, 1fr),
      align: (left, left, left, left),
      stroke: none,
      table.hline(),
      [*Step*], [*Reactions*], [*Step*], [*Reactions*],
      table.hline(),
      [(1)], [$H_2 + O_2 arrows.rl 2 "OH"^•$], [(16)], [$H^• + "HO"_2^• arrows.rl 2 "OH"^•$],
      [(2)], [$"OH"^• + H_2 arrows.rl H_2 O + H^•$], [(17)], [$H^• + "HO"_2^• arrows.rl H_2 O + O^•$],
      [(3)], [$H^• + O_2 arrows.rl "OH"^• + O^•$], [(18)], [$H^• + "HO"_2^• arrows.rl H_2 + O_2$],
      [(4)], [$O^• + H_2 arrows.rl "OH"^• + H^•$], [(19)], [$O^• + "HO"_2^• arrows.rl "OH"^• + O_2$],
      [(5)], [$O^• + H_2 O arrows.rl 2 "OH"^•$], [(20)], [$H^• + H_2 O_2 arrows.rl H_2 O + "OH"^•$],
      [(6)], [$2 H^• + M arrows.rl H_2 + M$], [(21)], [$O^• + H_2 O_2 arrows.rl "OH"^• + "HO"_2^•$],
      [(7)], [$2 O^• + M arrows.rl O_2 + M$], [(22)], [$H_2 + O_2 arrows.rl H_2 O_2 + O^•$],
      [(8)], [$H^• + "OH"^• + M arrows.rl H_2 O + M$], [(23)], [$H_2 + O_2 arrows.rl H_2 O_2 + M$],
      [(9)], [$2 "OH"^• + M arrows.rl H_2 O_2 + M$], [(24)], [$"OH"^• + H_2 O_2 arrows.rl H^• + "HO"_2^•$],
      [(10)], [$"OH"^• + O^• + M arrows.rl "HO"_2^• + M$], [(25)], [$"HO"_2^• + "OH"^• arrows.rl H_2 O + O_2$],
      [(11)], [$H^• + O_2 + M arrows.rl "HO"_2^• + M$], [(26)], [$H_2 + O^• + M arrows.rl H_2 O + M$],
      [(12)], [$"HO"_2^• + H_2 arrows.rl H_2 O_2 + H^•$], [(27)], [$O^• + H_2 O + M arrows.rl H_2 O_2 + M$],
      [(13)], [$"HO"_2^• + H_2 arrows.rl H_2 O + "OH"^•$], [(28)], [$O^• + H_2 O_2 arrows.rl H_2 O + O_2$],
      [(14)], [$"HO"_2^• + H_2 O arrows.rl H_2 O_2 + "OH"^•$], [(29)], [$H_2 + H_2 O_2 arrows.rl 2 H_2 O$],
      [(15)], [$2 "HO"_2^• arrows.rl H_2 O_2 + O_2$], [(30)], [$H^• + "HO"_2^• + M arrows.rl H_2 O_2 + M$],
      table.hline(),
    ),
    caption: [Kinetics of Chemical Reactions: Decoding Chemical Complexity Wiley - 2018]    
)

== 2.2 Elementary Reactions and the Mass-Action Law
If an elementary reaction involves one reactant molecule
$(A → B)$, it is classiﬁed as a unimolecular reaction or a ﬁrst-order reaction. If
two molecules take part in the reaction (e.g. 2A → B or A + B → C), the reaction
is called bimolecular or second order. With the participation of three molecules
(e.g. $3A → B or 2A + B → C$), the reaction is said to be termolecular or third
order. The simultaneous interaction of more than three reactant molecules in one
elementary reaction is believed to be highly improbable and even termolecular
reactions are very rare.
The reaction rate of an elementary step is determined by the diﬀerence between
the rates of the forward and the reverse reactions:
$
    r = r^+ = r^-
$
where $r$, $r+$$ , and $r−$$ are the rate of the step, the rate of the forward reaction, and
the rate of the reverse reaction (mol m−3 s−1 ), respectively.

=== 2.2.1 Homogeneous Reactions
Consider the general elementary step:
$
    a_1 A_1 + a_2 A_2 dots arrows.rl ^(k^+)_(k^+) beta_1 B_1 + beta_2 B_2 dots
$
or, equivalently:
$
    sum a_i A_i arrows.rl_(k^-)^(k^+) sum beta_i B_i
$
where Ai and Bi are reactants and products with 𝛼 i and 𝛽 i the absolute values of
their stoichiometric coeﬃcients, and k + and k − are the rate coeﬃcients for the
forward and reverse reaction, respectively. In addition to the limitation on the
values of $a_i and beta_i (<=3)$, the sum of coeﬃcients 𝛼 i must also not be greater than
three.
The dependence of the rates of the forward and reverse reactions on the con-
centrations of reactants is expressed in terms of the mass-action law as
$
    r^+ = k^+ c_(A_1)^(a^1) c_(A_2)^(a_2) dots = k^+ product c_(A_i)^(a_i)
$
$
    r-+ = k^- c_(B_1)^(beta^1) c_(B_2)^(beta_2) dots = k^+ product c_(B_i)^(beta_i)
$
where $c_(A_i)$ and $c_(B_i)$ are the concentrations of reactants and products (mol m−3 ),
respectively. The rate coeﬃcients $k^+$ and $k^− (s^(−1))$ determine the reaction rates of
the forward and the reverse reaction at unitary values of reactant concentrations.
They are governed by the Arrhenius dependence and increase exponentially with
temperature:
$
    k^+ = k_o^+ exp (-(E_a^+)/(R_g T))
$
$
    k^- = k_o^- exp (-(E_a^-)/(R_g T))
$
where $k_0^+$ and $k_0^−$ are pre-exponential factors $(s^(−1))$, $E_a^+$ and $E_a^−$ are activation ener-
gies $(J "mol"^(−1))$, $R_g$ is the universal gas constant $(8.314 J K^(−1) "mo"l^(−1))$, and $T$ is the
absolute temperature $(K)$.
The ratio of the rate coeﬃcients of the forward and reverse reaction determines
the equilibrium coeﬃcient:
$
    K_"eq" = (k^+)/(k^-)
$

The diﬀerence between the activation energies for the forward and reverse reaction determines
the reaction enthalpy:
$
    Delta_r H = E_a^+ - E_a^-
$
For an exothermic reaction, in which heat is released, $Delta_r H < 0$. For an endother-
mic reaction, in which heat is consumed, $Delta_r H > 0$.

Most “real” reactions are now considered to be multistep and complex. Nev-
ertheless, in the literature for some “real” cases one can ﬁnd mechanisms that
consist of only a single elementary reaction. This always looks a little peculiar and
reﬂects some level of knowledge.

The reaction
$
    "CO" + "O"_2 arrow.r "CO"_2 + "O"
$
is bimolecular or second order with reaction rates $r = k c_"CO" c_"O"_2$.
The reaction
$
    2"NO" + "O"_2 -> 2"NO"_2 
$
is termolecular or third order with $r = k c_"2NO"c_"O2"$ .
Stoichiometric equations for “real” complex reactions are free from the lim-
itations that are set on the stoichiometric coeﬃcients of elementary reactions.
For example, the $"C"_2"H"_4"O"$ oxidation reaction can be represented by the following
stoichiometric relation:
$
    2"C"_2"H"_4"O" + 5"O"_2 -> 4"CO"_2 + 4"H"_2"O"
$

== 2.2.3 Rate Expressions
The rate of an elementary reaction can be deﬁned as the number of elementary
acts of chemical transformation per unit volume of the reaction mixture (or unit
catalyst surface area, etc.) per unit time. For a system without exchange of matter
with the surrounding medium (closed system, see Chapter 3), the rate of a single
stoichiometric reaction can be expressed as
$
  r = -1/(a_i V) (d n_(A_i))/(d t) = 1/(beta_i V) (d n_(B_i))/(d t)  
$
where $n_(A_i)$ and $n_(B_i)$ are the number of moles of reactants and products and $V$ is the
volume of reaction mixture. The reaction rate $r$ is expressed in $"mol" m^(−3) s^(−1)$.
More speciﬁcally, for a heterogeneous catalytic reaction in a closed system, for
example, a gas–solid reaction, the reaction rate can be expressed as
$
  r = -1/(a_i S_"cat") (d n_(A_i))/(d t) = 1/(beta_i S_"cat") (d n_(B_i))/(d t)
$
where $S_"cat"$ is the catalyst surface area ($m^2_"cat"$) and the reaction rate $r_S$ is the rate per
unit catalyst surface area ($"mol m"^(−2)_"cat" s^(-1)$). The reaction rate can also be expressed
 per unit volume of catalyst $V_"cat" (m^3_"cat")$, with $r_V$ in $("mol m"^(−3) $cat$ s^(-1))$, or per unit mass
of catalyst $W_"cat" (k g_"cat")$, with $r_W$ in $"mol" k g_"cat" s^(-1)$. These rates can be easily transformed:
$
    r_S = V_"cat" / S_"cat" r_V = (V_"cat" rho_"cat")/(S_"cat") r_W = (W_"cat")/(S_"cat") r_W
$
where $rho_"cat"$ is the density of the catalyst ($"kgcat m"^(−3)"cat"$).
For chemical processes without a change in the number of moles during the
course of the reaction, Eq. (2.14) takes the traditional form
$
   r = -1/a_i (d c_(A_i))/(d t) = 1/beta_i (d c_(Beta_i))/(d t)
$
with concentration in $"mol m"^(-3)$.
Similarly eq2.15 can be written as
$
   r_w = -(V_f)/(a_a S_"cat") (d C_(A_i))/(d t) = (V_f)/(beta_i S_"cat") (d c_(Beta_i))/(d t)    
$
Where $V_f$ is the volume of the fluid phase $(m^3)$.

== 2.3 The Reaction Rate and Net Rate of Production of a Component – A Big Diﬀerence
In many cases, the number of reactant molecules converted or the number of
product molecules formed each time the reaction occurs, as indicated by the sto-
ichiometric coeﬃcient, is not necessarily equal to one, neither in overall reactions
nor in elementary reactions. This results in a diﬀerence between the reaction rate,
$r$, and the rate of change of a component by consumption or production in the
course of a single reaction or multiple reactions, also termed its net rate of pro-
duction, $R_i$ . Many mistakes in the literature are caused by not understanding this
diﬀerence. For a single stoichiometric reaction, the relationship between $r$ and $R_i$ can be
expressed as follows:
$
    r = R_i/v_i " or " R_i = v_i r
$
where $𝜈_i$ is the stoichiometric coeﬃcient of chemical component $i$. The conven-
tion is to assign negative coeﬃcients to reactants and positive coeﬃcients to
products. Thus $R_i$ is also negative for reactants and positive for products. For
example, for the elementary step $A arrows.rl B$
$
    r = R_A/v_A = R_B/v_b = R_A/(-1) = R_B/1 = -R_A = R_B " or " R_A = -r ", " R_B = r 
$
and for elementary step $2A + arrows.rl 3C$
$
  r = -R_A/2 = -R_B = R_C/3 " or " R_A = -2r ", " R_B = -r ", " R_C = 3r     
$
Because of this diﬀerence, the deﬁnitions of the net rate of production of a com-
ponent and the reaction rate have to be carefully distinguished. The net rate of
production of a component is an experimentally observed characteristic. It is
the change of the number of moles of a component per unit volume of reactor
(or catalyst surface, volume or mass) per unit time.
The reaction rate r can be introduced only after a chemical reaction equation
has been assumed with the corresponding stoichiometric coeﬃcients. Then, the

value of reaction rate can be calculated based on the assumed stoichiometric
equation using Eq. (2.19). Thus, there is a big conceptual diﬀerence between the
experimentally observed net rate of production of a component, $R_i$ , and the calcu-
lated reaction rate, $r$. This diﬀerence between the two rates has to be taken into
account even if we consider our reaction as a single one, say an isomerization
reaction of a reactant $A$ into a product $B$. Even knowing the rate of production of
$B$, we will obtain diﬀerent rates of reaction depending on what kind of elementary
reaction we are going to assume: $A -> B " or " 2A -> 2B$.
In the case that a component is participating in multiple reactions, $R_i$ is
a linear combination of the rates in which this component is consumed or
formed in the steps taking place, $r_s$. The coeﬃcients in this linear combi-
nation are the stoichiometric coeﬃcients $𝜈_"is"$ of the component in each of
the steps
$
    R_i = sum_s r_"is" = sum_s v_"is" r_s
$
The main methodological lesson of this analysis is: “Do not mix the experiment
with its interpretation.” The net rate of production of a component is an exper-
imentally measured value. The chemical reaction equation, on the other hand,
is a result of our interpretation, and it can be written arbitrarily. Therefore, the
reaction rate calculated in accordance with this reaction equation is part of our
interpretation as well.

== 2.4 Dimensions of the Kinetic Parameters and Their Orders of Magnitude
The dimension of the rate coeﬃcient k depends on the type of chemical reaction.
In the case of a homogeneous reaction, that is, a reaction involving a single phase,
the dimension of $k$, $[k]$, is
$
    [k^+]ι = ([r])/([product_i c^(a_i)_A_i])
$
for the forward reaction of Eqs. (2.2a) and (2.2b) and
$
    [k^-]ι = ([r])/([product_i c^(beta_i)_B_i])
$
for the reverse reaction of Eqs. (2.2a) and (2.2b).
Table 2.3 shows the dimension of the rate coeﬃcient for the three types of
elementary reactions. The dimension of the pre-exponential factor is the same
as the dimension of the rate coeﬃcient. Tables 2.4 and 2.5 show typical values of
the kinetic parameters for ﬁrst-order reactions. The pre-exponential factor for a
unimolecular reaction is about $10^(13) s^(−1)$.

#image("../images/tables.png", width: 50%)

*TODO: REMOVE The heterogeneous part of the kinetics, since the system is homogeneous*

== 2.5 Conclusions
In this chapter, we have used the term “elementary reaction.” In the literature
one can ﬁnd diﬀerent meanings of this term. Moreover, diﬀerent antonyms
are discussed: “elementary” – “complex,” “elementary” – “multiple,” and
“simple – complex.”
For a reaction to be considered elementary:
- it should be part of Van’t Hoﬀ’s “natural classiﬁcation,” that is, the reaction is
assumed to be unimolecular, bimolecular, or termolecular;
- its rate must be governed by the mass-action law;
- it must take place, according to the IUPAC Gold book [3] and Laidler [5], by
overcoming one energetic barrier according to the principle “one energetic
barrier – one elementary reaction.”

A reaction is not necessarily elementary if only one of the above statements is
true. For example, many reactions in which one, two, or three components are
participating are not elementary. Furthermore, in some cases the kinetic law of
a complex reaction may be approximated by the kinetic mass-action law of an
elementary reaction.
Nevertheless, the main paradigm of contemporary chemical kinetics is the fol-
lowing: a chemical reaction is complex and consists of elementary reactions for
which the kinetic law is assumed to be known.
The theoretical concepts presented in this chapter were introduced into the
ﬁeld of chemical science during a span of about one hundred years, from the
1860s to the 1960s.

= 3 Kinetic Experiments: Concepts and Realizations

== 3.1 Introduction
Kinetic experiments are performed in various types of reactors. Chemical
reactors can be classiﬁed as either open or closed reactors, depending on whether
there is exchange of matter with the surroundings. This classiﬁcation has been
adopted from thermodynamics, in which a distinction is made between open
and closed systems. Closed reactors can exchange energy and work with the
surroundings, but they cannot exchange matter, while open reactors can also
exchange matter. There are also semi-open (or semi-closed) reactors, in which
only some type of matter is exchanged with the surroundings. In chemical
kinetics and engineering, the closed reactor is better known as a batch reactor,
and the open reactor as continuous-ﬂow reactor. In pulse reactors, a small
quantity of a chemical substance is injected into the reactor.

== 3.2 Experimental Requirements
The chemical processes occurring in reactors, including laboratory reactors, are
complex and do not only consist of chemical reactions but also comprise physical
phenomena, such as mass and heat transport. The major goal of chemical kinetic
studies is to extract intrinsic kinetic information related to the complex chemical
reaction. Therefore, the transport regime in the reactor has to be well deﬁned and
its mathematical description has to be reliable. We will use the latter as a “mea-
suring stick” for extracting the kinetic information. A typical strategy in kinetic
experiments is the minimization of the eﬀects of mass and heat transfer on the
rate of change of the chemical composition. In accordance with this, the kinetic
experiment ideally has to fulﬁll two main requirements: isothermicity and unifor-
mity of the chemical composition. This can be achieved by, for example, perfect
mixing within the reaction zone.
A kinetic experiment should usually be performed under near isothermal con-
ditions. The temperature may be changed between two experiments. Tempera-
ture gradients across the reactor can be minimized in various ways, for example,
by intensive heat exchange between the reactor and the surroundings, by dilution
of the reactive medium, or by its rapid recirculation.
Uniformity of the chemical composition at the reactor scale is achieved by
intensive mixing using special mixing devices, either internal impellers or exter-
nal recirculation pumps.
Both isothermicity and uniformity of the chemical composition can also be
attained in reactors in which the reaction zone is suﬃciently small, such as diﬀer-
ential plug-ﬂow reactors (PFRs), shallow beds, and temporal analysis of products
(TAP) reactors with a thin zone of catalyst.

== 3.3 Material Balances
The material balance for any chemical component in a reactor can be presented
qualitatively as
$
    "temporal change of amount of component" = \ "transport" + "change due to reaction"
$
in which the temporal change of the amount of component, often termed accu-
mulation, is its change with respect to time at a ﬁxed position, the transport
change is the change caused by motion of the component and the reaction change
is the change caused by chemical reaction.
Rigorously, this equation is presented as the equation of change describ-
ing the composition of multicomponent mixtures, the so-called continuity
equation − see the classical textbook by Bird et al. [1]. The transport processes
governing the “transport change” are rather complex. Typically, they include
at least two types of processes: convection and diﬀusion. For convection, the
molar ﬂow rate $F_i ("mol" s^(−1))$ of a component i is determined as the product of the
total volumetric ﬂow rate $q_V (m^3 s^(−1))$ and the concentration of the component $c_i ("mol" m^(−3))$:
$
    F_i = q_V c_i
$
For diﬀusion, in the simplest case the molar ﬂow rate of a component is deter-
mined in accordance with Fick’s ﬁrst law:
$
    F_i = -D_i A (d c_i)/(d z)
$
where $D_i$ is the diﬀusion coeﬃcient $(m^2 s^(−1))$, A is the cross-sectional area of the
reactor available for ﬂuid ﬂow (m^2), and z is the axial reactor coordinate $(m)$.
In the model describing a batch reactor, the transport change term is absent
based on the assumption of perfect mixing.
Strictly speaking it is not necessary to have perfect mixing in the reaction
zone as long as the characteristics of the hydrodynamic regime are well deﬁned.
Pure convection and pure diﬀusion processes are examples of such well-deﬁned
regimes. We only need to know the hydrodynamic regime with its correspond-
ing mathematical description, which will be used as a “measuring stick” for
extracting the intrinsic kinetic dependences.

Quite often the importance of transport phenomena has to be assessed at dif-
ferent scales, with that of the reactor being the largest. In solid-catalyzed reac-
tions, the scale of the catalyst pellets also has to be considered. The inﬂuence of
inter- and intraparticle transport on the reaction rate has to be eliminated exper-
imentally and/or estimated quantitatively prior to the kinetic experiments.

== 3.4 Classiﬁcation of Reactors for Kinetic Experiments
Equation (3.1) can be used for the classiﬁcation and qualitative description of
diﬀerent types of reactors for kinetic studies. Figure 3.1 shows schematic repre-
sentations of several of reactor types.

=== 3.4.1 Steady-state and Non-steady-state Reactors
In non-steady-state reactors, the temporal change of the concentration of a component,
$d c_i \/ d t eq.not 0$, while in steady-state reactors $d c_i \/ d t = 0$.

=== 3.4.2 Transport in Reactors
In perfectly mixed convectional reactors, the “transport change” can be repre-
sented as the diﬀerence of convectional molar ﬂow rates:
$
    F_(i 0) - F_i = q_(V 0) c_(i 0) - q_V c_i
$
where $q_(V 0)$ and $q_V$ are the inlet and outlet volumetric ﬂow rates, respectively,
and $c_(i 0)$ and $c_i$ are the inlet and outlet concentrations, or if $q_V = q_V 0$ , it can be
represented as
$
    F_(i 0) - F_i = q_V (c_(i 0) - c_i)
$
In purely diﬀusional reactors, the “transport change” in the simplest case can
be represented as the diﬀerence between diﬀusional ﬂow rates in and out, $F_(i 0)$
and $F_i$ . Both ﬂow rates are written in accordance with Fick’s ﬁrst law:
$
    F_(i 0) = -D_i A [(partial c_i)/(partial z)] _z " , " F_i = -D_i [(partial c_i)/(partial z)]_(z + Delta z)
$
then
$
    F_(i 0) - F_i = (-D_i A [(partial c_i)/(partial z)]_z) - (-D_i A [(partial c_i)/(partial z)]_(z+Delta z)) = D_i A (partial^2 c_i)/(partial z^2) Delta z
$

=== 3.4.3 Ideal Reactors
In this section, we will consider ideal reactors of constant reaction volume in
which a stoichiometrically single reaction takes place, without explicitly taking
into account the presence of a solid catalyst; that is, we are assuming the reaction
is not catalyzed or is homogeneously catalyzed. Reaction rates are all expressed
in moles per unit of reaction volume per second $("mol" m^(−3) s^(−1))$. If solid catalysts
are involved, it is more convenient to express reaction rates per unit mass or unit
surface area of catalyst (Sections 2.2 and 3.4.4).

==== 3.4.3.1 Batch Reactor
In an ideal batch reactor, that is, a non-steady-state closed reactor with perfect
mixing, Eq. (3.1) becomes
$
    "temporal change of amount of component" = "change due to reaction"
$
The simplest mathematical model for the temporal change of any component
in a batch reactor of constant reaction volume is
$
    1/V (d n_i)/(d t) = (d c_i)/(d t) = R_i
$
where $V$ is the reaction volume $(m^3)$, ni is the number of moles of component
$i ("mol")$, and $R_i$ is the net rate of production of component i per unit of reaction
volume $("mol" m^(−3) s^(−1))$.
In chemical kinetics and chemical engineering, the concept of fractional conversion, or simply conversion,
$X_i$ , is widely used. $X_i$ is dimensionless and can
take values from 0 to 1. The conversion of a component in a batch reactor is
deﬁned as
$
    X_i = (n_(i 0) - n_i)/(n_(i 0)) " ; " n_i = n_(i 0) (1 - X_i)
$
or when the reaction volume is constant as
$
    X_i = (c_(i 0) - c_i)/(c_(i 0)) " ; " c_i = c_(i 0)(1 - X_i)
$
Then, Eq. can be written as
$
    c_(i 0) (d X_i)/(d t) = R_i
$
=== 3.4.3.2 Continuous Stirred-tank Reactor
A continuous stirred-tank reactor (CSTR) is an open reactor with perfect mix-
ing (gradientless reactor) and only convective ﬂow. Mixing can be achieved not
only by internal but also by external recirculation. The material balance for any
component in a non-steady-state CSTR can be written as
$
    dots
$

==== 3.4.3.3 Plug-ﬂow Reactor
In an ideal PFR, it is assumed that perfect uniformity is achieved in the radial
direction, which is the direction perpendicular to that of the ﬂow. This is rela-
tively easy to achieve in tubular reactors with high aspect ratio, that is, with large
length-to-diameter ratio. Axial diﬀusion eﬀects are also neglected. The compo-
sition of the ﬂuid phase varies along the reactor, so the material balance for any
component must be made for a diﬀerential element:

=== 3.4.5 Determination of the Net Rate of Production
Summarizing, the conceptual diﬀerence between the diﬀerent methods of
measuring the net rate of production, Eqs. (3.9), (3.15), (3.26), (3.30), (3.33),
and (3.34) is as follows: in the non-steady state batch reactor, the net rate of
production is determined from the time derivative of the reactant concentration,
$R_i prop − d c_i \/ d_t$. In the steady-state CSTR, the net rate of production is
the ratio of the concentration diﬀerence of the component to the space time,
$R_i = (c_(i 0) − c_i )/tau$.
Finally, in an integral PFR, the net rate of production is determined from the
derivative of the component concentration with respect to the axial position in
the reactor, $R_i prop − d c_i \/d z$, which, with $tau = z\/u$ can be written as $R_i prop − d c_i \/d tau$. See
Section 3.6 for further elaboration.
The conceptual material balance equation, Eq. (3.1), is often written as
$
    "termporal change of amount of component" \ = "flow in" - "flow out" + "change due to reaction"    
$
In a batch reactor, both “ﬂow in” and “ﬂow out” terms are absent, while in a
CSTR both ﬂow terms are present. In a PFR, both ﬂow terms are present too,
but “ﬂow in–ﬂow out” is presented in diﬀerential form. In pulse reactors, initially
there is only the “ﬂow in” term, while later there is only the “ﬂow out” term. The
next section elaborates further on all of these aspects.

== 3.5 Formal Analysis of Typical Ideal Reactors

=== 3.5.1 Batch Reactor

==== 3.5.1.1 Irreversible Reaction
For a single irreversible reaction, A → B, taking place in a batch reactor with
constant reaction volume, the material balance for reactant A can be written as
$
    (d c_A)/(d t) = R_A = -r
$
Assuming a ﬁrst-order reaction, $−R_A = r = k c_A$ , and using Eq. (3.11) we can
write Eq. (3.36) as
$
    (d X_A)/(d t) = k (1 - X_A)
$
At $t=0, X_A=0$ and integrating from $t=0$ to $t=t$ yields
$
    X_A = 1 - exp(-k t)
$
and
$
    c_A = C_(A 0) (1 - X_A) = c_(A O) exp (-k t)
$
or
$
    ln(c_(A O)/c_A) = k t
$
The half-life $t_(1\/2)$ , that is, the time interval required for the concentration of A
to decrease to half of its initial value, obeys the relation
$
    t_(1\/2) = (ln 2)/k approx 0.693/k
$
It has to be stressed that the stoichiometry of the overall reaction does not auto-
matically determine the kinetic dependence of the reaction rate. This dependence
could also be zero order in $A$, $r = k$; or second order, $r = k c^2_A$ , and so on. Typically,
four types of empirical kinetic dependences are analyzed with the corresponding
expressions for the conversion, which are shown in Figure 3.2.\
Zero order:
$
    r = k " ; " X_A = (k t)/C_(A 0)
$
First order:
$
    r = k c_A " ; " X_A = 1 - exp(-k t)
$
#image("../images/table_ch_3.png", width: 80%)
Second order:
$
    r = k c^2_A " ; " X_A = (k c_(A 0) t) / (1 + k c_(A 0) t)
$
fractional order:
$
    r = k c^n_A " ; " 0<n<1 " ; " X_A = 1 - (1 + k t(n-1)c^(n-1)_(A 0))^(1/(1-s))
$
Interestingly, only for the ﬁrst-order dependence the conversion does not
depend on the initial concentration. For the zero-order dependence a point of
discontinuity is indicated, namely the time after which A has been completely
converted.

==== 3.5.1.2 Reversible Reaction
For the ﬁrst-order reversible reaction A ⇄ B, the material balance for component
A can be written as
$
    (d c_A)/(d t) = -r = -r^+ + r^- = -k^+ c_A + k^- c_B 
$
where $r^+$ , $k^+$ and $r^−$ , $k^−$ are the reaction rates and rate coeﬃcients of the forward
and reverse reaction, respectively.
The sum of the concentrations of A and B is equal to the sum of the initial
concentrations and assuming that we start with pure $A (c_(B 0) = 0)$, the total con-
centration is $c_A + c_B = c_(A 0)$ and we obtain
$
    (d c_A)/(d t) = -k^+ c_A + k^- (C_(A 0) - C_A) = k^- c_(A 0) - (k^+ + k^-)c_A
$
Integration yields
$
    c_A - k^-/(k^+ + k^-)c_(A 0) = k^+/(k^+ + k^-)c_(A 0) exp[-(k^+ + k^-)t]
$
A special case of the non-steady-state regime is equilibrium, which occurs
when $d c_A/(d t) = 0$. Rigorously speaking, this is achieved at $t -> infinity$. At equilibrium
conditions, $r^+$ = $r^−$ , so
$
    k^+ c_(A.e q) = k^- c_(B,e q)
$
where $c_(A,"eq")$ and $c_(B,"eq")$ are the equilibrium concentrations of A and B. Since
$c_(A,"eq") + c_(B,"eq") = c_(A 0)$ , it follows that
$
    c_(A,"eq") = (k^-)/(k^+ + k^-)c_(A 0) " ; " c_(B,"eq")=(k^+)/(k^+ + k^-)c_(A 0)
$
Thus eq can be written as
$
    c_A - c_(A,"eq") = c_(B,"eq") exp[-(k^+ + k^-)t]
$
or more elegantly, as
$
    c_A - c_(A,"eq") = (c_(A 0) - c_(A,"eq"))exp[-(k^+ + k^-)t]
$
Equation (3.52) implies that at any time the distance from equilibrium,
$Delta c_A = c_A − c_(A,"eq")$ , can be determined by multiplying the initial distance,
$Delta c_(A 0) = c_(A 0) − c_(A,"eq")$ , by the exponential term $exp[−(k^+ + k^−)t]$, which is similar
to that for the irreversible reaction. The latter is a special case of the reversible
reaction; $k^− = 0$ and $c_(A,"eq") = 0$ and Eq. (3.52) reduces to
$
    c_A = c_(A 0) exp(-k t)    
$
In terms of conversion can be written as
$
    X_(A,eq) - X_A = X_(A,eq) exp[-(k^+ + k^-)t]
$
or
$
    X_A = X_(A,"eq") - X_(A,"eq")exp[-(k^+ + k^-)t] = X_(A,"eq")[1 - exp(-(k^+ + k^-)t)]
$
where $X_(A,"eq") = k^+\/(k^+ k^-)$ and
$
    1/X_(A,"eq") = (k^+ + k^-)/k^+ = 1 + 1/K_"eq"
$
where $K_"eq"$ is the equilibrium coeﬃcient.
Obviously, Eq. (3.38) is a speciﬁc case of Eq. (3.55), with again $k^− = 0$, and
$X_A,"eq") = 1$. Just as for the irreversible reaction, the conversion of this reversible
reaction does not depend on the initial concentration. This is an important
characteristic for identifying ﬁrst-order reactions based on batch-reactor data.
A reversible reaction is characterized by incomplete conversion. In a batch
reactor, the maximum conversion is reached after a certain time and then remains
constant or approximately constant (Figure 3.3). In the case of a reversible reac-
tion, the vicinity of the equilibrium conversion is reached faster than the ﬁnal
conversion for the irreversible reaction (assuming that the forward rate coef-
ﬁcient is the same) because the temporal exponential change is determined by
two rate coeﬃcients, forward and reverse, that “work together.” In both the irre-
versible and reversible cases, a simple transient regime can be observed. The
concentration and conversion dependences reach an equilibrium point without
overshoot. More complicated cases will be analyzed in Chapters 7 and 8.
The reversible reaction $A arrows.rl B$ and an equilibrium point of the experimental
kinetic dependence in the batch reactor can be considered as the simplest exhibition
of kinetic complexity caused by two opposite reactions.

#image("../images/table_2_ch3.png")

==== 3.5.1.3 How to Distinguish Parallel Reactions from Consecutive Reactions
One of the ﬁrst lessons in the diﬃcult science of decoding chemical complexity
is the typical problem of parallel versus consecutive reactions (Figure 3.4).
The question is which is the right mechanism for the formation of the desired
product B. In both cases, the material balance in the batch reactor has to be ful-
ﬁlled for each of the participating components:
$
    sum_i c_i = sum_i c_(i 0) " with " i= A,B,C    
$
Assuming only A is present initially, the initial conditions are
$
    c_A = c_(A 0) " ; " c_B = 0 " ; " c_C = 0
$
The model for the parallel mechanism consists of the following set of equations:
$
    (d c_A)/(d t) &= -k_1 c_A - k_2 c_A = -(k_1 + k_2) c_A \
    (d c_B)/(d t) &= k_1 c_A \
    (d c_C)/(d t) &= k_2 c_A \
$
where $k_1$ and $k_2$ are the rate coeﬃcients of the corresponding reactions.
The solution to this model is
$
    c_A &= c_(A 0) exp[-(k_1 + k_2)t] \
    c_B &= k_1/(k_1 + k_2)c_(A 0)[1 - exp(-(k_1 + k_2)t)] \
    c_C &= k_2/(k_1 + k_2)c_(A 0)[1 - exp(-(k_1 + k_2)t)]
$
Adding the three concentrations indeed yields $c_(A 0)$ , the condition posed by
Eqs. (3.57) and (3.58). The kinetic dependences are quite simple: the concentration of A decreases
exponentially, while the concentrations of $B$ and $C$ increase exponentially
(Figure 3.5).

#image("../images/table_3_ch3.png", width: 80%)
#image("../images/table_4_ch3.png", width: 80%)

The model for the consecutive mechanism consists of the following set of
equations:
$
    (d c_A)/(d t) &= -k_1 c_A \    
    (d c_B)/(d t) &= -k_1 c_A - k_2 c_B \    
    (d c_C)/(d t) &= k_2 c_B     
$
where $k_1$ and $k_2$ are the rate coeﬃcients of the corresponding reactions.
The solution to this model is
$
    c_A &= c_(A 0) exp(-k_1,t) \
    c_B &= k_1/(k_2 - k_1) c_(A 0) [exp(-k_1 t) - exp(-k_2 t)] \
    c_C &= c_(A 0) [1 - k_2/(k_2 - k_1) exp(-k_1 t) + k_1/(k_2 - k_1)exp(-k_2 t)]
$
and again summation of the three concentrations yields cA0 . Figure 3.6 shows the
kinetic dependences for the consecutive mechanism.
There is a distinct diﬀerence between the parallel and the consecutive mech-
anism. For the consecutive mechanism, the concentration of B goes through a
maximum. This is characteristic of the consecutive mechanism.
The position of the concentration maximum can be used for estimating the rate
coeﬃcients. At the maximumi $(d c_B)/(d t) = 0$

#image("../images/table_5_ch3.png", width: 80%)

and so, taking the derivative of cB as expressed in Eq. (3.62) and setting this equal
to zero, we obtain
$
    -k_1 exp(-k_1 t_"max") + k_2 exp(-k_2 t_"max") = 0
$
where $t_"max"$ is the time at which the maximum concentration of B is reached.
The
$
    k_1/k_2 = exp[-(k_2 - k_1)t_"max"] arrow.double.long ln(k_1/k_2) = -(k_2 - k_1)t_"max"
$
and
$
    t_"max" = 1/(k_1 - k_2)ln(k_1/k_2)
$
The maximum concentration of $B$, $c_(B,"max")$ , can be found by substituting
Eq. (3.66) into Eq. (3.62):
$
    c_"B,max" = c_(A 0) k_1/(k_2 - k_1){exp[-k_1/(k_1 - k_2)ln(k_1/k_2)] - exp[-k_2 / (k_1 - k_2)ln(k_1/k_2)]} = c_"A0" (k_1/k_2)^(1/(1 - k_1\/k_2))
$
From Eqs 3.66 and 3.67 it follows that
$
    ln(c_"A0" / c_"B,max") = 1/(k_1/k_2 - 1) ln(k_1/k_2) = k_2/(k_1 - k_2) ln(k_1/k_2) = k_2 t_"max"
$
This interesting relationship obtained by Yablonsky et al. [4] is very similar to
the one for a ﬁrst-order irreversible reaction $A -> B$, Eq. (3.40). However, the obvi-
ous diﬀerence between these equations is that Eq. (3.40) is valid at any moment
in time, whereas Eq. (3.68) is only valid at the maximum.
From Eq. (3.68), the rate coeﬃcient $k_2$ can be obtained if $t_"max"$ is known. Next,
$k_1$ can be estimated based on the maximum condition $k_1 c_A , c_"B,max" = k_2 c_"B,max"$ . Thus,
the two rate coefficients for the consecutive mechanism can be determined using
the initial concentration $c_"A0"$ and the time t max at which the concentration of $B$
reaches its maximum value, $c_"B,max"$.
The consecutive mechanism has a remarkable property; simple analysis using
l’Hôpital’s rule shows that if $k_1 -> k_2$ and hence $k_2 t_"max" -> 1$:
$
    c_"A0"/c_"B,max" = e
$
Such a point may be termed a Eulerian kinetic point. A relationship of this kind was ﬁrst presented by Kubasov [5].
Obviously, the equality $k_1 = k_2$ can only be achieved under certain conditions.
Generally, the rate coeﬃcients will show a diﬀerent Arrhenius dependence:
$
    k_1 &= k_"1,0" exp(-E_"a,1"/(R_g T)) \
    k_2 &= k_"2,0" exp(-E_"a,2"/(R_g T)) \
$
It can easily be shown that the equality $k_1 = k_2$ can be achieved at a certain
temperature only if $k_"1,0" > k_"2,0"$ and $E_"a,1" > E_"a,2"$ or if $k_"1,0" < k_"2,0"$ and $E_"a,1" < E_"a,2"$.
The relationship of Eq. (3.69) can be tested experimentally for known $c_"A0"$ and
$c_"B,max"$ . Alternatively, if the experimental data yield Eq. (3.69), it follows that at
these conditions $k_1 = k_2$.
Another remarkable property of the Eulerian point follows from simple
relationships; from the expression for $(d c_B)/(d t)$ in Eqs. (3.61) and Eq. (3.63), it
follows that $k_1 c_A = k_2 c_"B,max"$ and, since $k_1 = k_2$, that $c_A = c_"B,max"$ at this point. A
detailed analysis of special points of intersections and coincidences using the
mechanism $A -> B -> C$ as an example has been made recently [4].

#pagebreak()

= 4 Chemical Book-keeping: Linear Algebra in Chemical Kinetics

== 4.1 Basic Elements of Linear Algebra
A natural language corresponding to the complexity of chemical reactions is the
language of linear algebra. Its basic concepts are described in this section.
Linear algebra is concerned with solving sets of linear equations containing
several unknowns. Such equations can conveniently be represented using the
formalism of matrices and vectors. A matrix is a rectangular array of numbers,
symbols or expressions, containing m rows and n columns. A general form of this
$(m times n)$ matrix is


#pagebreak()


ReactionStep
#table(
  columns: 2,
    [reactants],[Map string int]
)

=== Units
```OCaml
[<Measure>] type g

[<Struct>] type MolecularWeight = {MR: double<g/mol>}
[<Struct>] type HeatOfFormation = {H:  double<J/mol>}
[<Struct>] type HeatCapacity    = {Cp: double<J/mol K>}
[<Struct>] type Concentration   = {mol:double<mol/m^3>}
[<Struct>] type Volume          = {vol:double<m^3>}

[<Struct>] type Temperature = {T:double<K>}
[<Struct>] type Pressure    = {P:double<Pa>}
[<Struct>] type GasConstant = {R:double<J/mol K>}

[<Struct>] type PreExponentialFactor = {A:double}
[<Struct>] type ActivationEnergy     = {E:double<J/mol>}

// TAGS --> structs with no fields
type Reactant = struct end
type Product  = struct end
type ControlVolume = struct end
```

=== Species & Constants
```OCaml
type E' =
    | H2 = 1
    | O2 = 2
    | H2O = 3
    | C = 4
    | CO = 5
    | CO2 = 6

let MR = Map [
    E'.H2, 2.<g/mol>
    E'.O2, 32.<g/mol>
    E'.H2O, 18.<g/mol>
    E'.C, 12.<g/mol>
    E'.CO, 28.<g/mol>
    E'.CO2, 46.<g/mol>
]
```

=== Reactions
```OCaml
let reactions = [
    // 2.0**H2 ++ 1.0**O2 <=> [2.0**H2O]  // H2 + O2 = H2O
    // 2.0**C  ++ 1.0**O2 <=> [2.0**CO] 
    // 1.0**C  ++ 1.0**O2 <=> [1.0**CO2] 
    // 1.0**CO ++ 0.5**O2 <=> [1.0**CO2]

    [2.,H2; 1.,O2] <=> [2.,H2O]
    [2.,C;  1.,O2] <=> [2.,CO]
    [1.,C;  1.,O2] <=> [1.,CO2]
    [1.,CO; 0.5,O2] <=> [1.,CO2]
]
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

