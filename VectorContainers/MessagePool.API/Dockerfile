FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["MessagePool.API/MessagePool.API.csproj", "MessagePool.API/"]
COPY ["Core.API/Core.API.csproj", "Core.API/"]
RUN dotnet restore "MessagePool.API/MessagePool.API.csproj"
COPY . .
WORKDIR "/src/MessagePool.API"
RUN dotnet build "MessagePool.API.csproj" -c Release -o /app
FROM build AS publish
RUN dotnet publish "MessagePool.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MessagePool.API.dll"]
