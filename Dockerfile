# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8081
ENV ASPNETCORE_URLS=http://*:8081
ENV ASPNETCORE_ENVIRONMENT=Development


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Fiap.Hackatoon.Identity.API/Fiap.Hackatoon.Identity.API.csproj", "Fiap.Hackatoon.Identity.API/"]
COPY ["Fiap.Hackatoon.Identity.Application/Fiap.Hackatoon.Identity.Application.csproj", "Fiap.Hackatoon.Identity.Application/"]
COPY ["Fiap.Hackatoon.Identity.Domain/Fiap.Hackatoon.Identity.Domain.csproj", "Fiap.Hackatoon.Identity.Domain/"]
COPY ["Fiap.Hackatoon.Identity.Infrastructure/Fiap.Hackatoon.Identity.Infrastructure.csproj", "Fiap.Hackatoon.Identity.Infrastructure/"]
RUN dotnet restore "./Fiap.Hackatoon.Identity.API/Fiap.Hackatoon.Identity.API.csproj"
COPY . .
WORKDIR "/src/Fiap.Hackatoon.Identity.API"
RUN dotnet build "./Fiap.Hackatoon.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Fiap.Hackatoon.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fiap.Hackatoon.Identity.API.dll"]