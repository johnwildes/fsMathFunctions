namespace FsMathFunctions.Api

open System
open Microsoft.AspNetCore.Http

/// HTTP handlers for every finance API endpoint.
/// Each handler validates its inputs first (early-return on error) and then
/// delegates to FinanceCalculations for the pure maths.
module Handlers =

    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    /// Build a structured HTTP 400 Bad Request result.
    let private badRequest (code: string) (message: string) (details: string list) : IResult =
        Results.BadRequest(
            { error = { code = code; message = message; details = details } }
        )

    // -----------------------------------------------------------------------
    // POST /api/loan/payment
    // -----------------------------------------------------------------------

    /// Calculate the fixed monthly payment, total paid, and total interest
    /// for a fully-amortising loan.
    ///
    /// Rates are accepted as percent values (e.g. annualRatePercent = 5 means 5 %).
    /// The handler normalises them to fractions before calling FinanceCalculations.
    ///
    /// Validation rules:
    ///   - principal > 0
    ///   - 0 ≤ annualRatePercent ≤ 100
    ///   - 1 ≤ termYears ≤ 50
    ///
    /// Example request body:
    ///   { "principal": 200000, "annualRatePercent": 5, "termYears": 30 }
    let handleLoanPayment (req: LoanPaymentRequest) : IResult =
        // --- Input validation ---
        let errors = ResizeArray<string>()
        if req.principal <= 0m then
            errors.Add("principal must be greater than 0")
        if req.annualRatePercent < 0m then
            errors.Add("annualRatePercent must be non-negative")
        if req.annualRatePercent > 100m then
            errors.Add("annualRatePercent must be at most 100")
        if req.termYears <= 0 then
            errors.Add("termYears must be greater than 0")
        if req.termYears > 50 then
            errors.Add("termYears must be at most 50")

        if errors.Count > 0 then
            badRequest "VALIDATION_ERROR" "Invalid request parameters" (List.ofSeq errors)
        else
            // --- Normalise rate from percent to fraction (5 → 0.05) ---
            let monthlyRate = req.annualRatePercent / 100m / 12m
            let months      = req.termYears * 12

            let monthlyPayment = FinanceCalculations.calculateMonthlyPayment req.principal monthlyRate months
            let totalPaid      = Math.Round(monthlyPayment * decimal months, 2)
            let totalInterest  = Math.Round(totalPaid - req.principal, 2)

            Results.Ok(
                {
                    monthlyPayment = monthlyPayment
                    totalPaid      = totalPaid
                    totalInterest  = totalInterest
                }
            )

    // -----------------------------------------------------------------------
    // POST /api/mortgage/amortization
    // -----------------------------------------------------------------------

    /// Generate a full month-by-month amortisation schedule for a mortgage.
    ///
    /// Rates are accepted as percent values (e.g. annualRatePercent = 6.5 means 6.5 %).
    /// An optional extraMonthlyPayment accelerates principal reduction and may shorten
    /// the effective term.
    ///
    /// Validation rules:
    ///   - principal > 0
    ///   - 0 ≤ annualRatePercent ≤ 100
    ///   - 1 ≤ termYears ≤ 50
    ///   - extraMonthlyPayment ≥ 0 (if provided)
    ///
    /// Example request body:
    ///   { "principal": 300000, "annualRatePercent": 6.5, "termYears": 30,
    ///     "startDate": "2025-01-01", "extraMonthlyPayment": 200 }
    let handleMortgageAmortization (req: MortgageAmortizationRequest) : IResult =
        let errors = ResizeArray<string>()
        if req.principal <= 0m then
            errors.Add("principal must be greater than 0")
        if req.annualRatePercent < 0m then
            errors.Add("annualRatePercent must be non-negative")
        if req.annualRatePercent > 100m then
            errors.Add("annualRatePercent must be at most 100")
        if req.termYears <= 0 then
            errors.Add("termYears must be greater than 0")
        if req.termYears > 50 then
            errors.Add("termYears must be at most 50")
        if req.extraMonthlyPayment.HasValue && req.extraMonthlyPayment.Value < 0m then
            errors.Add("extraMonthlyPayment must be non-negative")

        if errors.Count > 0 then
            badRequest "VALIDATION_ERROR" "Invalid request parameters" (List.ofSeq errors)
        else
            let monthlyRate = req.annualRatePercent / 100m / 12m
            let months      = req.termYears * 12
            let extra       =
                if req.extraMonthlyPayment.HasValue then req.extraMonthlyPayment.Value
                else 0m

            let schedule =
                FinanceCalculations.generateAmortizationSchedule
                    req.principal monthlyRate months req.startDate extra

            let totalPayments  = schedule |> List.sumBy (fun e -> e.payment)
            let totalPrincipal = schedule |> List.sumBy (fun e -> e.principalPortion)
            let totalInterest  = schedule |> List.sumBy (fun e -> e.interestPortion)

            Results.Ok(
                {
                    schedule       = schedule
                    totalPayments  = Math.Round(totalPayments,  2)
                    totalPrincipal = Math.Round(totalPrincipal, 2)
                    totalInterest  = Math.Round(totalInterest,  2)
                }
            )

    // -----------------------------------------------------------------------
    // POST /api/investment/compound-interest
    // -----------------------------------------------------------------------

    /// Calculate compound interest with optional periodic contributions.
    ///
    /// Rates are accepted as percent values (e.g. annualRatePercent = 7 means 7 %).
    /// contributionFrequency must be one of "monthly", "quarterly", or "annually"
    /// (ignored when periodicContribution is absent or 0).
    ///
    /// Validation rules:
    ///   - initialPrincipal ≥ 0
    ///   - 0 ≤ annualRatePercent ≤ 100
    ///   - 1 ≤ years ≤ 100
    ///   - 1 ≤ compoundsPerYear ≤ 365
    ///   - periodicContribution ≥ 0 (if provided)
    ///
    /// Example request body:
    ///   { "initialPrincipal": 10000, "annualRatePercent": 7, "years": 10,
    ///     "compoundsPerYear": 12, "periodicContribution": 200,
    ///     "contributionFrequency": "monthly" }
    let handleCompoundInterest (req: CompoundInterestRequest) : IResult =
        let errors = ResizeArray<string>()
        if req.initialPrincipal < 0m then
            errors.Add("initialPrincipal must be non-negative")
        if req.annualRatePercent < 0m then
            errors.Add("annualRatePercent must be non-negative")
        if req.annualRatePercent > 100m then
            errors.Add("annualRatePercent must be at most 100")
        if req.years <= 0 then
            errors.Add("years must be greater than 0")
        if req.years > 100 then
            errors.Add("years must be at most 100")
        if req.compoundsPerYear <= 0 then
            errors.Add("compoundsPerYear must be greater than 0")
        if req.compoundsPerYear > 365 then
            errors.Add("compoundsPerYear must be at most 365")
        if req.periodicContribution.HasValue && req.periodicContribution.Value < 0m then
            errors.Add("periodicContribution must be non-negative")

        if errors.Count > 0 then
            badRequest "VALIDATION_ERROR" "Invalid request parameters" (List.ofSeq errors)
        else
            let annualRate   = req.annualRatePercent / 100m
            let contribution =
                if req.periodicContribution.HasValue then req.periodicContribution.Value
                else 0m

            // Map contributionFrequency string to times-per-year; default to monthly
            let contributionsPerYear =
                if contribution > 0m then
                    match req.contributionFrequency with
                    | "quarterly" -> 4
                    | "annually"  -> 1
                    | _           -> 12  // "monthly" or anything else → monthly
                else
                    0

            let endingBalance, totalContributions, breakdown =
                FinanceCalculations.calculateCompoundInterest
                    req.initialPrincipal annualRate req.years req.compoundsPerYear
                    contribution contributionsPerYear

            let interestEarned =
                Math.Round(endingBalance - req.initialPrincipal - totalContributions, 2)

            Results.Ok(
                {
                    endingBalance      = endingBalance
                    totalContributions = totalContributions
                    interestEarned     = interestEarned
                    yearlyBreakdown    = breakdown
                }
            )
