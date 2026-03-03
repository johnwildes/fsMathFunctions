# Multi-stage build for FsMathFunctions.Portal (.NET 10)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FsMathFunctions.Portal/FsMathFunctions.Portal.fsproj", "FsMathFunctions.Portal/"]
COPY ["FsMathFunctions.Data/FsMathFunctions.Data.fsproj",     "FsMathFunctions.Data/"]
RUN dotnet restore "FsMathFunctions.Portal/FsMathFunctions.Portal.fsproj"

COPY . .
WORKDIR "/src/FsMathFunctions.Portal"
RUN dotnet build "FsMathFunctions.Portal.fsproj" -c Release -o /app/build

# ---- Publish stage ----
FROM build AS publish
RUN dotnet publish "FsMathFunctions.Portal.fsproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FsMathFunctions.Portal.dll"]
