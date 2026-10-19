FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY . ./
RUN dotnet restore Catalogo.Api/Catalogo.Api.csproj
RUN dotnet publish Catalogo.Api/Catalogo.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalogo.Api.dll"]
