// #include "template.typ"

// ****************************************************
// Use formating in main.typ and then Load the chapters
// ****************************************************
#set page(
  paper: "a4",
  numbering: "1.",
  header: align(right)[notes] + line(length: 100%)
)

#set par(justify: true)
#set heading(numbering: "1")

#set table(align: horizon + center)

#show link: set text(blue)
#show table: set align(right)

#include "kinetics.typ"
#include "reference.typ"
#include "bibliography.typ"
