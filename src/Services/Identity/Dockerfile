FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Identity.Domain/Identity.Domain.csproj Identity.Domain/
COPY Identity.Application/Identity.Application.csproj Identity.Application/
COPY Identity.Infrastructure/Identity.Infrastructure.csproj Identity.Infrastructure/
COPY Identity.API/Identity.API.csproj Identity.API/

RUN dotnet restore Identity.API/Identity.API.csproj

COPY . .

RUN dotnet publish Identity.API/Identity.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5210
ENTRYPOINT ["dotnet", "Identity.API.dll"]
