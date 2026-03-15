@echo off
REM Same order as GitHub Actions: Generators first, then Falcorm (all TFMs), then pack
dotnet build Falcorm\Istarion.Falcorm.csproj -c Release -f net8.0
dotnet build Falcorm\Istarion.Falcorm.csproj -c Release -f net9.0
dotnet build Falcorm\Istarion.Falcorm.csproj -c Release -f net10.0
dotnet pack Falcorm\Istarion.Falcorm.csproj -c Release -o nupkg --no-build
