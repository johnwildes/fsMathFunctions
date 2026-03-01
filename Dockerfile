# Multi-stage build for FsMathFunctions.Api (.NET 10)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["FsMathFunctions.Api/FsMathFunctions.Api.fsproj", "FsMathFunctions.Api/"]
RUN dotnet restore "FsMathFunctions.Api/FsMathFunctions.Api.fsproj"

# Copy source and build
COPY . .
WORKDIR "/src/FsMathFunctions.Api"
RUN dotnet build "FsMathFunctions.Api.fsproj" -c Release -o /app/build

# ---- Publish stage ----
FROM build AS publish
RUN dotnet publish "FsMathFunctions.Api.fsproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FsMathFunctions.Api.dll"]
