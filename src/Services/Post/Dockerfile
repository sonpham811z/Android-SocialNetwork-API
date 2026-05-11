FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Post.Domain/Post.Domain.csproj Post.Domain/
COPY Post.Application/Post.Application.csproj Post.Application/
COPY Post.Infrastructure/Post.Infrastructure.csproj Post.Infrastructure/
COPY Post.API/Post.API.csproj Post.API/

RUN dotnet restore Post.API/Post.API.csproj

COPY . .

RUN dotnet publish Post.API/Post.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5175
ENTRYPOINT ["dotnet", "Post.API.dll"]
