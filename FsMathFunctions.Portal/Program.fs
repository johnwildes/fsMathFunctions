module FsMathFunctions.Portal.Program

open System
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.OpenApi.Models
open FsMathFunctions.Data.AppDbContext
open FsMathFunctions.Data
open FsMathFunctions.Portal.Auth
open FsMathFunctions.Portal.Handlers
open FsMathFunctions.Portal.Models

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // -----------------------------------------------------------------------
    // Services
    // -----------------------------------------------------------------------

    let cfg = builder.Configuration

    // PostgreSQL
    let connStr = cfg.GetConnectionString("Default") |> Option.ofObj |> Option.defaultValue ""
    if not (String.IsNullOrWhiteSpace(connStr)) then
        builder.Services.AddDbContext<AppDbContext>(fun opts ->
            opts.UseNpgsql(connStr) |> ignore
        ) |> ignore

    // JWT config
    let jwtCfg : JwtConfig =
        { secret      = cfg.["JWT__Secret"]      |> Option.ofObj |> Option.defaultValue "dev-secret-please-change"
          issuer      = cfg.["JWT__Issuer"]      |> Option.ofObj |> Option.defaultValue "fsmathfunctions-portal"
          audience    = cfg.["JWT__Audience"]    |> Option.ofObj |> Option.defaultValue "fsmathfunctions-portal"
          expiryHours = cfg.["JWT__ExpiryHours"] |> Option.ofObj |> Option.bind (fun s -> match Int32.TryParse(s) with true, n -> Some n | _ -> None) |> Option.defaultValue 24 }

    builder.Services.AddSingleton(jwtCfg) |> ignore

    // JWT authentication
    builder.Services
        .AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", fun opts ->
            opts.TokenValidationParameters <-
                TokenValidationParameters(
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtCfg.issuer,
                    ValidAudience            = jwtCfg.audience,
                    IssuerSigningKey         = SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg.secret))
                )
        ) |> ignore

    builder.Services.AddAuthorization() |> ignore

    // Swagger
    builder.Services.AddEndpointsApiExplorer() |> ignore
    builder.Services.AddSwaggerGen(fun c ->
        c.SwaggerDoc("v1", OpenApiInfo(Title = "FsMathFunctions Portal API", Version = "v1"))

        let bearerScheme =
            OpenApiSecurityScheme(
                Description  = "JWT Bearer token. Enter: Bearer {token}",
                Name         = "Authorization",
                In           = ParameterLocation.Header,
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT"
            )
        c.AddSecurityDefinition("Bearer", bearerScheme)

        let secReq = OpenApiSecurityRequirement()
        secReq.Add(
            OpenApiSecurityScheme(Reference = OpenApiReference(Type = ReferenceType.SecurityScheme, Id = "Bearer")),
            Collections.Generic.List<string>()
        )
        c.AddSecurityRequirement(secReq)
    ) |> ignore

    // CORS — allow Portal UI origin (configured via CORS__Origins env var, comma-separated)
    builder.Services.AddCors(fun opts ->
        opts.AddDefaultPolicy(fun policy ->
            let origins =
                cfg.["CORS__Origins"]
                |> Option.ofObj
                |> Option.map (fun s -> s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                |> Option.defaultValue [| "http://localhost:3000" |]
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod() |> ignore
        )
    ) |> ignore

    let app = builder.Build()

    // -----------------------------------------------------------------------
    // DB init
    // -----------------------------------------------------------------------

    if not (String.IsNullOrWhiteSpace(connStr)) then
        do
            use scope = app.Services.CreateScope()
            let db = scope.ServiceProvider.GetRequiredService<AppDbContext>()
            DatabaseInitializer.ensureCreated db

    // -----------------------------------------------------------------------
    // Middleware
    // -----------------------------------------------------------------------

    app.UseSwagger() |> ignore
    app.UseSwaggerUI(fun opts ->
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Portal API v1")
        opts.RoutePrefix <- "swagger"
    ) |> ignore

    app.UseCors()           |> ignore
    app.UseAuthentication() |> ignore
    app.UseAuthorization()  |> ignore

    // -----------------------------------------------------------------------
    // Endpoints
    // -----------------------------------------------------------------------

    // Auth (no JWT required)
    app.MapPost("/auth/register",
        Func<AppDbContext, JwtConfig, RegisterRequest, _>(
            fun db jwtCfg req -> handleRegister db jwtCfg req
        )).AllowAnonymous() |> ignore

    app.MapPost("/auth/login",
        Func<AppDbContext, JwtConfig, LoginRequest, _>(
            fun db jwtCfg req -> handleLogin db jwtCfg req
        )).AllowAnonymous() |> ignore

    // Keys (JWT required)
    app.MapGet("/api/keys",
        Func<AppDbContext, HttpContext, _>(
            fun db ctx -> handleListKeys db ctx
        )).RequireAuthorization() |> ignore

    app.MapPost("/api/keys",
        Func<AppDbContext, HttpContext, CreateKeyRequest, _>(
            fun db ctx req -> handleCreateKey db ctx req
        )).RequireAuthorization() |> ignore

    app.MapDelete("/api/keys/{id}",
        Func<AppDbContext, HttpContext, Guid, _>(
            fun db ctx id -> handleRevokeKey db ctx id
        )).RequireAuthorization() |> ignore

    // Admin (JWT + admin role required)
    app.MapGet("/admin/users",
        Func<AppDbContext, _>(
            fun db -> handleListUsers db
        )).RequireAuthorization() |> ignore

    app.MapDelete("/admin/users/{id}",
        Func<AppDbContext, HttpContext, Guid, _>(
            fun db ctx id -> handleDeleteUser db ctx id
        )).RequireAuthorization() |> ignore

    app.Run()
    0

