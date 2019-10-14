FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["TGMGateway/TGMGateway.csproj", "TGMGateway/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "TGMGateway/TGMGateway.csproj"
COPY . .
WORKDIR "/src/TGMGateway"
RUN dotnet build "TGMGateway.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "TGMGateway.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGMGateway.dll"]
