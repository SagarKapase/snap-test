# Build context is the repository root (so LICENSE and THIRD-PARTY-NOTICES.md can be copied in):
#   docker build -f snap-test/Dockerfile -t apibee .
# Visual Studio uses the same context via <DockerfileContext>..</DockerfileContext> in snap-test.csproj.

# Base image for building. Runs on the build machine's own architecture: the output is framework-dependent IL
# (UseAppHost=false), so one build serves every target platform and multi-arch builds need no emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
WORKDIR /src

# Copy csproj first so the restore layer is cached
COPY ["snap-test/snap-test.csproj", "snap-test/"]
RUN dotnet restore "snap-test/snap-test.csproj"

# Copy full project
COPY snap-test/ snap-test/
RUN dotnet publish "snap-test/snap-test.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
ARG VERSION=1.0.0
LABEL org.opencontainers.image.title="APIBee" \
      org.opencontainers.image.description="Hardcoded dummy APIs for testing HTTP clients, API tools and automation suites" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.source="https://github.com/SagarKapase/snap-test" \
      org.opencontainers.image.version="$VERSION"
WORKDIR /app
COPY --from=build /app/publish .
COPY ["LICENSE", "THIRD-PARTY-NOTICES.md", "./"]
EXPOSE 8080
# The aspnet base image listens on 8080 and provides a non-root "app" user.
USER $APP_UID
ENTRYPOINT ["dotnet", "snap-test.dll"]
