module FsMathFunctions.Data.DesignTimeFactory

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Design
open FsMathFunctions.Data.AppDbContext

/// Allows `dotnet ef migrations add` to run against this project directly,
/// without needing a running application host.
type AppDbContextFactory() =
    interface IDesignTimeDbContextFactory<AppDbContext> with
        member _.CreateDbContext(_args: string[]) =
            let opts =
                DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql("Host=localhost;Database=fsmathfunctions;Username=app;Password=devpassword")
                    .Options
            new AppDbContext(opts)
