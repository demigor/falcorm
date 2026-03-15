# Stage for VS debug: empty runtime, app is mounted from host
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Falcorm/Istarion.Falcorm.csproj", "Falcorm/"]
COPY ["Generators/Istarion.Generators.csproj", "Generators/"]
COPY ["IntegrationTests/Falcorm.IntegrationTests.csproj", "IntegrationTests/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore IntegrationTests/Falcorm.IntegrationTests.csproj
COPY . .
RUN dotnet build IntegrationTests/Falcorm.IntegrationTests.csproj -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS run
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Falcorm.IntegrationTests.dll"]
