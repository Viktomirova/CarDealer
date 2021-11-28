using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

using CarDealer.Data;
using CarDealer.DTO.ExportDto;
using CarDealer.DTO.ImportDto;
using CarDealer.Models;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            CarDealerContext dbContext = new CarDealerContext();

            //dbContext.Database.EnsureDeleted();
            //dbContext.Database.EnsureCreated();

            string inputXmlSuppliers = File.ReadAllText("../../../Datasets/suppliers.xml");
            Console.WriteLine(ImportSuppliers(dbContext, inputXmlSuppliers));

            string inputXmlParts = File.ReadAllText("../../../Datasets/parts.xml");
            Console.WriteLine(ImportParts(dbContext, inputXmlParts));

            string inputXmlCars = File.ReadAllText("../../../Datasets/cars.xml");
            Console.WriteLine(ImportCars(dbContext, inputXmlCars));

            string inputXmlCustomers = File.ReadAllText("../../../Datasets/customers.xml");
            Console.WriteLine(ImportCustomers(dbContext, inputXmlCustomers));

            string inputXmlSales = File.ReadAllText("../../../Datasets/sales.xml");
            Console.WriteLine(ImportSales(dbContext, inputXmlSales));

            Console.WriteLine(GetCarsWithDistance(dbContext));

            Console.WriteLine(GetCarsFromMakeBmw(dbContext));

            Console.WriteLine(GetSalesWithAppliedDiscount(dbContext));

            Console.WriteLine(GetLocalSuppliers(dbContext));

            Console.WriteLine(GetCarsWithTheirListOfParts(dbContext));

            Console.WriteLine(GetTotalSalesByCustomer(dbContext));
        }

        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Suppliers");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSupplierDto[]), xmlRoot);

            using StringReader stringReader = new StringReader(inputXml);

            ImportSupplierDto[] supplierDtos = (ImportSupplierDto[])xmlSerializer.Deserialize(stringReader);

            ICollection<Supplier> suppliers = new HashSet<Supplier>();

            foreach (ImportSupplierDto supplierDto in supplierDtos)
            {
                Supplier supplier = new Supplier()
                {
                    Name = supplierDto.Name,
                    IsImporter = bool.Parse(supplierDto.Importer)
                };

                suppliers.Add(supplier);
            }

            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Count}";
        }

        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Parts");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportPartsDto[]), xmlRoot);

            using StringReader stringReader = new StringReader(inputXml);

            ImportPartsDto[] partsDtos = (ImportPartsDto[])xmlSerializer.Deserialize(stringReader);

            ICollection<Part> parts = new HashSet<Part>();

            foreach (ImportPartsDto partsDto in partsDtos)
            {
                Supplier supplier = context
                                    .Suppliers
                                    .Find(partsDto.SupplierId);

                if (supplier == null)
                {
                    continue;
                }

                Part part = new Part()
                {
                    Name = partsDto.Name,
                    Price = decimal.Parse(partsDto.Price),
                    Quantity = partsDto.Quantity,
                    Supplier = supplier
                };

                parts.Add(part);
            }

            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Count}";
        }

        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Cars");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCarsDto[]), xmlRoot);

            using StringReader stringReader = new StringReader(inputXml);

            ImportCarsDto[] carDtos = (ImportCarsDto[])xmlSerializer.Deserialize(stringReader);

            ICollection<Car> cars = new HashSet<Car>();

            foreach (ImportCarsDto carDto in carDtos)
            {
                Car car = new Car()
                {
                    Make = carDto.Make,
                    Model = carDto.Model,
                    TravelledDistance = carDto.TravelledDistance
                };

                ICollection<PartCar> currentCarParts = new HashSet<PartCar>();

                foreach (int partId in carDto.Parts.Select(p => p.Id).Distinct())
                {
                    Part part = context.Parts.Find(partId);

                    if (part == null)
                    {
                        continue;
                    }

                    PartCar partCar = new PartCar()
                    {
                        Car = car,
                        Part = part
                    };

                    currentCarParts.Add(partCar);
                }

                car.PartCars = currentCarParts;
                cars.Add(car);
            }

            context.Cars.AddRange(cars);
            context.SaveChanges();

            return $"Successfully imported {cars.Count}";
        }

        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Customers");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCustomersDto[]), xmlRoot);

            using StringReader stringReader = new StringReader(inputXml);

            ImportCustomersDto[] customersDtos = (ImportCustomersDto[])xmlSerializer.Deserialize(stringReader);

            ICollection<Customer> customers = new HashSet<Customer>();

            foreach (ImportCustomersDto customersDto in customersDtos)
            {
                Customer customer = new Customer()
                {
                    Name = customersDto.Name,
                    BirthDate = DateTime.Parse(customersDto.BirthDate, CultureInfo.InvariantCulture),
                    IsYoungDriver = bool.Parse(customersDto.IsYoungDriver)
                };

                customers.Add(customer);
            }

            context.Customers.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Count}";

        }

        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Sales");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSalesDto[]), xmlRoot);

            using StringReader stringReader = new StringReader(inputXml);

            ImportSalesDto[] salesDtos = (ImportSalesDto[])xmlSerializer.Deserialize(stringReader);

            ICollection<Sale> sales = new HashSet<Sale>();

            foreach (ImportSalesDto salesDto in salesDtos)
            {
                Car car = context.Cars.Find(salesDto.CarId);

                if (car == null)
                {
                    continue;
                }

                Sale sale = new Sale()
                {
                    CarId = salesDto.CarId,
                    CustomerId = salesDto.CustomerId,
                    Discount = decimal.Parse(salesDto.Discount)
                };

                sales.Add(sale);
            }

            context.Sales.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Count}";

        }

        public static string GetCarsWithDistance(CarDealerContext context)
        {
            StringBuilder sb = new StringBuilder();
            using StringWriter stringWriter = new StringWriter(sb);

            XmlRootAttribute xmlRoot = new XmlRootAttribute("cars");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportCarsWithDistanceDto[]), xmlRoot);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, String.Empty);

            ExportCarsWithDistanceDto[] carsDtos = context
                                                   .Cars
                                                   .Where(c => c.TravelledDistance > 2000000)
                                                   .OrderBy(c => c.Make)
                                                   .ThenBy(c => c.Model)
                                                   .Take(10)
                                                   .Select(c => new ExportCarsWithDistanceDto()
                                                   {
                                                       Make = c.Make,
                                                       Model = c.Model,
                                                       TravelledDistance = c.TravelledDistance.ToString()
                                                   })
                                                   .ToArray();
            xmlSerializer.Serialize(stringWriter, carsDtos, namespaces);

            return sb.ToString().TrimEnd();
        }

        public static string GetCarsFromMakeBmw(CarDealerContext context)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter stringWriter = new StringWriter(sb);

            XmlRootAttribute xmlRoot = new XmlRootAttribute("cars");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportCarsFromMakeBmwDto[]), xmlRoot);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, String.Empty);

            ExportCarsFromMakeBmwDto[] carsFromMake = context
                                                      .Cars
                                                      .Where(c => c.Make == "BMW")
                                                      .OrderBy(c => c.Model)
                                                      .ThenByDescending(c => c.TravelledDistance)
                                                      .Select(c => new ExportCarsFromMakeBmwDto()
                                                      {
                                                          Id = c.Id.ToString(),
                                                          Model = c.Model,
                                                          TravelledDistance = c.TravelledDistance.ToString()
                                                      })
                                                      .ToArray();


            xmlSerializer.Serialize(stringWriter, carsFromMake, namespaces);

            return stringWriter.ToString().TrimEnd();

        }

        public static string GetLocalSuppliers(CarDealerContext context)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("suppliers");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportLocalSuppliersDto[]), xmlRoot);
            StringWriter stringWriter = new StringWriter();
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            ExportLocalSuppliersDto[] localSuppliers = context
                                                  .Suppliers
                                                  .Where(s => s.IsImporter == false)
                                                  .Select(s => new ExportLocalSuppliersDto()
                                                  {
                                                      Id = s.Id.ToString(),
                                                      Name = s.Name,
                                                      PartsCount = s.Parts.Count.ToString()
                                                  }).ToArray();

            xmlSerializer.Serialize(stringWriter, localSuppliers, namespaces);

            return stringWriter.ToString().TrimEnd();
        }

        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("cars");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportCarsWithListOfPartsDto[]), xmlRoot);
            StringWriter stringWriter = new StringWriter();
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            ExportCarsWithListOfPartsDto[] cars = context
                                                .Cars
                                                .Select(c => new ExportCarsWithListOfPartsDto()
                                                {
                                                    Make = c.Make,
                                                    Model = c.Model,
                                                    TravelledDistance = c.TravelledDistance,
                                                    Parts = c.PartCars.Select(p => new ExportPartCarsDto()
                                                    {
                                                        Name = p.Part.Name,
                                                        Price = p.Part.Price
                                                    })
                                                             .OrderByDescending(p => p.Price)
                                                             .ToArray()
                                                })
                                                .OrderByDescending(x => x.TravelledDistance)
                                                .ThenBy(x => x.Model)
                                                .Take(5)
                                                .ToArray();

            xmlSerializer.Serialize(stringWriter, cars, namespaces);

            return stringWriter.ToString().TrimEnd();
        }

        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            ExportSalesCustomersDto[] customers = context
                                            .Customers
                                            .Where(c => c.Sales.Any())
                                            .Select(c => new ExportSalesCustomersDto
                                            {
                                                Name = c.Name,
                                                BoughtCars = c.Sales.Count(),
                                                SpentMoney = c.Sales
                                                              .SelectMany(s => s.Car.PartCars)
                                                              .Sum(p => p.Part.Price)

                                            })
                                            .OrderByDescending(c => c.SpentMoney)
                                            .ToArray();

            XmlSerializer serializer = new XmlSerializer(typeof(ExportSalesCustomersDto[]), new XmlRootAttribute("customers"));
            StringBuilder sb = new StringBuilder();
            using StringWriter writer = new StringWriter(sb);

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);
            serializer.Serialize(writer, customers, namespaces);

            return sb.ToString().TrimEnd();
        }

        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter stringWriter = new StringWriter(sb);

            XmlRootAttribute xmlRoot = new XmlRootAttribute("sales");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportSalesWithDiscountDto[]), xmlRoot);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, String.Empty);

            ExportSalesWithDiscountDto[] salesDtos = context
                                                     .Sales
                                                     .Select(s => new ExportSalesWithDiscountDto()
                                                     {
                                                         Car = new ExportSalesCarsDto()
                                                         {
                                                             Make = s.Car.Make,
                                                             Model = s.Car.Model,
                                                             TravelledDistance = s.Car.TravelledDistance.ToString()
                                                         },
                                                         Discount = s.Discount.ToString(CultureInfo.InvariantCulture),
                                                         CustomerName = s.Customer.Name,
                                                         Price = s.Car.PartCars.Sum(pc => pc.Part.Price).ToString(CultureInfo.InvariantCulture),
                                                         PriceWithDiscount =
                                                             (s.Car.PartCars.Sum(pc => pc.Part.Price) -
                                                              s.Car.PartCars.Sum(pc => pc.Part.Price) * s.Discount / 100)
                                                             .ToString(CultureInfo.InvariantCulture),
                                                     })
                                                     .ToArray();

            xmlSerializer.Serialize(stringWriter, salesDtos, namespaces);

            return sb.ToString().TrimEnd();
        }
    }
}