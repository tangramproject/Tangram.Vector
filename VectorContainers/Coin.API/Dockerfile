FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Coin.API/Coin.API.csproj", "Coin.API/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "Coin.API/Coin.API.csproj"
COPY . .
WORKDIR "/src/Coin.API"
RUN dotnet build "Coin.API.csproj" -c Release -o /app
FROM build AS publish
RUN dotnet publish "Coin.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Coin.API.dll"]
