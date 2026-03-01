namespace FsMathFunctions.Api

open System

// ---------------------------------------------------------------------------
// Loan Payment
// ---------------------------------------------------------------------------

/// Input for the loan payment calculation endpoint.
/// annualRatePercent: rate as seen online, e.g. 5 means 5 %. The API normalises
/// this to a fraction (÷ 100) internally before any calculation.
type LoanPaymentRequest =
    {
        /// Loan principal amount in dollars (must be > 0)
        principal: decimal
        /// Annual interest rate as a percent value, e.g. 5 for 5 % (must be 0–100)
        annualRatePercent: decimal
        /// Loan term in years (must be 1–50)
        termYears: int
    }

/// Response for the loan payment calculation.
/// All monetary values are rounded to 2 decimal places.
type LoanPaymentResponse =
    {
        /// Fixed monthly payment amount
        monthlyPayment: decimal
        /// Total amount paid over the life of the loan
        totalPaid: decimal
        /// Total interest paid (totalPaid - principal)
        totalInterest: decimal
    }

// ---------------------------------------------------------------------------
// Mortgage Amortization
// ---------------------------------------------------------------------------

/// Input for the mortgage amortization schedule endpoint.
type MortgageAmortizationRequest =
    {
        /// Loan principal amount in dollars (must be > 0)
        principal: decimal
        /// Annual interest rate as a percent value, e.g. 5 for 5 % (must be 0–100)
        annualRatePercent: decimal
        /// Loan term in years (must be 1–50)
        termYears: int
        /// Date of the first payment (ISO 8601, e.g. "2025-01-01")
        startDate: DateTime
        /// Optional additional monthly payment applied to principal (must be ≥ 0)
        extraMonthlyPayment: Nullable<decimal>
    }

/// One entry in the amortization schedule representing a single monthly payment.
type AmortizationEntry =
    {
        /// 1-based month index within the loan term
        month: int
        /// Payment date
        date: DateTime
        /// Total payment amount for this month
        payment: decimal
        /// Portion of the payment applied to principal
        principalPortion: decimal
        /// Portion of the payment applied to interest
        interestPortion: decimal
        /// Remaining loan balance after this payment
        remainingBalance: decimal
    }

/// Response for the mortgage amortization endpoint.
type MortgageAmortizationResponse =
    {
        /// Month-by-month payment schedule
        schedule: AmortizationEntry list
        /// Sum of all payments made
        totalPayments: decimal
        /// Total principal repaid
        totalPrincipal: decimal
        /// Total interest paid
        totalInterest: decimal
    }

// ---------------------------------------------------------------------------
// Compound Interest
// ---------------------------------------------------------------------------

/// Input for the compound-interest calculation endpoint.
type CompoundInterestRequest =
    {
        /// Starting investment amount in dollars (must be ≥ 0)
        initialPrincipal: decimal
        /// Annual interest rate as a percent value, e.g. 7 for 7 % (must be 0–100)
        annualRatePercent: decimal
        /// Investment duration in years (must be 1–100)
        years: int
        /// Number of times interest is compounded per year, e.g. 12 for monthly (must be 1–365)
        compoundsPerYear: int
        /// Optional regular contribution per period (must be ≥ 0 if provided)
        periodicContribution: Nullable<decimal>
        /// Frequency of contributions: "monthly", "quarterly", or "annually" (default: "monthly")
        contributionFrequency: string
    }

/// Year-by-year breakdown entry for compound-interest calculation.
type YearlyBreakdown =
    {
        /// Calendar year number within the investment (1-based)
        year: int
        /// Account balance at end of this year
        balance: decimal
        /// Interest earned during this year
        interestEarned: decimal
        /// Contributions made during this year
        contributions: decimal
    }

/// Response for the compound-interest calculation.
type CompoundInterestResponse =
    {
        /// Final balance at the end of the investment period
        endingBalance: decimal
        /// Total contributions made (excluding initial principal)
        totalContributions: decimal
        /// Total interest earned over the investment period
        interestEarned: decimal
        /// Year-by-year growth breakdown
        yearlyBreakdown: YearlyBreakdown list
    }

// ---------------------------------------------------------------------------
// Error types
// ---------------------------------------------------------------------------

/// Details for a structured error response.
type ErrorDetail =
    {
        /// Machine-readable error code, e.g. "VALIDATION_ERROR"
        code: string
        /// Human-readable summary
        message: string
        /// List of specific validation or error messages
        details: string list
    }

/// Wrapper returned on HTTP 400/401/403 responses.
type ErrorResponse = { error: ErrorDetail }
