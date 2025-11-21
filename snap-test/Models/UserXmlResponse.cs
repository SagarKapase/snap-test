using System.Xml.Serialization;

namespace snap_test.Models
{
    [XmlRoot("User")]
    public class UserXmlResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
