FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY Gateway.csproj .
RUN dotnet restore Gateway.csproj
COPY . .
RUN dotnet publish Gateway.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
