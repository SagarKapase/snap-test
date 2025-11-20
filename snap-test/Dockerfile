# Base image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj
COPY ["snap-test.csproj", "./"]
RUN dotnet restore "./snap-test.csproj"

# Copy full project
COPY . .
RUN dotnet publish "snap-test.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "snap-test.dll"]
