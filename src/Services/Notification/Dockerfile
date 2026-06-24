FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Notification.Domain/Notification.Domain.csproj Notification.Domain/
COPY Notification.Application/Notification.Application.csproj Notification.Application/
COPY Notification.Infrastructure/Notification.Infrastructure.csproj Notification.Infrastructure/
COPY Notification.API/Notification.API.csproj Notification.API/

RUN dotnet restore Notification.API/Notification.API.csproj

COPY . .

RUN dotnet publish Notification.API/Notification.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5095
ENTRYPOINT ["dotnet", "Notification.API.dll"]
