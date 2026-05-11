FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Friend.Domain/Friend.Domain.csproj Friend.Domain/
COPY Friend.Application/Friend.Application.csproj Friend.Application/
COPY Friend.Infrastructure/Friend.Infrastructure.csproj Friend.Infrastructure/
COPY Friend.API/Friend.API.csproj Friend.API/

RUN dotnet restore Friend.API/Friend.API.csproj

COPY . .

RUN dotnet publish Friend.API/Friend.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5176 5178
ENTRYPOINT ["dotnet", "Friend.API.dll"]
