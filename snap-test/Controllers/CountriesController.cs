using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Countries with ISO codes, capitals, regions, currencies, languages and timezones. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class CountriesController : CrudControllerBase<Country>
    {
        /// <inheritdoc />
        protected override List<Country> Store => CountryStore.Countries;
        /// <inheritdoc />
        protected override string ResourceName => "Country";

        /// <inheritdoc />
        protected override string? Validate(Country item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Name)) return "Missing required field: name";
            if (item.Code?.Length != 2) return "Field 'code' must be a 2-letter ISO 3166-1 alpha-2 code.";
            if (!string.IsNullOrEmpty(item.Code3) && item.Code3.Length != 3) return "Field 'code3' must be a 3-letter ISO 3166-1 alpha-3 code.";
            if (item.Population < 0 || item.Area < 0) return "Fields 'population' and 'area' must be zero or greater.";
            return null;
        }

        // -------------------- BY ISO CODE (alpha-2 or alpha-3) --------------------
        /// <summary>Get a country by ISO 3166-1 code.</summary>
        /// <param name="code">Alpha-2 (JP) or alpha-3 (JPN) code, case-insensitive.</param>
        /// <response code="200">The country.</response>
        /// <response code="404">No country with this code does not exist.</response>
        [HttpGet("code/{code}")]
        public IActionResult GetByCode(string code)
        {
            lock (Store)
            {
                var country = Store.FirstOrDefault(c =>
                    string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Code3, code, StringComparison.OrdinalIgnoreCase));

                return country == null
                    ? NotFound(ApiResponse.Error(404, $"Country with code '{code}' does not exist."))
                    : Ok(country);
            }
        }

        // -------------------- REGIONS --------------------
        /// <summary>List regions with country counts and total population.</summary>
        /// <response code="200">Objects of the form { region, countries, population }, sorted by region.</response>
        [HttpGet("regions")]
        public IActionResult GetRegions()
        {
            lock (Store)
            {
                return Ok(Store.GroupBy(c => c.Region, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key)
                    .Select(g => new { region = g.Key, countries = g.Count(), population = g.Sum(c => c.Population) })
                    .ToList());
            }
        }

        // -------------------- BY REGION --------------------
        /// <summary>List the countries in a region.</summary>
        /// <param name="name">Region name, case-insensitive, e.g. europe.</param>
        /// <response code="200">Matching countries (an empty array for an unknown region).</response>
        [HttpGet("region/{name}")]
        public IActionResult GetByRegion(string name)
        {
            lock (Store)
            {
                return Ok(Store.Where(c => string.Equals(c.Region, name, StringComparison.OrdinalIgnoreCase)).ToList());
            }
        }
    }
}
