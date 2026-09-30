using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>An employee of one of the sample companies.</summary>
    public class Employee : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Given name (required).</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>Family name (required).</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>Work email (required, must contain @).</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Phone number.</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>Department name (required).</summary>
        public string Department { get; set; } = string.Empty;
        /// <summary>Job title.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Annual salary; zero or greater.</summary>
        public decimal Salary { get; set; }
        /// <summary>Hire date (yyyy-MM-dd).</summary>
        public string HireDate { get; set; } = string.Empty;
        /// <summary>ID of the employee's manager, or null at the top of the org chart.</summary>
        public int? ManagerId { get; set; }
        /// <summary>ID of the employing company.</summary>
        public int CompanyId { get; set; }
        /// <summary>Skill tags.</summary>
        public List<string> Skills { get; set; } = new();
        /// <summary>Home address.</summary>
        public Address Address { get; set; } = new();
        /// <summary>Whether the employee currently works there.</summary>
        public bool Active { get; set; }
    }
}
