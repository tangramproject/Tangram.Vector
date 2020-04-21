FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Membership.API/Membership.API.csproj", "Membership.API/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "Membership.API/Membership.API.csproj"
COPY . .
WORKDIR "/src/Membership.API"
RUN dotnet build "Membership.API.csproj" -c Release -o /app
FROM build AS publish
RUN dotnet publish "Membership.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Membership.API.dll"]
