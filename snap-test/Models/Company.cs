using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A sample company.</summary>
    public class Company : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Company name (required).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Industry (required).</summary>
        public string Industry { get; set; } = string.Empty;
        /// <summary>Year founded (1800 to the current year, or 0 if unknown).</summary>
        public int Founded { get; set; }
        /// <summary>Website URL.</summary>
        public string Website { get; set; } = string.Empty;
        /// <summary>Headquarters location.</summary>
        public CompanyHeadquarters Headquarters { get; set; } = new();
        /// <summary>Annual revenue in USD; zero or greater.</summary>
        public decimal Revenue { get; set; }
        /// <summary>Headcount; zero or greater.</summary>
        public int EmployeeCount { get; set; }
    }

    /// <summary>City and country of a company's headquarters.</summary>
    public class CompanyHeadquarters
    {
        /// <summary>City.</summary>
        public string City { get; set; } = string.Empty;
        /// <summary>Country.</summary>
        public string Country { get; set; } = string.Empty;
    }
}
