module FsMathFunctions.Data.Models

open System

/// Represents a registered portal user.
[<AllowNullLiteral>]
type User() =
    member val Id          : Guid            = Guid.NewGuid() with get, set
    member val Email       : string          = ""             with get, set
    member val PasswordHash: string          = ""             with get, set
    /// "user" or "admin"
    member val Role        : string          = "user"         with get, set
    member val CreatedAt   : DateTimeOffset  = DateTimeOffset.UtcNow with get, set
    member val ApiKeys     : System.Collections.Generic.ICollection<ApiKey> =
        System.Collections.Generic.List<ApiKey>() with get, set

/// Represents a single API key belonging to a user.
/// The raw key is NEVER stored — only the SHA-256 hash.
and [<AllowNullLiteral>] ApiKey() =
    member val Id        : Guid             = Guid.NewGuid() with get, set
    member val UserId    : Guid             = Guid.Empty     with get, set
    member val User      : User             = null           with get, set
    member val Label     : string           = ""             with get, set
    /// SHA-256 hash of the raw key (hex string, lowercase).
    member val KeyHash   : string           = ""             with get, set
    /// First 8 characters of the raw key — safe to display so users can identify keys.
    member val KeyPrefix : string           = ""             with get, set
    member val CreatedAt : DateTimeOffset   = DateTimeOffset.UtcNow with get, set
    member val RevokedAt : Nullable<DateTimeOffset> = Nullable() with get, set
