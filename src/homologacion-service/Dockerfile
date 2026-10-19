FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Homologacion.Domain/Homologacion.Domain.csproj Homologacion.Domain/
COPY Homologacion.Application/Homologacion.Application.csproj Homologacion.Application/
COPY Homologacion.Infrastructure/Homologacion.Infrastructure.csproj Homologacion.Infrastructure/
COPY Homologacion.Api/Homologacion.Api.csproj Homologacion.Api/
RUN dotnet restore Homologacion.Api/Homologacion.Api.csproj
COPY . .
RUN dotnet publish Homologacion.Api/Homologacion.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Homologacion.Api.dll"]
