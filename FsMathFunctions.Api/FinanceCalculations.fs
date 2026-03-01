namespace FsMathFunctions.Api

open System

/// Pure decimal-precision financial calculations used by the API handlers.
/// All rate parameters in this module are already normalised fractions
/// (e.g. 0.05 for 5 %). The HTTP handlers are responsible for converting
/// the client-supplied percent values (e.g. 5) to fractions before calling
/// these functions.
///
/// Rounding strategy: all monetary results are rounded to 2 decimal places
/// (banker's rounding via Math.Round with MidpointRounding.ToEven default).
module FinanceCalculations =

    // -----------------------------------------------------------------------
    // Loan payment
    // -----------------------------------------------------------------------

    /// Calculate the fixed monthly payment for a fully-amortising loan.
    ///
    /// Uses the standard amortisation formula:
    ///   M = P * r * (1 + r)^n / ((1 + r)^n - 1)
    /// where P = principal, r = monthly rate, n = number of monthly periods.
    /// When r = 0 (interest-free loan) the payment is simply P / n.
    ///
    /// principal   : loan amount in dollars           (e.g. 200_000m)
    /// monthlyRate : monthly rate as a fraction       (e.g. 0.00416667m for 5 % p.a.)
    /// months      : total number of monthly payments (e.g. 360 for 30 years)
    ///
    /// Example:
    ///   calculateMonthlyPayment 200_000m (0.05m / 12m) 360
    ///   // returns ≈ 1_073.64m
    let calculateMonthlyPayment (principal: decimal) (monthlyRate: decimal) (months: int) : decimal =
        if months <= 0 then
            0m
        elif monthlyRate = 0m then
            Math.Round(principal / decimal months, 2)
        else
            let r = float monthlyRate
            let n = float months
            let p = float principal
            let factor = Math.Pow(1.0 + r, n)
            Math.Round(decimal (p * r * factor / (factor - 1.0)), 2)

    // -----------------------------------------------------------------------
    // Amortisation schedule
    // -----------------------------------------------------------------------

    /// Generate a full month-by-month amortisation schedule for a loan.
    ///
    /// Extra principal payments reduce the outstanding balance faster and can
    /// shorten the effective loan term (the schedule stops early when the
    /// balance reaches zero).
    ///
    /// principal          : initial loan balance
    /// monthlyRate        : monthly rate as a fraction
    /// months             : contractual number of monthly periods
    /// startDate          : date assigned to the first payment
    /// extraMonthlyPayment: additional amount applied to principal each month
    ///
    /// Returns a list of AmortizationEntry records in chronological order.
    ///
    /// Example:
    ///   generateAmortizationSchedule 300_000m (0.065m / 12m) 360 (DateTime(2025,1,1)) 0m
    ///   // returns 360 entries for a 30-year mortgage
    let generateAmortizationSchedule
        (principal: decimal)
        (monthlyRate: decimal)
        (months: int)
        (startDate: DateTime)
        (extraMonthlyPayment: decimal)
        : AmortizationEntry list =

        let basePayment = calculateMonthlyPayment principal monthlyRate months

        let rec build (balance: decimal) (month: int) (acc: AmortizationEntry list) =
            if month > months || balance <= 0m then
                List.rev acc
            else
                let interestPortion   = Math.Round(balance * monthlyRate, 2)
                // Principal paid = base payment − interest + any extra; cap at remaining balance
                let rawPrincipal      = basePayment - interestPortion + extraMonthlyPayment
                let principalPortion  = min rawPrincipal balance
                let actualPayment     = principalPortion + interestPortion
                let newBalance        = Math.Round(balance - principalPortion, 2)
                let entry =
                    {
                        month            = month
                        date             = startDate.AddMonths(month - 1)
                        payment          = Math.Round(actualPayment, 2)
                        principalPortion = Math.Round(principalPortion, 2)
                        interestPortion  = interestPortion
                        remainingBalance = max 0m newBalance
                    }
                build (max 0m newBalance) (month + 1) (entry :: acc)

        build principal 1 []

    // -----------------------------------------------------------------------
    // Compound interest
    // -----------------------------------------------------------------------

    /// Calculate compound interest with optional periodic contributions.
    ///
    /// Interest is applied each compounding period (ratePerPeriod = rate / compoundsPerYear).
    /// Contributions are distributed uniformly across compounding periods within each year
    /// (annualContribution = periodicContribution × contributionsPerYear, then divided equally
    /// across compoundsPerYear periods). This is a standard approximation used when the
    /// contribution frequency differs from the compounding frequency.
    ///
    /// principal           : initial investment in dollars
    /// rate                : annual rate as a fraction             (e.g. 0.07m for 7 %)
    /// years               : investment duration in years
    /// compoundsPerYear    : compounding periods per year          (e.g. 12 for monthly)
    /// periodicContribution: amount added each contribution period (e.g. 200m per month)
    /// contributionsPerYear: how many times per year a contribution is made (0 = none)
    ///
    /// Returns: (endingBalance, totalContributions, yearlyBreakdown list)
    ///
    /// Example:
    ///   calculateCompoundInterest 10_000m 0.07m 10 12 200m 12
    ///   // ≈ (endingBalance: 54_457m, totalContributions: 24_000m, ...)
    let calculateCompoundInterest
        (principal: decimal)
        (rate: decimal)
        (years: int)
        (compoundsPerYear: int)
        (periodicContribution: decimal)
        (contributionsPerYear: int)
        : decimal * decimal * YearlyBreakdown list =

        let ratePerPeriod = rate / decimal compoundsPerYear
        // Annual contribution total (0 if no contributions requested)
        let annualContrib =
            if contributionsPerYear > 0 then
                periodicContribution * decimal contributionsPerYear
            else
                0m
        // Amount added per compounding period (spread evenly)
        let contribPerPeriod =
            if compoundsPerYear > 0 && annualContrib > 0m then
                annualContrib / decimal compoundsPerYear
            else
                0m

        let rec buildYearly
            (balance: decimal)
            (year: int)
            (totalContribs: decimal)
            (acc: YearlyBreakdown list)
            =
            if year > years then
                (Math.Round(balance, 2), totalContribs, List.rev acc)
            else
                let startBalance = balance
                let mutable b = balance
                for _ in 1 .. compoundsPerYear do
                    b <- b + b * ratePerPeriod + contribPerPeriod

                let endBalance        = b
                let yearContribs      = annualContrib
                let interestThisYear  = Math.Round(endBalance - startBalance - yearContribs, 2)
                let entry =
                    {
                        year          = year
                        balance       = Math.Round(endBalance, 2)
                        interestEarned = interestThisYear
                        contributions = yearContribs
                    }
                buildYearly endBalance (year + 1) (totalContribs + yearContribs) (entry :: acc)

        buildYearly principal 1 0m []
