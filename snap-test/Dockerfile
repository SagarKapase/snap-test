# Works with either build context:
#   repository root:  docker build -f snap-test/Dockerfile .   (GitHub release workflow, Visual Studio)
#   project folder:   docker build snap-test                   (e.g. Render with Root Directory = snap-test)
# LICENSE and THIRD-PARTY-NOTICES.md are copied into the publish output by the csproj whenever they are
# inside the build context (always true for the repository-root builds used for published images).

# Build stage. Runs on the build machine's own architecture: the output is framework-dependent IL
# (UseAppHost=false), so one build serves every target platform and multi-arch builds need no emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
WORKDIR /src
COPY . .
RUN set -eu; \
    if [ -f snap-test/snap-test.csproj ]; then PROJECT=snap-test/snap-test.csproj; else PROJECT=snap-test.csproj; fi; \
    dotnet publish "$PROJECT" -c "$BUILD_CONFIGURATION" -o /app/publish -p:UseAppHost=false -p:Version="$VERSION"

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
# Listens on 8080 by default, or on $PORT when the host sets it (Render, Railway, Heroku, Cloud Run).
EXPOSE 8080
# The aspnet base image provides a non-root "app" user.
USER $APP_UID
ENTRYPOINT ["dotnet", "snap-test.dll"]
