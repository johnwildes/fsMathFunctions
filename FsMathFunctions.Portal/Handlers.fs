module FsMathFunctions.Portal.Handlers

open System
open System.Linq
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authorization
open Microsoft.EntityFrameworkCore
open FsMathFunctions.Data.AppDbContext
open FsMathFunctions.Data.Models
open FsMathFunctions.Portal.Models
open FsMathFunctions.Portal.Auth

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private badRequest (code: string) (message: string) (details: ErrorDetail list) =
    Results.Json(
        { error = { code = code; message = message; details = details } },
        statusCode = 400
    )

let private unauthorized (message: string) =
    Results.Json(
        { error = { code = "UNAUTHORIZED"; message = message; details = [] } },
        statusCode = 401
    )

let private forbidden (message: string) =
    Results.Json(
        { error = { code = "FORBIDDEN"; message = message; details = [] } },
        statusCode = 403
    )

let private notFound (message: string) =
    Results.Json(
        { error = { code = "NOT_FOUND"; message = message; details = [] } },
        statusCode = 404
    )

// ---------------------------------------------------------------------------
// POST /auth/register
// ---------------------------------------------------------------------------

let handleRegister (db: AppDbContext) (jwtCfg: JwtConfig) (req: RegisterRequest) =
    task {
        // Basic validation
        let details =
            [ if String.IsNullOrWhiteSpace(req.email) || not (req.email.Contains('@')) then
                yield { field = "email"; message = "A valid email address is required." }
              if String.IsNullOrWhiteSpace(req.password) || req.password.Length < 8 then
                yield { field = "password"; message = "Password must be at least 8 characters." } ]

        if not (List.isEmpty details) then
            return badRequest "VALIDATION_ERROR" "Invalid registration data." details
        else

        let email = req.email.Trim().ToLowerInvariant()
        let! existing = db.Users.AnyAsync(fun u -> u.Email = email)
        if existing then
            return badRequest "CONFLICT" "An account with that email already exists." []
        else

        let user =
            User(
                Id           = Guid.NewGuid(),
                Email        = email,
                PasswordHash = hashPassword req.password,
                Role         = "user",
                CreatedAt    = DateTimeOffset.UtcNow
            )

        db.Users.Add(user) |> ignore
        let! _ = db.SaveChangesAsync()

        let token, expiresAt = createToken jwtCfg user.Id user.Email user.Role
        return Results.Created("/api/keys", { token = token; expiresAt = expiresAt } :> obj)
    }

// ---------------------------------------------------------------------------
// POST /auth/login
// ---------------------------------------------------------------------------

let handleLogin (db: AppDbContext) (jwtCfg: JwtConfig) (req: LoginRequest) =
    task {
        let email = (req.email |> Option.ofObj |> Option.defaultValue "").Trim().ToLowerInvariant()
        let! user = db.Users.FirstOrDefaultAsync(fun u -> u.Email = email)

        // Constant-time-safe: verify even if user is null so timing is identical
        let dummyHash = "$2a$12$aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        let hash      = if isNull user then dummyHash else user.PasswordHash
        let valid     = verifyPassword req.password hash

        if isNull user || not valid then
            return unauthorized "Invalid email or password."
        else

        let token, expiresAt = createToken jwtCfg user.Id user.Email user.Role
        return Results.Ok({ token = token; expiresAt = expiresAt } :> obj)
    }

// ---------------------------------------------------------------------------
// GET /api/keys
// ---------------------------------------------------------------------------

[<Authorize>]
let handleListKeys (db: AppDbContext) (ctx: HttpContext) =
    task {
        match getUserId ctx.User with
        | None -> return unauthorized "Invalid token."
        | Some userId ->
            let! keys =
                db.ApiKeys
                    .Where(fun k -> k.UserId = userId)
                    .OrderByDescending(fun k -> k.CreatedAt)
                    .ToListAsync()
            let dtos =
                keys |> Seq.map (fun k ->
                    { id        = k.Id
                      prefix    = k.KeyPrefix
                      label     = k.Label
                      createdAt = k.CreatedAt
                      revokedAt = k.RevokedAt |> Option.ofNullable })
                |> Seq.toList
            return Results.Ok(dtos :> obj)
    }

