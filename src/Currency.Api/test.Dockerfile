# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install curl (for health checks or debugging)
RUN apt-get update && apt-get install -y curl

# Create a non-root user and switch to it for better security
ARG APP_UID=1000
#RUN useradd -m -u $APP_UID appuser
#USER appuser

WORKDIR /app

# Expose ports your app uses — adjust to your app settings
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Currency.Api/Currency.Api.csproj", "Currency.Api/"]
RUN dotnet restore "Currency.Api/Currency.Api.csproj"

COPY . .

WORKDIR "/src/Currency.Api"
RUN dotnet build "./Currency.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Currency.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Currency.Api.dll"]

