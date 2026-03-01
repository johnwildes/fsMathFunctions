module FsMathFunctions.Data.AppDbContext

open Microsoft.EntityFrameworkCore
open FsMathFunctions.Data.Models

type AppDbContext(options: DbContextOptions<AppDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private _users   : DbSet<User>
    [<DefaultValue>]
    val mutable private _apiKeys : DbSet<ApiKey>

    member this.Users
        with get() = this._users
        and  set v = this._users <- v

    member this.ApiKeys
        with get() = this._apiKeys
        and  set v = this._apiKeys <- v

    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(fun e ->
            e.HasKey(fun u -> u.Id :> obj) |> ignore
            e.HasIndex(fun u -> u.Email :> obj).IsUnique() |> ignore
            e.Property(fun u -> u.Email).IsRequired().HasMaxLength(320) |> ignore
            e.Property(fun u -> u.PasswordHash).IsRequired() |> ignore
            e.Property(fun u -> u.Role).IsRequired().HasDefaultValue("user") |> ignore
            e.Property(fun u -> u.CreatedAt).IsRequired() |> ignore
        ) |> ignore

        // ── ApiKey ────────────────────────────────────────────────────────────
        modelBuilder.Entity<ApiKey>(fun e ->
            e.HasKey(fun k -> k.Id :> obj) |> ignore
            e.HasIndex(fun k -> k.KeyHash :> obj).IsUnique() |> ignore
            e.Property(fun k -> k.Label).IsRequired().HasMaxLength(100) |> ignore
            e.Property(fun k -> k.KeyHash).IsRequired().HasMaxLength(64) |> ignore
            e.Property(fun k -> k.KeyPrefix).IsRequired().HasMaxLength(8) |> ignore
            e.Property(fun k -> k.CreatedAt).IsRequired() |> ignore
        ) |> ignore

        // Relationship: ApiKey → User (configured outside the lambda to satisfy F# type inference)
        modelBuilder.Entity<ApiKey>()
            .HasOne(fun (k: ApiKey) -> k.User)
            .WithMany(fun (u: User) -> u.ApiKeys :> seq<ApiKey>)
            .HasForeignKey(fun (k: ApiKey) -> k.UserId :> obj)
            .OnDelete(DeleteBehavior.Cascade) |> ignore
