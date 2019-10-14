FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Onion.API/Onion.API.csproj", "Onion.API/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "Onion.API/Onion.API.csproj"
COPY . .
WORKDIR "/src/Onion.API"
RUN dotnet build "Onion.API.csproj" -c Release -o /app
FROM build AS publish
RUN dotnet publish "Onion.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Onion.API.dll"]
