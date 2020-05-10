FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Broker.API/Broker.API.csproj", "Broker.API/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "Broker.API/Broker.API.csproj"
COPY . .
WORKDIR "/src/Broker.API"
RUN dotnet build "Broker.API.csproj" -c Release -o /app
FROM build AS publish
RUN dotnet publish "Broker.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Broker.API.dll"]
