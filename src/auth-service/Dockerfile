FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY Auth.Domain/Auth.Domain.csproj Auth.Domain/
COPY Auth.Application/Auth.Application.csproj Auth.Application/
COPY Auth.Infrastructure/Auth.Infrastructure.csproj Auth.Infrastructure/
COPY Auth.Api/Auth.Api.csproj Auth.Api/
RUN dotnet restore Auth.Api/Auth.Api.csproj
COPY Auth.Domain/ Auth.Domain/
COPY Auth.Application/ Auth.Application/
COPY Auth.Infrastructure/ Auth.Infrastructure/
COPY Auth.Api/ Auth.Api/
RUN dotnet publish Auth.Api/Auth.Api.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
