namespace FsMathFunctions

/// Module containing statistical functions
module Statistics =
    /// Calculate the mean of a sequence of numbers
    let mean (numbers: seq<float>) =
        let sum = Seq.sum numbers
        let count = Seq.length numbers
        sum / float count
    
    /// Calculate the median of a sequence of numbers
    let median (numbers: seq<float>) =
        let sorted = numbers |> Seq.sort |> Seq.toArray
        let count = Array.length sorted
        if count = 0 then
            failwith "Cannot calculate median of empty sequence"
        elif count % 2 = 1 then
            // Odd count: return the middle element
            sorted.[count / 2]
        else
            // Even count: return the average of the two middle elements
            (sorted.[count / 2 - 1] + sorted.[count / 2]) / 2.0
    
    /// Calculate the standard deviation of a sequence of numbers
    let standardDeviation (numbers: seq<float>) =
        let avg = mean numbers
        let variance = 
            numbers 
            |> Seq.map (fun x -> pown (x - avg) 2)
            |> Seq.average
        sqrt variance
