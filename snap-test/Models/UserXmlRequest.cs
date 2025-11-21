using System.Xml.Serialization;

namespace snap_test.Models
{
    [XmlRoot("UserRequest")]
    public class UserXmlRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

}
