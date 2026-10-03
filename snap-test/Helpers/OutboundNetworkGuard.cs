using System.Net;
using System.Net.Sockets;

namespace snap_test.Helpers
{
    /// <summary>
    /// Keeps server-side fetches (the proxy endpoint) away from private, loopback, link-local and cloud-platform
    /// addresses, so a public deployment can't be used to reach its own internal network or the cloud metadata
    /// service. The check runs in the socket connect callback against the address actually being dialled, so it
    /// also covers redirects and hostnames that resolve to internal addresses (DNS rebinding).
    /// </summary>
    public static class OutboundNetworkGuard
    {
        public const string HttpClientName = "proxy";

        /// <summary>Registers the guarded HttpClient used by the proxy endpoint.</summary>
        /// <remarks>Set <c>Proxy:AllowPrivateNetworks</c> to true only for local development (e.g. to proxy to localhost).</remarks>
        public static IServiceCollection AddGuardedProxyClient(this IServiceCollection services, IConfiguration config)
        {
            var allowPrivate = config.GetValue("Proxy:AllowPrivateNetworks", false);

            services.AddHttpClient(HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30))
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,       // every redirect hop goes through ConnectCallback again
                    MaxAutomaticRedirections = 5,
                    UseProxy = false,
                    ConnectCallback = async (context, ct) =>
                    {
                        var host = context.DnsEndPoint.Host;
                        var addresses = IPAddress.TryParse(host, out var literal)
                            ? new[] { literal }
                            : await Dns.GetHostAddressesAsync(host, ct);

                        var allowed = addresses.Where(a => allowPrivate || IsPublic(a)).ToArray();
                        if (allowed.Length == 0)
                            throw new BlockedDestinationException(host);

                        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                        try
                        {
                            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch
                        {
                            socket.Dispose();
                            throw;
                        }
                    }
                });

            return services;
        }

        /// <summary>True only for globally routable unicast addresses.</summary>
        public static bool IsPublic(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = ip.GetAddressBytes();
                return !(b[0] == 0                                   // 0.0.0.0/8 "this network"
                      || b[0] == 10                                  // 10.0.0.0/8 private
                      || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // 100.64.0.0/10 carrier-grade NAT
                      || b[0] == 127                                 // loopback
                      || (b[0] == 169 && b[1] == 254)                // link-local, incl. cloud metadata 169.254.169.254
                      || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16.0.0/12 private
                      || (b[0] == 192 && b[1] == 0 && b[2] == 0)     // 192.0.0.0/24 IETF protocol assignments
                      || (b[0] == 192 && b[1] == 168)                // 192.168.0.0/16 private
                      || (b[0] == 198 && (b[1] == 18 || b[1] == 19)) // 198.18.0.0/15 benchmarking
                      || b[0] >= 224                                 // multicast, reserved, broadcast
                      || (b[0] == 168 && b[1] == 63 && b[2] == 129 && b[3] == 16)); // Azure platform (wire server, DNS)
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.IPv6None) || ip.Equals(IPAddress.IPv6Any)) return false;
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || ip.IsIPv6UniqueLocal) return false;
                var b = ip.GetAddressBytes();
                // 64:ff9b::/96 NAT64 embeds an IPv4 address; judge the embedded one.
                if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b && b.Take(12).Skip(4).All(x => x == 0))
                    return IsPublic(new IPAddress(b.Skip(12).ToArray()));
                return true;
            }

            return false;
        }
    }

    /// <summary>Thrown when every address a host resolves to is private or reserved.</summary>
    public class BlockedDestinationException : IOException
    {
        public BlockedDestinationException(string host)
            : base($"'{host}' resolves only to private, loopback, link-local or reserved addresses, which the proxy does not call.") { }
    }
}
