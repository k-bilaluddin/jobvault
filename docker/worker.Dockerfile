# ================================
# Build Stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first
COPY backend/src/JobVault.API/JobVault.sln ./

COPY backend/src/JobVault.API/JobVault.API.csproj                         JobVault.API/
COPY backend/src/JobVault.Application/JobVault.Application.csproj         JobVault.Application/
COPY backend/src/JobVault.Domain/JobVault.Domain.csproj                   JobVault.Domain/
COPY backend/src/JobVault.Infrastructure/JobVault.Infrastructure.csproj   JobVault.Infrastructure/
COPY backend/src/JobVault.Contracts/JobVault.Contracts.csproj             JobVault.Contracts/
COPY backend/src/JobVault.Worker/JobVault.Worker.csproj                   JobVault.Worker/
COPY backend/tests/JobVault.ArchitectureTests/JobVault.ArchitectureTests.csproj JobVault.ArchitectureTests/

COPY backend/src/JobVault.API/nuget.config ./nuget.config

RUN dotnet restore JobVault.Worker/JobVault.Worker.csproj

# Copy source
COPY backend/src/JobVault.Worker/         JobVault.Worker/
COPY backend/src/JobVault.Application/    JobVault.Application/
COPY backend/src/JobVault.Domain/         JobVault.Domain/
COPY backend/src/JobVault.Infrastructure/ JobVault.Infrastructure/
COPY backend/src/JobVault.Contracts/      JobVault.Contracts/

RUN dotnet publish JobVault.Worker/JobVault.Worker.csproj \
    -c Release \
    -o /out

# ================================
# Runtime Stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# LibreOffice + font tooling for DOCX → PDF conversion
RUN apt-get update && apt-get install -y --no-install-recommends \
    libreoffice-writer \
    fontconfig \
    && rm -rf /var/lib/apt/lists/*

# Copy Calibri fonts from build context
COPY docker/fonts/*.ttf /usr/share/fonts/truetype/microsoft/

# Refresh font cache so LibreOffice can detect Calibri
RUN fc-cache -f -v

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

# Copy published application
COPY --from=build /out .

# Switch to non-root user
USER appuser

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "JobVault.Worker.dll"]