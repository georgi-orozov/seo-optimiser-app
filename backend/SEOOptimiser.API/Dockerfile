# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer-cache-friendly restore
COPY SEOOptimiser.Core/SEOOptimiser.Core.csproj SEOOptimiser.Core/
COPY SEOOptimiser.Infrastructure/SEOOptimiser.Infrastructure.csproj SEOOptimiser.Infrastructure/
COPY SEOOptimiser.API/SEOOptimiser.API.csproj SEOOptimiser.API/

RUN dotnet restore SEOOptimiser.API/SEOOptimiser.API.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish SEOOptimiser.API/SEOOptimiser.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SEOOptimiser.API.dll"]
