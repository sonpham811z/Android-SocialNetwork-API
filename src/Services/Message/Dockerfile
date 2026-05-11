FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Message.Domain/Message.Domain.csproj Message.Domain/
COPY Message.Application/Message.Application.csproj Message.Application/
COPY Message.Infrastructure/Message.Infrastructure.csproj Message.Infrastructure/
COPY Message.API/Message.API.csproj Message.API/

RUN dotnet restore Message.API/Message.API.csproj

COPY . .

RUN dotnet publish Message.API/Message.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5177
ENTRYPOINT ["dotnet", "Message.API.dll"]
