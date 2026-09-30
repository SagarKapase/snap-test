using System.Globalization;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// SOAP services for testing SOAP clients (SoapUI, Postman, WCF, zeep...). Both SOAP 1.1 (text/xml + SOAPAction)
    /// and SOAP 1.2 (application/soap+xml) are accepted; the response uses the same version as the request.
    ///   /api/soap/calculator — Add / Subtract / Multiply / Divide on intA + intB (namespace http://tempuri.org/). ?wsdl available.
    ///   /api/soap/countries  — GetCountryInfo(countryCode) / ListCountries over hardcoded data.
    /// Errors come back as SOAP Faults (1.1: always 500; 1.2: Sender faults 400, Receiver faults 500).
    /// </summary>
    [ApiController]
    [Route("api/soap")]
    public class SoapController : ControllerBase
    {
        private const string Soap11Ns = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string Soap12Ns = "http://www.w3.org/2003/05/soap-envelope";
        private const int MaxBodyBytes = 1024 * 1024;

        private static readonly XNamespace CalcNs = "http://tempuri.org/";
        private static readonly XNamespace CountryNs = "http://example.com/countries";
        private static readonly string[] CalcOperations = { "Add", "Subtract", "Multiply", "Divide" };

        private static readonly (string Code, string Name, string Capital, string Currency, string PhoneCode, string Continent, long Population)[] Countries =
        {
            ("US", "United States", "Washington, D.C.", "USD", "+1", "North America", 334914895),
            ("GB", "United Kingdom", "London", "GBP", "+44", "Europe", 68350000),
            ("IN", "India", "New Delhi", "INR", "+91", "Asia", 1428627663),
            ("DE", "Germany", "Berlin", "EUR", "+49", "Europe", 84482267),
            ("FR", "France", "Paris", "EUR", "+33", "Europe", 68170228),
            ("JP", "Japan", "Tokyo", "JPY", "+81", "Asia", 124516650),
            ("BR", "Brazil", "Brasília", "BRL", "+55", "South America", 216422446),
            ("CA", "Canada", "Ottawa", "CAD", "+1", "North America", 40097761),
            ("AU", "Australia", "Canberra", "AUD", "+61", "Oceania", 26638544),
            ("ZA", "South Africa", "Pretoria", "ZAR", "+27", "Africa", 60414495)
        };

        private enum SoapVersion { Soap11, Soap12 }

        // -------------------- CALCULATOR: WSDL / USAGE --------------------
        /// <summary>Get the calculator WSDL (with <c>?wsdl</c>) or usage information.</summary>
        /// <remarks>
        /// <c>GET /api/soap/calculator?wsdl</c> returns a WSDL 1.1 document with SOAP 1.1 and 1.2 bindings, which SoapUI,
        /// WCF or zeep can import. Without <c>?wsdl</c> you get JSON listing the operations and ready-to-send sample envelopes.
        /// </remarks>
        /// <response code="200">WSDL (<c>text/xml</c>) or usage JSON.</response>
        [HttpGet("calculator")]
        public IActionResult CalculatorInfo()
        {
            if (Request.Query.ContainsKey("wsdl"))
                return Content(CalculatorWsdl(), "text/xml; charset=utf-8", Encoding.UTF8);

            return Ok(new
            {
                service = "Calculator",
                wsdl = $"{BaseUrl()}/api/soap/calculator?wsdl",
                operations = CalcOperations,
                soap11 = new
                {
                    contentType = "text/xml; charset=utf-8",
                    soapAction = "http://tempuri.org/Add",
                    sample = $"<soap:Envelope xmlns:soap=\"{Soap11Ns}\"><soap:Body><Add xmlns=\"http://tempuri.org/\"><intA>5</intA><intB>3</intB></Add></soap:Body></soap:Envelope>"
                },
                soap12 = new
                {
                    contentType = "application/soap+xml; charset=utf-8",
                    sample = $"<soap:Envelope xmlns:soap=\"{Soap12Ns}\"><soap:Body><Add xmlns=\"http://tempuri.org/\"><intA>5</intA><intB>3</intB></Add></soap:Body></soap:Envelope>"
                }
            });
        }

        // -------------------- CALCULATOR --------------------
        /// <summary>Call a calculator operation: Add, Subtract, Multiply or Divide.</summary>
        /// <remarks>
        /// SOAP 1.1: send <c>Content-Type: text/xml</c> and <c>SOAPAction: "http://tempuri.org/Add"</c> (optional; if
        /// sent, it must name the operation in the body).
        /// <code>
        /// &lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;
        ///   &lt;soap:Body&gt;
        ///     &lt;Add xmlns="http://tempuri.org/"&gt;&lt;intA&gt;5&lt;/intA&gt;&lt;intB&gt;3&lt;/intB&gt;&lt;/Add&gt;
        ///   &lt;/soap:Body&gt;
        /// &lt;/soap:Envelope&gt;
        /// </code>
        /// SOAP 1.2: send <c>Content-Type: application/soap+xml</c> with the same body, using the
        /// <c>http://www.w3.org/2003/05/soap-envelope</c> namespace.
        /// <para>The response is <c>&lt;AddResponse&gt;&lt;AddResult&gt;8&lt;/AddResult&gt;&lt;/AddResponse&gt;</c>, using the
        /// same SOAP version as the request. Errors come back as SOAP Faults. SOAP 1.1 faults always use 500. SOAP 1.2 uses
        /// 400 for Sender faults and 500 for Receiver faults. Divide by zero is a Server/Receiver fault. DTDs are rejected.</para>
        /// </remarks>
        /// <response code="200">SOAP response envelope.</response>
        /// <response code="400">SOAP 1.2 Sender fault (bad input).</response>
        /// <response code="415">Content-Type is not <c>text/xml</c> or <c>application/soap+xml</c>.</response>
        /// <response code="500">SOAP Fault (all SOAP 1.1 faults; SOAP 1.2 Receiver faults).</response>
        [HttpPost("calculator")]
        public async Task<IActionResult> Calculator()
        {
            var version = DetectVersion();
            if (version == null) return UnsupportedMediaType();

            var (op, fault) = await ReadOperationAsync(version.Value);
            if (fault != null) return fault;

            var name = op!.Name.LocalName;
            if (!CalcOperations.Contains(name))
                return Fault(version.Value, "Client", $"Unknown operation '{name}'. Supported: {string.Join(", ", CalcOperations)}.");

            var actionFault = CheckSoapAction(version.Value, name);
            if (actionFault != null) return actionFault;

            if (!TryGetInt(op, "intA", out var a)) return Fault(version.Value, "Client", "Element 'intA' is missing or is not a valid integer.");
            if (!TryGetInt(op, "intB", out var b)) return Fault(version.Value, "Client", "Element 'intB' is missing or is not a valid integer.");

            int result;
            try
            {
                result = name switch
                {
                    "Add" => checked(a + b),
                    "Subtract" => checked(a - b),
                    "Multiply" => checked(a * b),
                    _ => b == 0 ? throw new DivideByZeroException() : a / b
                };
            }
            catch (DivideByZeroException)
            {
                return Fault(version.Value, "Server", "Attempted to divide by zero.");
            }
            catch (OverflowException)
            {
                return Fault(version.Value, "Client", "Arithmetic operation resulted in an overflow of a 32-bit integer.");
            }

            return Envelope(version.Value,
                new XElement(CalcNs + $"{name}Response", new XElement(CalcNs + $"{name}Result", result)));
        }

        // -------------------- COUNTRIES: USAGE --------------------
        /// <summary>Get usage information for the countries SOAP service.</summary>
        /// <response code="200">Operations, available country codes and a sample envelope.</response>
        [HttpGet("countries")]
        public IActionResult CountriesInfo() => Ok(new
        {
            service = "Countries",
            @namespace = CountryNs.NamespaceName,
            operations = new[] { "GetCountryInfo(countryCode)", "ListCountries()" },
            codes = Countries.Select(c => c.Code),
            sample = $"<soap:Envelope xmlns:soap=\"{Soap11Ns}\"><soap:Body><GetCountryInfo xmlns=\"{CountryNs.NamespaceName}\"><countryCode>IN</countryCode></GetCountryInfo></soap:Body></soap:Envelope>"
        });

        // -------------------- COUNTRIES --------------------
        /// <summary>Call a countries operation: GetCountryInfo or ListCountries.</summary>
        /// <remarks>
        /// Namespace <c>http://example.com/countries</c>. For SOAP 1.1 the optional SOAPAction is
        /// <c>"http://example.com/countries/GetCountryInfo"</c>.
        /// <code>
        /// &lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;
        ///   &lt;soap:Body&gt;
        ///     &lt;GetCountryInfo xmlns="http://example.com/countries"&gt;&lt;countryCode&gt;IN&lt;/countryCode&gt;&lt;/GetCountryInfo&gt;
        ///   &lt;/soap:Body&gt;
        /// &lt;/soap:Envelope&gt;
        /// </code>
        /// <c>ListCountries</c> takes no arguments and returns the code and name of all 10 countries.
        /// </remarks>
        /// <response code="200">SOAP response envelope.</response>
        /// <response code="400">SOAP 1.2 Sender fault (e.g. unknown country code).</response>
        /// <response code="415">Content-Type is not <c>text/xml</c> or <c>application/soap+xml</c>.</response>
        /// <response code="500">SOAP 1.1 fault.</response>
        [HttpPost("countries")]
        public async Task<IActionResult> CountriesService()
        {
            var version = DetectVersion();
            if (version == null) return UnsupportedMediaType();

            var (op, fault) = await ReadOperationAsync(version.Value);
            if (fault != null) return fault;

            var name = op!.Name.LocalName;
            var actionFault = CheckSoapAction(version.Value, name);
            if (actionFault != null) return actionFault;

            switch (name)
            {
                case "ListCountries":
                    return Envelope(version.Value, new XElement(CountryNs + "ListCountriesResponse",
                        new XElement(CountryNs + "Countries", Countries.Select(c =>
                            new XElement(CountryNs + "Country",
                                new XElement(CountryNs + "Code", c.Code),
                                new XElement(CountryNs + "Name", c.Name))))));

                case "GetCountryInfo":
                    var code = op.Elements().FirstOrDefault(e => e.Name.LocalName == "countryCode")?.Value.Trim().ToUpperInvariant();
                    if (string.IsNullOrEmpty(code))
                        return Fault(version.Value, "Client", "Element 'countryCode' is required.");

                    var country = Countries.FirstOrDefault(c => c.Code == code);
                    if (country.Code == null)
                        return Fault(version.Value, "Client", $"Country code '{code}' not found.");

                    return Envelope(version.Value, new XElement(CountryNs + "GetCountryInfoResponse",
                        new XElement(CountryNs + "Country",
                            new XElement(CountryNs + "Code", country.Code),
                            new XElement(CountryNs + "Name", country.Name),
                            new XElement(CountryNs + "Capital", country.Capital),
                            new XElement(CountryNs + "Currency", country.Currency),
                            new XElement(CountryNs + "PhoneCode", country.PhoneCode),
                            new XElement(CountryNs + "Continent", country.Continent),
                            new XElement(CountryNs + "Population", country.Population))));

                default:
                    return Fault(version.Value, "Client", $"Unknown operation '{name}'. Supported: GetCountryInfo, ListCountries.");
            }
        }

        private SoapVersion? DetectVersion()
        {
            var type = Request.ContentType ?? string.Empty;
            if (type.Contains("application/soap+xml", StringComparison.OrdinalIgnoreCase)) return SoapVersion.Soap12;
            if (type.Contains("text/xml", StringComparison.OrdinalIgnoreCase)) return SoapVersion.Soap11;
            return null;
        }

        private IActionResult UnsupportedMediaType() =>
            StatusCode(415, ApiResponse.Error(415, "SOAP requests must use Content-Type text/xml (SOAP 1.1) or application/soap+xml (SOAP 1.2)."));

        private async Task<(XElement? operation, IActionResult? fault)> ReadOperationAsync(SoapVersion version)
        {
            if (Request.ContentLength > MaxBodyBytes)
                return (null, Fault(version, "Client", "Request body exceeds 1MB."));

            string raw;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                raw = await reader.ReadToEndAsync();

            XDocument doc;
            try
            {
                // DTDs rejected outright: no external entities (XXE) and no entity-expansion bombs.
                using var xml = XmlReader.Create(new StringReader(raw),
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                doc = XDocument.Load(xml);
            }
            catch (XmlException ex)
            {
                return (null, Fault(version, "Client", $"Malformed XML: {ex.Message}"));
            }

            var envNs = version == SoapVersion.Soap11 ? Soap11Ns : Soap12Ns;
            var envelope = doc.Root;

            if (envelope == null || envelope.Name.LocalName != "Envelope")
                return (null, Fault(version, "Client", "Root element must be a SOAP Envelope."));

            if (envelope.Name.NamespaceName != envNs)
                return (null, Fault(version, "VersionMismatch",
                    $"Envelope namespace '{envelope.Name.NamespaceName}' does not match the Content-Type (expected '{envNs}')."));

            var operation = envelope.Element(XName.Get("Body", envNs))?.Elements().FirstOrDefault();
            if (operation == null)
                return (null, Fault(version, "Client", "SOAP Body must contain an operation element."));

            return (operation, null);
        }

        // SOAP 1.1 only: if a SOAPAction header is sent it must name the operation in the body.
        private IActionResult? CheckSoapAction(SoapVersion version, string operation)
        {
            if (version != SoapVersion.Soap11) return null;

            var action = Request.Headers["SOAPAction"].ToString().Trim().Trim('"');
            if (action.Length == 0 || action == operation || action.EndsWith("/" + operation)) return null;

            return Fault(version, "Client", $"SOAPAction '{action}' does not match body operation '{operation}'.");
        }

        private static bool TryGetInt(XElement op, string name, out int value)
        {
            value = 0;
            var text = op.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
            return text != null && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static ContentResult Envelope(SoapVersion version, XElement payload, int status = 200)
        {
            XNamespace ns = version == SoapVersion.Soap11 ? Soap11Ns : Soap12Ns;

            var envelope = new XElement(ns + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", ns.NamespaceName),
                new XElement(ns + "Body", payload));

            return new ContentResult
            {
                Content = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + envelope,
                ContentType = version == SoapVersion.Soap11 ? "text/xml; charset=utf-8" : "application/soap+xml; charset=utf-8",
                StatusCode = status
            };
        }

        /// <summary>code is a SOAP 1.1 fault code (Client / Server / VersionMismatch); mapped to Sender / Receiver for 1.2.</summary>
        private static ContentResult Fault(SoapVersion version, string code, string reason)
        {
            if (version == SoapVersion.Soap11)
            {
                XNamespace ns = Soap11Ns;
                return Envelope(version, new XElement(ns + "Fault",
                    new XElement("faultcode", $"soap:{code}"),
                    new XElement("faultstring", reason)), 500);
            }

            XNamespace ns12 = Soap12Ns;
            var code12 = code switch { "Client" => "Sender", "Server" => "Receiver", _ => code };

            return Envelope(version, new XElement(ns12 + "Fault",
                new XElement(ns12 + "Code", new XElement(ns12 + "Value", $"soap:{code12}")),
                new XElement(ns12 + "Reason",
                    new XElement(ns12 + "Text", new XAttribute(XNamespace.Xml + "lang", "en"), reason))),
                code12 == "Sender" ? 400 : 500);
        }

        private string BaseUrl() => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        private string CalculatorWsdl()
        {
            var location = SecurityElement.Escape($"{BaseUrl()}/api/soap/calculator");
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<wsdl:definitions xmlns:wsdl=\"http://schemas.xmlsoap.org/wsdl/\" xmlns:soap=\"http://schemas.xmlsoap.org/wsdl/soap/\" xmlns:soap12=\"http://schemas.xmlsoap.org/wsdl/soap12/\" xmlns:s=\"http://www.w3.org/2001/XMLSchema\" xmlns:tns=\"http://tempuri.org/\" targetNamespace=\"http://tempuri.org/\">");

            sb.AppendLine("  <wsdl:types>");
            sb.AppendLine("    <s:schema elementFormDefault=\"qualified\" targetNamespace=\"http://tempuri.org/\">");
            foreach (var op in CalcOperations)
            {
                sb.AppendLine($"      <s:element name=\"{op}\"><s:complexType><s:sequence><s:element minOccurs=\"1\" maxOccurs=\"1\" name=\"intA\" type=\"s:int\"/><s:element minOccurs=\"1\" maxOccurs=\"1\" name=\"intB\" type=\"s:int\"/></s:sequence></s:complexType></s:element>");
                sb.AppendLine($"      <s:element name=\"{op}Response\"><s:complexType><s:sequence><s:element minOccurs=\"1\" maxOccurs=\"1\" name=\"{op}Result\" type=\"s:int\"/></s:sequence></s:complexType></s:element>");
            }
            sb.AppendLine("    </s:schema>");
            sb.AppendLine("  </wsdl:types>");

            foreach (var op in CalcOperations)
            {
                sb.AppendLine($"  <wsdl:message name=\"{op}SoapIn\"><wsdl:part name=\"parameters\" element=\"tns:{op}\"/></wsdl:message>");
                sb.AppendLine($"  <wsdl:message name=\"{op}SoapOut\"><wsdl:part name=\"parameters\" element=\"tns:{op}Response\"/></wsdl:message>");
            }

            sb.AppendLine("  <wsdl:portType name=\"CalculatorSoap\">");
            foreach (var op in CalcOperations)
                sb.AppendLine($"    <wsdl:operation name=\"{op}\"><wsdl:input message=\"tns:{op}SoapIn\"/><wsdl:output message=\"tns:{op}SoapOut\"/></wsdl:operation>");
            sb.AppendLine("  </wsdl:portType>");

            foreach (var (binding, prefix) in new[] { ("CalculatorSoap", "soap"), ("CalculatorSoap12", "soap12") })
            {
                sb.AppendLine($"  <wsdl:binding name=\"{binding}\" type=\"tns:CalculatorSoap\">");
                sb.AppendLine($"    <{prefix}:binding transport=\"http://schemas.xmlsoap.org/soap/http\"/>");
                foreach (var op in CalcOperations)
                    sb.AppendLine($"    <wsdl:operation name=\"{op}\"><{prefix}:operation soapAction=\"http://tempuri.org/{op}\" style=\"document\"/><wsdl:input><{prefix}:body use=\"literal\"/></wsdl:input><wsdl:output><{prefix}:body use=\"literal\"/></wsdl:output></wsdl:operation>");
                sb.AppendLine("  </wsdl:binding>");
            }

            sb.AppendLine("  <wsdl:service name=\"Calculator\">");
            sb.AppendLine($"    <wsdl:port name=\"CalculatorSoap\" binding=\"tns:CalculatorSoap\"><soap:address location=\"{location}\"/></wsdl:port>");
            sb.AppendLine($"    <wsdl:port name=\"CalculatorSoap12\" binding=\"tns:CalculatorSoap12\"><soap12:address location=\"{location}\"/></wsdl:port>");
            sb.AppendLine("  </wsdl:service>");
            sb.AppendLine("</wsdl:definitions>");

            return sb.ToString();
        }
    }
}
