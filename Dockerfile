# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/WeddingInviteBackend.sln"
RUN dotnet build "src/WeddingInviteBackend.sln" -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "src/WeddingInvite.Api/WeddingInvite.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeddingInvite.Api.dll"]
