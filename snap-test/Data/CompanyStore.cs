using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for companies (8 records). Employees reference these via companyId.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class CompanyStore
    {
        public static List<Company> Companies = new()
        {
            new Company { Id = 1, Name = "Nimbus Cloud Systems", Industry = "Technology", Founded = 2008, Website = "https://nimbuscloud.example.com", Headquarters = new CompanyHeadquarters { City = "San Francisco", Country = "USA" }, Revenue = 1250000000m, EmployeeCount = 4200 },
            new Company { Id = 2, Name = "Greenleaf Foods", Industry = "Food & Beverage", Founded = 1994, Website = "https://greenleaffoods.example.com", Headquarters = new CompanyHeadquarters { City = "Chicago", Country = "USA" }, Revenue = 860000000m, EmployeeCount = 6100 },
            new Company { Id = 3, Name = "Aurora Health", Industry = "Healthcare", Founded = 2001, Website = "https://aurorahealth.example.com", Headquarters = new CompanyHeadquarters { City = "Boston", Country = "USA" }, Revenue = 2100000000m, EmployeeCount = 9800 },
            new Company { Id = 4, Name = "Vertex Financial Group", Industry = "Finance", Founded = 1987, Website = "https://vertexfinancial.example.com", Headquarters = new CompanyHeadquarters { City = "London", Country = "United Kingdom" }, Revenue = 3400000000m, EmployeeCount = 12500 },
            new Company { Id = 5, Name = "Bluewave Logistics", Industry = "Logistics", Founded = 2012, Website = "https://bluewave.example.com", Headquarters = new CompanyHeadquarters { City = "Rotterdam", Country = "Netherlands" }, Revenue = 540000000m, EmployeeCount = 2300 },
            new Company { Id = 6, Name = "Pixelcraft Studios", Industry = "Media", Founded = 2015, Website = "https://pixelcraft.example.com", Headquarters = new CompanyHeadquarters { City = "Montreal", Country = "Canada" }, Revenue = 120000000m, EmployeeCount = 450 },
            new Company { Id = 7, Name = "Solstice Energy", Industry = "Energy", Founded = 1999, Website = "https://solstice-energy.example.com", Headquarters = new CompanyHeadquarters { City = "Berlin", Country = "Germany" }, Revenue = 1780000000m, EmployeeCount = 5400 },
            new Company { Id = 8, Name = "Kitsune Robotics", Industry = "Manufacturing", Founded = 2010, Website = "https://kitsune-robotics.example.com", Headquarters = new CompanyHeadquarters { City = "Tokyo", Country = "Japan" }, Revenue = 960000000m, EmployeeCount = 3100 }
        };
    }
}