// ---------------------------------------------------------------------------
// POST /api/keys
// ---------------------------------------------------------------------------

[<Authorize>]
let handleCreateKey (db: AppDbContext) (ctx: HttpContext) (req: CreateKeyRequest) =
    task {
        match getUserId ctx.User with
        | None -> return unauthorized "Invalid token."
        | Some userId ->

        if String.IsNullOrWhiteSpace(req.label) then
            return badRequest "VALIDATION_ERROR" "Label is required." [ { field = "label"; message = "Label cannot be empty." } ]
        else

        let rawKey = generateRawKey ()
        let apiKey =
            ApiKey(
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Label     = req.label.Trim(),
                KeyHash   = hashApiKey rawKey,
                KeyPrefix = rawKey.[..7],
                CreatedAt = DateTimeOffset.UtcNow
            )

        db.ApiKeys.Add(apiKey) |> ignore
        let! _ = db.SaveChangesAsync()

        return Results.Created(
            $"/api/keys/{apiKey.Id}",
            { id        = apiKey.Id
              rawKey    = rawKey
              prefix    = apiKey.KeyPrefix
              label     = apiKey.Label
              createdAt = apiKey.CreatedAt } :> obj
        )
    }

// ---------------------------------------------------------------------------
// DELETE /api/keys/{id}
// ---------------------------------------------------------------------------

[<Authorize>]
let handleRevokeKey (db: AppDbContext) (ctx: HttpContext) (id: Guid) =
    task {
        match getUserId ctx.User with
        | None -> return unauthorized "Invalid token."
        | Some userId ->

        let! key = db.ApiKeys.FirstOrDefaultAsync(fun k -> k.Id = id)
        if isNull key then
            return notFound "API key not found."
        elif key.UserId <> userId then
            return forbidden "You do not own this key."
        elif key.RevokedAt.HasValue then
            return badRequest "ALREADY_REVOKED" "This key has already been revoked." []
        else

        key.RevokedAt <- Nullable(DateTimeOffset.UtcNow)
        let! _ = db.SaveChangesAsync()
        return Results.NoContent()
    }

// ---------------------------------------------------------------------------
// GET /admin/users
// ---------------------------------------------------------------------------

[<Authorize(Roles = "admin")>]
let handleListUsers (db: AppDbContext) =
    task {
        let! users = db.Users.Include(fun u -> u.ApiKeys).OrderBy(fun u -> u.CreatedAt).ToListAsync()
        let dtos =
            users |> Seq.map (fun u ->
                { id        = u.Id
                  email     = u.Email
                  role      = u.Role
                  keyCount  = u.ApiKeys |> Seq.filter (fun k -> not k.RevokedAt.HasValue) |> Seq.length
                  createdAt = u.CreatedAt })
            |> Seq.toList
        return Results.Ok(dtos :> obj)
    }

// ---------------------------------------------------------------------------
// DELETE /admin/users/{id}
// ---------------------------------------------------------------------------

[<Authorize(Roles = "admin")>]
let handleDeleteUser (db: AppDbContext) (ctx: HttpContext) (id: Guid) =
    task {
        let! user = db.Users.FirstOrDefaultAsync(fun u -> u.Id = id)
        if isNull user then
            return notFound "User not found."
        else

        // Prevent self-deletion
        match getUserId ctx.User with
        | Some callerId when callerId = id ->
            return badRequest "SELF_DELETE" "You cannot delete your own account." []
        | _ ->

        db.Users.Remove(user) |> ignore
        let! _ = db.SaveChangesAsync()
        return Results.NoContent()
    }
