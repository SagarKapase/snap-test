using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Rich user profiles with nested address, geo coordinates and company, at /api/people. The legacy /api/user endpoints are separate. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class PeopleController : CrudControllerBase<Person>
    {
        /// <inheritdoc />
        protected override List<Person> Store => PersonStore.People;
        /// <inheritdoc />
        protected override string ResourceName => "Person";

        /// <inheritdoc />
        protected override string? Validate(Person item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.FirstName)) return "Missing required field: firstName";
            if (string.IsNullOrWhiteSpace(item.LastName)) return "Missing required field: lastName";
            if (string.IsNullOrWhiteSpace(item.Username)) return "Missing required field: username";
            if (string.IsNullOrWhiteSpace(item.Email) || !item.Email.Contains('@')) return "Field 'email' must be a valid email address.";
            return null;
        }
    }
}
