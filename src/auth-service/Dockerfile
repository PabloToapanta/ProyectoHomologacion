FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY AuthService/AuthService.csproj AuthService/
RUN dotnet restore AuthService/AuthService.csproj
COPY AuthService/ AuthService/
RUN dotnet publish AuthService/AuthService.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthService.dll"]
