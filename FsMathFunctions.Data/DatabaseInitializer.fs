module FsMathFunctions.Data.DatabaseInitializer

open Microsoft.EntityFrameworkCore
open FsMathFunctions.Data.AppDbContext

/// Ensures the database schema exists.
/// Uses EnsureCreated() which creates all tables defined in AppDbContext if they
/// do not already exist.  This is intentionally idempotent and safe to call on
/// every application start.
///
/// For production schema changes run:
///   dotnet ef dbcontext script --project FsMathFunctions.Data
/// and apply the generated SQL to your database.
let ensureCreated (db: AppDbContext) =
    db.Database.EnsureCreated() |> ignore
