# TestingAPIs

**API: [api.testingapis.com](https://api.testingapis.com)** — interactive docs at [api.testingapis.com/swagger](https://api.testingapis.com/swagger).

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/SagarKapase/snap-test?include_prereleases)](https://github.com/SagarKapase/snap-test/releases)

Hardcoded dummy APIs for testing HTTP clients, API tools and automation suites — about 430 REST endpoints,
GraphQL, WebSockets and SOAP, built with ASP.NET Core 9. No database, no sign-up: all data is in memory and
resets when the server restarts.

## What's inside

| Area | Examples |
|---|---|
| HTTP basics | echo, every HTTP method, any status code, redirects, cookies, ETag / 304 caching, request body types |
| Formats & data | JSON, XML, CSV, YAML, HTML, real PNG/PDF/ZIP files, gzip/brotli, byte ranges, JSON edge cases, utilities |
| Auth & security | Basic, Bearer, API key, Digest, HMAC signatures, CSRF, OAuth2 (PKCE), JWT with roles, rate limiting, idempotent payments |
| API patterns | Server-sent events, streaming, WebSockets, async jobs, six pagination styles, chaos testing, webhook capture, versioning, SOAP, validation |
| Resources | products, posts, todos, carts, orders, employees, books, movies, countries and more — full CRUD with filtering, sorting and paging |

Add `?delay=N` (seconds) or `?error=CODE` to any request to simulate slow responses and failures.

## Run it

**Download a release** from the [Releases page](https://github.com/SagarKapase/snap-test/releases) for your
platform, extract it and run `snap-test` (`snap-test.exe` on Windows). The server listens on
http://localhost:5000 by default; choose another address with `--urls http://localhost:5251`.

On macOS, clear the download quarantine first if Gatekeeper blocks it: `xattr -d com.apple.quarantine snap-test`.

The `portable` archive needs the [.NET 9 runtime](https://dotnet.microsoft.com/download/dotnet/9.0):
`dotnet snap-test.dll`.

**Docker:**

```bash
docker run --rm -p 8080:8080 ghcr.io/sagarkapase/apibee:latest
```

**From source** (requires the .NET 9 SDK):

```bash
dotnet run --project snap-test --launch-profile http   # http://localhost:5251
```

## Deploy to Azure (always on)

[`infra/main.bicep`](infra/main.bicep) runs the published Docker image on Azure Container Apps with exactly one
always-on replica (no sleeping, no cold starts), HTTPS and health checks. With the
[Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli):

```bash
az login
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.OperationalInsights --wait
az group create -n rg-apibee -l centralindia
az deployment group create -g rg-apibee -f infra/main.bicep -p image=ghcr.io/sagarkapase/apibee:latest
```

The last command prints the public URL. Keep the replica count at one: the API stores everything in memory,
so a second replica would not see the first one's data.

After the first deployment, every stable release can update Azure automatically: set the repository variables
`AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` for an identity that has a federated
credential for `repo:SagarKapase/snap-test:environment:production` and Contributor access to the resource group.

## Where to look

| URL | What |
|---|---|
| `/swagger` | Interactive API documentation (the site root redirects here) |
| `/openapi/v1.json` | OpenAPI 3.0 document |
| `/api` | Live list of every route |
| `/graphql` | GraphQL endpoint (browser IDE on GET) |
| `ws://<host>/ws/echo`, `ws://<host>/ws/ticker` | WebSocket channels |

Test credentials for every auth scheme are listed in Swagger and at `GET /api/auth`.

## Security note

Everything here is for testing. All credentials are hardcoded and public, and the JWT signing key in
`snap-test/appsettings.json` is a published test value. If you deploy this anywhere reachable by others, set your
own key through the environment (`Jwt__Key=<at least 32 random characters>`) and do not rely on any of the demo
credentials. `/api/Proxy/call` forwards requests from the server to public URLs. It requires an `X-Proxy-Key` header matching
the server's `Proxy__AccessKey` setting (no key configured = proxy disabled), and it refuses private, loopback,
link-local and cloud-metadata addresses, also after redirects. Set `Proxy__AllowPrivateNetworks=true` only for
local development.

## Releasing

Push a version tag and GitHub Actions does the rest:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The [release workflow](.github/workflows/release.yml) builds self-contained archives for Windows, Linux (x64 and
ARM64) and macOS (Intel and Apple Silicon) plus a portable build, smoke-tests the Linux build, publishes a GitHub
Release with SHA-256 checksums, and pushes a multi-arch Docker image to `ghcr.io`. Tags with a suffix such as
`v1.1.0-beta.1` are published as pre-releases.

## License

MIT © 2026 Sagar Kapase — see [LICENSE](LICENSE).

Builds include third-party components under their own licenses (MIT, Apache-2.0 and, for the GraphQL IDE, the
ChilliCream License 1.0). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Sample data is fictional; images are
links to the free placeholder services picsum.photos and pravatar.cc and are not included in this repository.
