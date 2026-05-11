FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY User.Domain/User.Domain.csproj User.Domain/
COPY User.Application/User.Application.csproj User.Application/
COPY User.Infrastructure/User.Infrastructure.csproj User.Infrastructure/
COPY User.API/User.API.csproj User.API/

RUN dotnet restore User.API/User.API.csproj

COPY . .

RUN dotnet publish User.API/User.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5220
ENTRYPOINT ["dotnet", "User.API.dll"]
