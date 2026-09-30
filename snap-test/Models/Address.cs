namespace snap_test.Models
{
    /// <summary>Postal address shared by Employee and Person.</summary>
    public class Address
    {
        /// <summary>Street and house number.</summary>
        public string Street { get; set; } = string.Empty;
        /// <summary>City.</summary>
        public string City { get; set; } = string.Empty;
        /// <summary>State, province or region (may be empty).</summary>
        public string State { get; set; } = string.Empty;
        /// <summary>Postal code.</summary>
        public string Zip { get; set; } = string.Empty;
        /// <summary>Country name.</summary>
        public string Country { get; set; } = string.Empty;
        /// <summary>Map coordinates, or null when unknown.</summary>
        public GeoLocation? Geo { get; set; }
    }

    /// <summary>Latitude/longitude pair.</summary>
    public class GeoLocation
    {
        /// <summary>Latitude in decimal degrees.</summary>
        public double Lat { get; set; }
        /// <summary>Longitude in decimal degrees.</summary>
        public double Lng { get; set; }
    }
}
