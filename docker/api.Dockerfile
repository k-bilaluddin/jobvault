# ================================
# Build Stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and all project files first (layer caching)
COPY backend/src/JobVault.API/JobVault.sln ./

COPY backend/src/JobVault.API/JobVault.API.csproj                         JobVault.API/
COPY backend/src/JobVault.Application/JobVault.Application.csproj         JobVault.Application/
COPY backend/src/JobVault.Domain/JobVault.Domain.csproj                   JobVault.Domain/
COPY backend/src/JobVault.Infrastructure/JobVault.Infrastructure.csproj   JobVault.Infrastructure/
COPY backend/src/JobVault.Contracts/JobVault.Contracts.csproj             JobVault.Contracts/
COPY backend/src/JobVault.Worker/JobVault.Worker.csproj                   JobVault.Worker/
COPY backend/tests/JobVault.ArchitectureTests/JobVault.ArchitectureTests.csproj JobVault.ArchitectureTests/

COPY backend/src/JobVault.API/nuget.config ./nuget.config


RUN dotnet restore JobVault.API/JobVault.API.csproj

# Copy all source
COPY backend/src/JobVault.API/           JobVault.API/
COPY backend/src/JobVault.Application/   JobVault.Application/
COPY backend/src/JobVault.Domain/        JobVault.Domain/
COPY backend/src/JobVault.Infrastructure/ JobVault.Infrastructure/
COPY backend/src/JobVault.Contracts/     JobVault.Contracts/

RUN dotnet publish JobVault.API/JobVault.API.csproj \
    -c Release \
    -o /out

# ================================
# Runtime Stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "JobVault.API.dll"]
