FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["SiloHost/SiloHost.csproj", "SiloHost/"]
COPY ["Interfaces/Interfaces.csproj", "Interfaces/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Grains/Grains.csproj", "Grains/"]
RUN dotnet restore "SiloHost/SiloHost.csproj"
COPY . .
WORKDIR "/src/SiloHost"
RUN dotnet build "SiloHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SiloHost.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SiloHost.dll"]
