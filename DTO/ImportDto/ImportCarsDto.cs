using CarDealer.Models;

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CarDealer.DTO.ImportDto
{
    [XmlType("Car")]
    public class ImportCarsDto
    {
        [XmlElement("make")]
        public string Make { get; set; }

        [XmlElement("model")]
        public string Model { get; set; }

        [XmlElement("TraveledDistance")]
        public int TravelledDistance { get; set; }

        [XmlArray("parts")]
        public ImportPartCarsDto[] Parts { get; set; }
    }
}
