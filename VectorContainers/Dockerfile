FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
COPY . /src
WORKDIR /src

RUN dotnet restore "TGMGateway/TGMGateway.csproj"
RUN dotnet restore "Broker.API/Broker.API.csproj"
RUN dotnet restore "Coin.API/Coin.API.csproj"
RUN dotnet restore "Core.API/Core.API.csproj"
RUN dotnet restore "Membership.API/Membership.API.csproj"
RUN dotnet restore "MessagePool.API/MessagePool.API.csproj"
RUN dotnet restore "Onion.API/Onion.API.csproj"

RUN cd /src/TGMGateway && dotnet build "TGMGateway.csproj" -c Release -o /app
RUN cd /src/Broker.API && dotnet build "Broker.API.csproj" -c Release -o /app
RUN cd /src/Coin.API && dotnet build "Coin.API.csproj" -c Release -o /app
RUN cd /src/Core.API && dotnet build "Core.API.csproj" -c Release -o /app
RUN cd /src/Membership.API && dotnet build "Membership.API.csproj" -c Release -o /app
RUN cd /src/MessagePool.API && dotnet build "MessagePool.API.csproj" -c Release -o /app
RUN cd /src/Onion.API && dotnet build "Onion.API.csproj" -c Release -o /app

FROM build AS publish
RUN cd /src/TGMGateway && dotnet publish "TGMGateway.csproj" -c Release -o /app
RUN cd /src/Broker.API && dotnet publish "Broker.API.csproj" -c Release -o /app
RUN cd /src/Coin.API && dotnet publish "Coin.API.csproj" -c Release -o /app
RUN cd /src/Core.API && dotnet publish "Core.API.csproj" -c Release -o /app
RUN cd /src/Membership.API && dotnet publish "Membership.API.csproj" -c Release -o /app
RUN cd /src/MessagePool.API && dotnet publish "MessagePool.API.csproj" -c Release -o /app
RUN cd /src/Onion.API && dotnet publish "Onion.API.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app .

RUN apt-get update && apt-get install -y supervisor
COPY supervisord.conf /etc/supervisord/conf.d/vector.conf
ENTRYPOINT ["supervisord", "-c", "/etc/supervisor/supervisord.conf"]
