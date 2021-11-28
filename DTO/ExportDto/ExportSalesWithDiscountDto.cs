using CarDealer.Models;

using Castle.Core.Resource;

using System.Xml.Serialization;

namespace CarDealer.DTO.ExportDto
{
    [XmlType("sale")]
    public class ExportSalesWithDiscountDto
    {
        [XmlElement("car")]
        public ExportSalesCarsDto Car { get; set; }
        
        [XmlElement("discount")]
        public string Discount { get; set; }

        [XmlElement("customer-name")]
        public string CustomerName { get; set; }

        [XmlElement("price")]
        public string Price { get; set; }

        [XmlElement("price-with-discount")]
        public string PriceWithDiscount { get; set; }

    }
}
