module FsMathFunctions.Api.Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.OpenApi.Models
open FsMathFunctions.Api.Handlers
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open FsMathFunctions.Data.AppDbContext
open FsMathFunctions.Data

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // -----------------------------------------------------------------------
    // Services
    // -----------------------------------------------------------------------

    // Make minimal-API endpoints visible to Swagger generator
    builder.Services.AddEndpointsApiExplorer() |> ignore

    // Configure JSON to use camelCase and accept case-insensitive property names
    builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(fun (opts: Microsoft.AspNetCore.Http.Json.JsonOptions) ->
        opts.SerializerOptions.PropertyNamingPolicy <- System.Text.Json.JsonNamingPolicy.CamelCase
        opts.SerializerOptions.PropertyNameCaseInsensitive <- true
    ) |> ignore

    builder.Services.AddSwaggerGen(fun c ->
        c.SwaggerDoc(
            "v1",
            OpenApiInfo(
                Title       = "FsMathFunctions Finance API",
                Version     = "v1",
                Description =
                    "Finance calculation API built with F# and ASP.NET Core. " +
                    "**Important:** all rate fields are percent values as shown online " +
                    "(e.g. `5` means 5 %, not 0.05). The API normalises them internally."
            )
        )

        // API-key security definition (header: X-API-Key)
        c.AddSecurityDefinition(
            "ApiKey",
            OpenApiSecurityScheme(
                Description = "API key sent in the X-API-Key request header.",
                In          = ParameterLocation.Header,
                Name        = "X-API-Key",
                Type        = SecuritySchemeType.ApiKey
            )
        )

        // Apply the API-key requirement globally in the UI
        let secReq = OpenApiSecurityRequirement()
        secReq.Add(
            OpenApiSecurityScheme(
                Reference = OpenApiReference(Type = ReferenceType.SecurityScheme, Id = "ApiKey")
            ),
            Collections.Generic.List<string>()
        )
        c.AddSecurityRequirement(secReq)
    ) |> ignore

    builder.Services.AddDbContext<AppDbContext>(fun opts ->
        let cs = builder.Configuration.GetConnectionString("Default") |> Option.ofObj |> Option.defaultValue ""
        opts.UseNpgsql(cs) |> ignore
    ) |> ignore

    let app = builder.Build()

    // Ensure the database schema exists on every startup.
    // When no connection string is configured (e.g. local dev without Postgres)
    // the call is skipped so the API still starts up.
    let connStr = app.Configuration.GetConnectionString("Default") |> Option.ofObj |> Option.defaultValue ""
    if not (String.IsNullOrWhiteSpace(connStr)) then
        do
            use scope = app.Services.CreateScope()
            let db = scope.ServiceProvider.GetRequiredService<AppDbContext>()
            DatabaseInitializer.ensureCreated db

    // -----------------------------------------------------------------------
    // Middleware
    // -----------------------------------------------------------------------

    // Swagger UI is available in all environments (restrict further via API key)
    app.UseSwagger() |> ignore
    app.UseSwaggerUI(fun opts ->
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "FsMathFunctions Finance API v1")
        opts.RoutePrefix <- "swagger"
    ) |> ignore

    // API-key authentication middleware for every /api/* route.
    // Incoming keys are validated by hashing the value (SHA-256, hex) and
    // checking for a matching non-revoked row in the api_keys table.
    // When no connection string is configured the check is skipped (local dev).
    app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
        task {
            if ctx.Request.Path.StartsWithSegments(PathString("/api")) then
                if String.IsNullOrWhiteSpace(connStr) then
                    // No DB configured → allow through (local dev without Postgres)
                    do! next.Invoke(ctx)
                else
                    let found, values = ctx.Request.Headers.TryGetValue("X-API-Key")
                    if not found || values.Count = 0 then
                        ctx.Response.StatusCode  <- 401
                        ctx.Response.ContentType <- "application/json"
                        do! ctx.Response.WriteAsJsonAsync(
                            { error = { code = "UNAUTHORIZED"; message = "X-API-Key header is required"; details = [] } }
                        )
                    else
                        let incomingKey = values[0] |> Option.ofObj |> Option.defaultValue ""
                        let hashBytes   = Security.Cryptography.SHA256.HashData(Text.Encoding.UTF8.GetBytes(incomingKey))
                        let keyHash     = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                        use scope = ctx.RequestServices.CreateScope()
                        let db    = scope.ServiceProvider.GetRequiredService<AppDbContext>()
                        let valid =
                            query {
                                for k in db.ApiKeys do
                                exists (k.KeyHash = keyHash && not k.RevokedAt.HasValue)
                            }
                        if valid then
                            do! next.Invoke(ctx)
                        else
                            ctx.Response.StatusCode  <- 403
                            ctx.Response.ContentType <- "application/json"
                            do! ctx.Response.WriteAsJsonAsync(
                                { error = { code = "FORBIDDEN"; message = "Invalid API key"; details = [] } }
                            )
            else
                do! next.Invoke(ctx)
        } :> System.Threading.Tasks.Task
    ) |> ignore

    // -----------------------------------------------------------------------
    // Endpoints
    // -----------------------------------------------------------------------

    app
        .MapPost("/api/loan/payment", Func<LoanPaymentRequest, IResult>(handleLoanPayment))
        .WithName("LoanPayment")
        .WithTags("Loan")
        .WithSummary("Calculate monthly loan payment")
        .WithDescription(
            "Returns the fixed monthly payment, total amount paid, and total interest " +
            "for a fully-amortising loan. `annualRatePercent` is a percent value (5 = 5 %)."
        )
        .Produces<LoanPaymentResponse>(200)
        .Produces<ErrorResponse>(400)
    |> ignore

    app
        .MapPost("/api/mortgage/amortization", Func<MortgageAmortizationRequest, IResult>(handleMortgageAmortization))
        .WithName("MortgageAmortization")
        .WithTags("Mortgage")
        .WithSummary("Generate mortgage amortization schedule")
        .WithDescription(
            "Returns a full month-by-month amortisation schedule with totals. " +
            "`annualRatePercent` is a percent value (6.5 = 6.5 %). " +
            "Optional `extraMonthlyPayment` is applied to principal and can shorten the loan term."
        )
        .Produces<MortgageAmortizationResponse>(200)
        .Produces<ErrorResponse>(400)
    |> ignore

    app
        .MapPost("/api/investment/compound-interest", Func<CompoundInterestRequest, IResult>(handleCompoundInterest))
        .WithName("CompoundInterest")
        .WithTags("Investment")
        .WithSummary("Calculate compound interest")
        .WithDescription(
            "Returns ending balance, total contributions, interest earned, and a year-by-year " +
            "breakdown. `annualRatePercent` is a percent value (7 = 7 %). " +
            "`contributionFrequency` must be \"monthly\", \"quarterly\", or \"annually\"."
        )
        .Produces<CompoundInterestResponse>(200)
        .Produces<ErrorResponse>(400)
    |> ignore

    app.Run()
    0
