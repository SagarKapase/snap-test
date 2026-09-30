using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Sample companies; employees link to them through companyId. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class CompaniesController : CrudControllerBase<Company>
    {
        /// <inheritdoc />
        protected override List<Company> Store => CompanyStore.Companies;
        /// <inheritdoc />
        protected override string ResourceName => "Company";

        /// <inheritdoc />
        protected override string? Validate(Company item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Name)) return "Missing required field: name";
            if (string.IsNullOrWhiteSpace(item.Industry)) return "Missing required field: industry";
            if (item.Founded != 0 && (item.Founded < 1800 || item.Founded > DateTime.UtcNow.Year))
                return $"Field 'founded' must be between 1800 and {DateTime.UtcNow.Year}.";
            if (item.Revenue < 0 || item.EmployeeCount < 0) return "Fields 'revenue' and 'employeeCount' must be zero or greater.";
            return null;
        }

        // -------------------- COMPANY EMPLOYEES (nested) --------------------
        /// <summary>List a company's employees.</summary>
        /// <param name="id">Company ID.</param>
        /// <response code="200">Employees whose companyId is this ID (an empty array if none).</response>
        /// <response code="404">The company does not exist.</response>
        [HttpGet("{id:int}/employees")]
        public IActionResult GetEmployees(int id)
        {
            lock (Store)
            {
                if (!Store.Any(c => c.Id == id)) return NotFoundError(id);
            }

            lock (EmployeeStore.Employees)
            {
                return Ok(EmployeeStore.Employees.Where(e => e.CompanyId == id).ToList());
            }
        }
    }
}
