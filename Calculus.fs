namespace FsMathFunctions

/// Module containing calculus functions
module Calculus =
    /// Calculate the numerical derivative of function f at point x with step h
    let derivative f x h =
        (f (x + h) - f x) / h
    
    /// Calculate the definite integral of function f from a to b using the trapezoidal rule
    /// with n subdivisions
    let integrate f a b n =
        let h = (b - a) / float n
        let sum = 
            [1..n-1] 
            |> List.sumBy (fun i -> f (a + float i * h))
        h * ((f a + f b) / 2.0 + sum)
