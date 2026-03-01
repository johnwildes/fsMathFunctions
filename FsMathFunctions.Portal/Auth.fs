module FsMathFunctions.Portal.Auth

open System
open System.Security.Claims
open System.Security.Cryptography
open System.Text
open Microsoft.IdentityModel.Tokens
open System.IdentityModel.Tokens.Jwt

// ---------------------------------------------------------------------------
// Password hashing (BCrypt)
// ---------------------------------------------------------------------------

let hashPassword (plaintext: string) : string =
    BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor = 12)

let verifyPassword (plaintext: string) (hash: string) : bool =
    BCrypt.Net.BCrypt.Verify(plaintext, hash)

// ---------------------------------------------------------------------------
// API key utilities
// ---------------------------------------------------------------------------

/// Generates a cryptographically random 32-byte URL-safe base64 key (43 chars).
let generateRawKey () : string =
    let bytes = RandomNumberGenerator.GetBytes(32)
    Convert.ToBase64String(bytes)
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=')

/// Returns the lowercase hex SHA-256 hash of a key string.
let hashApiKey (rawKey: string) : string =
    let bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))
    BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()

// ---------------------------------------------------------------------------
// JWT
// ---------------------------------------------------------------------------

type JwtConfig =
    { secret      : string
      issuer      : string
      audience    : string
      expiryHours : int }

let createToken (cfg: JwtConfig) (userId: Guid) (email: string) (role: string) : string * DateTimeOffset =
    let key     = SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.secret))
    let creds   = SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    let expires = DateTimeOffset.UtcNow.AddHours(float cfg.expiryHours)

    let claims =
        [| Claim(JwtRegisteredClaimNames.Sub,   userId.ToString())
           Claim(JwtRegisteredClaimNames.Email, email)
           Claim(ClaimTypes.Role,               role)
           Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()) |]

    let token =
        JwtSecurityToken(
            issuer             = cfg.issuer,
            audience           = cfg.audience,
            claims             = claims,
            expires            = expires.UtcDateTime,
            signingCredentials = creds
        )

    JwtSecurityTokenHandler().WriteToken(token), expires

/// Extracts the user id (sub claim) from a validated ClaimsPrincipal.
let getUserId (principal: ClaimsPrincipal) : Guid option =
    principal.FindFirst(JwtRegisteredClaimNames.Sub)
    |> Option.ofObj
    |> Option.bind (fun c ->
        match Guid.TryParse(c.Value) with
        | true, g -> Some g
        | _       -> None)
