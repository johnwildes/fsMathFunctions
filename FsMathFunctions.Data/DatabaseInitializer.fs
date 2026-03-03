module FsMathFunctions.Data.DatabaseInitializer

open Microsoft.EntityFrameworkCore
open Npgsql
open FsMathFunctions.Data.AppDbContext

/// Ensures the database schema exists.
/// Uses EnsureCreated() which creates all tables defined in AppDbContext if they
/// do not already exist.  Handles the race condition where multiple services
/// start simultaneously and both attempt schema creation — if tables are already
/// created by a sibling service, the 42P07 error is safely ignored.
///
/// For production schema changes run:
///   dotnet ef dbcontext script --project FsMathFunctions.Data
/// and apply the generated SQL to your database.
let ensureCreated (db: AppDbContext) =
    try
        db.Database.EnsureCreated() |> ignore
    with
    | :? PostgresException as ex when ex.SqlState = "42P07" ->
        // Another service already created the schema concurrently — this is fine.
        ()
