using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Employees of the sample companies, with an org chart (managerId), departments and salary stats. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class EmployeesController : CrudControllerBase<Employee>
    {
        /// <inheritdoc />
        protected override List<Employee> Store => EmployeeStore.Employees;
        /// <inheritdoc />
        protected override string ResourceName => "Employee";

        /// <inheritdoc />
        protected override string? Validate(Employee item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.FirstName)) return "Missing required field: firstName";
            if (string.IsNullOrWhiteSpace(item.LastName)) return "Missing required field: lastName";
            if (string.IsNullOrWhiteSpace(item.Email) || !item.Email.Contains('@')) return "Field 'email' must be a valid email address.";
            if (string.IsNullOrWhiteSpace(item.Department)) return "Missing required field: department";
            if (item.Salary < 0) return "Field 'salary' must be zero or greater.";
            return null;
        }

        // -------------------- DIRECT REPORTS --------------------
        /// <summary>List an employee's direct reports.</summary>
        /// <param name="id">Manager employee ID.</param>
        /// <response code="200">Employees whose managerId is this ID (an empty array if none).</response>
        /// <response code="404">The employee does not exist.</response>
        [HttpGet("{id:int}/reports")]
        public IActionResult GetReports(int id)
        {
            lock (Store)
            {
                if (!Store.Any(e => e.Id == id)) return NotFoundError(id);
                return Ok(Store.Where(e => e.ManagerId == id).ToList());
            }
        }

        // -------------------- DEPARTMENTS --------------------
        /// <summary>List department names, sorted and without duplicates.</summary>
        /// <response code="200">Department names.</response>
        [HttpGet("departments")]
        public IActionResult GetDepartments()
        {
            lock (Store)
            {
                return Ok(Store.Select(e => e.Department)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(d => d)
                    .ToList());
            }
        }

        // -------------------- SALARY STATS PER DEPARTMENT --------------------
        /// <summary>Get salary statistics overall and per department.</summary>
        /// <response code="200">Totals, active count, average salary, and count/average/min/max salary per department.</response>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            lock (Store)
            {
                var departments = Store
                    .GroupBy(e => e.Department, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        department = g.Key,
                        count = g.Count(),
                        averageSalary = Math.Round(g.Average(e => e.Salary), 2),
                        minSalary = g.Min(e => e.Salary),
                        maxSalary = g.Max(e => e.Salary)
                    })
                    .ToList();

                return Ok(new
                {
                    totalEmployees = Store.Count,
                    activeEmployees = Store.Count(e => e.Active),
                    averageSalary = Store.Count == 0 ? 0 : Math.Round(Store.Average(e => e.Salary), 2),
                    departments
                });
            }
        }
    }
}
