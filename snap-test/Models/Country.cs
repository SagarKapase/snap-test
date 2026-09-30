using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A country.</summary>
    public class Country : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Common name (required).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>ISO 3166-1 alpha-2 code (required, 2 letters).</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>ISO 3166-1 alpha-3 code (3 letters).</summary>
        public string Code3 { get; set; } = string.Empty;
        /// <summary>Capital city.</summary>
        public string Capital { get; set; } = string.Empty;
        /// <summary>Region: Africa, Americas, Asia, Europe or Oceania.</summary>
        public string Region { get; set; } = string.Empty;
        /// <summary>Approximate population; zero or greater.</summary>
        public long Population { get; set; }
        /// <summary>Area in square kilometres; zero or greater.</summary>
        public double Area { get; set; }
        /// <summary>Main currency.</summary>
        public CountryCurrency Currency { get; set; } = new();
        /// <summary>Official or main languages.</summary>
        public List<string> Languages { get; set; } = new();
        /// <summary>International dialling code, e.g. +81.</summary>
        public string CallingCode { get; set; } = string.Empty;
        /// <summary>Flag emoji.</summary>
        public string Flag { get; set; } = string.Empty;
        /// <summary>UTC offsets used.</summary>
        public List<string> Timezones { get; set; } = new();
    }

    /// <summary>A currency.</summary>
    public class CountryCurrency
    {
        /// <summary>ISO 4217 code, e.g. JPY.</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Currency name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Currency symbol.</summary>
        public string Symbol { get; set; } = string.Empty;
    }
}
