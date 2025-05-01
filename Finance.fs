namespace FsMathFunctions

/// Module containing financial calculations for banking and investment
module Finance =
    /// Calculate simple interest
    /// principal: initial amount of money
    /// rate: annual interest rate (as a decimal, e.g., 0.05 for 5%)
    /// time: time in years
    let simpleInterest principal rate time =
        principal * rate * time
        
    /// Calculate compound interest
    /// principal: initial amount of money
    /// rate: annual interest rate (as a decimal, e.g., 0.05 for 5%)
    /// time: time in years
    /// compounds: number of times interest is compounded per year
    let compoundInterest principal rate time compounds =
        principal * (pown (1.0 + rate / float compounds) (compounds * time) - 1.0)
        
    /// Calculate future value of an investment with compound interest
    /// principal: initial amount of money
    /// rate: annual interest rate (as a decimal, e.g., 0.05 for 5%)
    /// time: time in years
    /// compounds: number of times interest is compounded per year
    let futureValue principal rate time compounds =
        principal * pown (1.0 + rate / float compounds) (compounds * time)
        
    /// Calculate present value (the current value of a future sum of money)
    /// futureAmount: future sum of money
    /// rate: annual interest rate (as a decimal, e.g., 0.05 for 5%)
    /// time: time in years
    /// compounds: number of times interest is compounded per year
    let presentValue futureAmount rate time compounds =
        futureAmount / pown (1.0 + rate / float compounds) (compounds * time)
        
    /// Calculate monthly loan payment (for mortgage or other loans)
    /// principal: loan amount
    /// rate: annual interest rate (as a decimal, e.g., 0.05 for 5%)
    /// years: loan term in years
    let monthlyPayment principal rate years =
        let monthlyRate = rate / 12.0
        let months = years * 12
        let factor = pown (1.0 + monthlyRate) months
        principal * monthlyRate * factor / (factor - 1.0)
        
    /// Calculate the annual percentage yield (APY)
    /// rate: nominal interest rate (as a decimal, e.g., 0.05 for 5%)
    /// compounds: number of times interest is compounded per year
    let annualPercentageYield rate compounds =
        pown (1.0 + rate / float compounds) compounds - 1.0
        
    /// Calculate the return on investment (ROI)
    /// gain: the profit from the investment
    /// cost: the cost of the investment
    let returnOnInvestment gain cost =
        gain / cost
        
    /// Calculate straight-line depreciation
    /// cost: initial cost of the asset
    /// salvage: salvage value at the end of its useful life
    /// life: useful life of the asset in years
    let straightLineDepreciation cost salvage life =
        (cost - salvage) / float life
        
    /// Calculate the net present value (NPV) of a series of cash flows
    /// rate: discount rate per period (as a decimal)
    /// cashFlows: sequence of cash flows (negative for outflows, positive for inflows)
    let netPresentValue rate (cashFlows: seq<float>) =
        cashFlows
        |> Seq.mapi (fun i cf -> cf / pown (1.0 + rate) (i + 1))
        |> Seq.sum
        
    /// Calculate internal rate of return (IRR) using a simple iterative approach
    /// cashFlows: sequence of cash flows (first one is typically negative, representing initial investment)
    /// tolerance: acceptable error in the calculation
    /// maxIterations: maximum number of iterations to try
    let internalRateOfReturn (cashFlows: seq<float>) (tolerance: float) (maxIterations: int) =
        let rec tryRate rate iteration =
            if iteration >= maxIterations then
                None
            else
                let npv = netPresentValue rate cashFlows
                if abs npv < tolerance then
                    Some rate
                else
                    // Adjust rate based on NPV sign
                    let newRate = 
                        if npv > 0.0 then rate + 0.01 else rate - 0.01
                    tryRate newRate (iteration + 1)
                    
        // Start with a reasonable guess (10%)
        tryRate 0.1 0
