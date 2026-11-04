
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VetPax.Api/*.csproj VetPax.Api/
COPY VetPax.Application/*.csproj VetPax.Application/
COPY VetPax.Domain/*.csproj VetPax.Domain/
COPY VetPax.Infrastructure/*.csproj VetPax.Infrastructure/

RUN dotnet restore VetPax.Api/VetPax.Api.csproj

COPY . .

RUN dotnet publish VetPax.Api/VetPax.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VetPax.Api.dll"]
