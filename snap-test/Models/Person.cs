using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>Rich user profile (JSONPlaceholder/randomuser style). Separate from the legacy /api/user model.</summary>
    public class Person : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Given name (required).</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>Family name (required).</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>Username (required).</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>Email address (required, must contain @).</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Gender.</summary>
        public string Gender { get; set; } = string.Empty;
        /// <summary>Date of birth (yyyy-MM-dd).</summary>
        public string DateOfBirth { get; set; } = string.Empty;
        /// <summary>Phone number.</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>Avatar image URL.</summary>
        public string Avatar { get; set; } = string.Empty;
        /// <summary>Home address with coordinates.</summary>
        public Address Address { get; set; } = new();
        /// <summary>Employer and job title.</summary>
        public PersonCompany Company { get; set; } = new();
    }

    /// <summary>Where a person works.</summary>
    public class PersonCompany
    {
        /// <summary>Company name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Job title.</summary>
        public string Title { get; set; } = string.Empty;
    }
}
