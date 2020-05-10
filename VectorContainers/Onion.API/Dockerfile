FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
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
# Needed for 'libevent' to be found by dynamic linker...
RUN echo "/app" >/etc/ld.so.conf.d/app.conf && ldconfig
ENTRYPOINT ["dotnet", "Onion.API.dll"]
