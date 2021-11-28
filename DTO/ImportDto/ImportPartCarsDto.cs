using System.Xml.Serialization;

namespace CarDealer.DTO.ImportDto
{
    [XmlType("partId")]
    public class ImportPartCarsDto
    {
        [XmlAttribute("id")]
        public int Id { get; set; }
    }
}
