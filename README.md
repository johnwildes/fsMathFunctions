# F# Math Functions Library

A comprehensive F# library providing various mathematical functions for different domains.

## Overview

This library includes modules for:

- Basic arithmetic operations
- Calculus functions
- Statistical analysis
- Geometric calculations
- Financial calculations

## Modules

### BasicMath

Contains fundamental arithmetic operations like addition, subtraction, multiplication, division, and more.

```fsharp
// Example
open FsMathFunctions.BasicMath

let sum = add 5 3        // 8
let product = multiply 4 2  // 8
```

### Calculus

Provides numerical methods for derivatives and integrals.

```fsharp
// Example
open FsMathFunctions.Calculus

// Calculate derivative of f(x) = x^2 at x = 2 with step h = 0.001
let f x = x * x
let derivative_at_2 = derivative f 2.0 0.001  // Approximately 4

// Calculate integral of f(x) = x^2 from 0 to 1
let integral = integrate f 0.0 1.0 1000  // Approximately 0.333...
```

### Statistics

Offers functions for statistical analysis.

```fsharp
// Example
open FsMathFunctions.Statistics

let data = [1.0; 2.0; 3.0; 4.0; 5.0]
let avg = mean data  // 3.0
let med = median data  // 3.0
let stdDev = standardDeviation data  // Approximately 1.4142
```

### Geometry

Includes functions for geometric calculations.

```fsharp
// Example
open FsMathFunctions.Geometry

let area = circleArea 5.0  // Area of circle with radius 5
let dist = distance (0.0, 0.0) (3.0, 4.0)  // 5.0
```

### Finance

Provides financial calculations for banking and investment.

```fsharp
// Example
open FsMathFunctions.Finance

// Calculate monthly payment for a $200,000 loan at 3.5% interest for 30 years
let payment = monthlyPayment 200000.0 0.035 30  // Approximately $898.09

// Calculate compound interest on $1000 at 5% for 5 years, compounded quarterly
let interest = compoundInterest 1000.0 0.05 5 4  // Approximately $280.08
```

## Installation

Include the library in your F# project by referencing the appropriate assemblies.

## Version

Current version: 1.1.0

## License

[Add your license information here]
