using GettingStartedCS.main.ClassStructureInfo;
using GettingStartedCS.main.CodeValidation;
using GettingStartedCS.main.IdentifyDomainModel;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GettingStartedCS
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string input = @"
                public class Human
                {
                    public string Id { get; set; }
                    public string Name { get; set; }
                    public int Age { get; set; }

                    public Human(string id, string name, int age)
                    {
                        Id = id;
                        Name = name;
                        Age = age;
                    }

                    public string Introduce()
                    {
                        return $""Hello, my name is {Name} and I am {Age} years old."";
                    }
                }

                public abstract class Entity
                {
                    public Guid Id { get; }

                    protected Entity(Guid id)
                    {
                        Id = id;
                    }
                }

                public class Order : Entity
                {
                    public string CustomerName { get; private set; }
                    public decimal Total { get; private set; }

                    public Order(Guid id, string customerName, decimal total) : base(id)
                    {
                        CustomerName = customerName;
                        Total = total;
                    }

                    public void ApplyDiscount(decimal percentage)
                    {
                        Total -= Total * (percentage / 100m);
                    }
                }

                public class Address
                {
                    public string Street { get; }
                    public string City { get; }
                    public string PostalCode { get; }

                    public Address(string street, string city, string postalCode)
                    {
                        Street = street;
                        City = city;
                        PostalCode = postalCode;
                    }

                    public override bool Equals(object obj)
                    {
                        if (obj is not Address other) return false;
                        return Street == other.Street
                            && City == other.City
                            && PostalCode == other.PostalCode;
                    }

                    public override int GetHashCode()
                    {
                        return HashCode.Combine(Street, City, PostalCode);
                    }

                    public string GetFullAddress()
                    {
                        return $""{Street}, {City}, {PostalCode}"";
                    }
                }
            }";

            DomainModelList domainModelList = new DomainModelList();
            var parser = new parser.Parser();
            parser.Parse(input);
            var classes = parser.GetClasses();

            foreach (var classDeclaration in classes)
            {
                var classInfo = new ClassStructureInfo(classDeclaration.Identifier.Text);
                var classBaseType = parser.GetClassBaseType(classDeclaration);
                classInfo.SetBaseClassName(classBaseType);
                var properties = parser.GetProperties(classDeclaration);
                foreach (var property in properties)
                {
                    var isReadOnly = parser.IsPropertyImmutable(property);
                    var propertyType = parser.GetPropertyType(property);
                    var classProperty = new PropertyStructureInfo(property.Identifier.Text, propertyType, isReadOnly);
                    classInfo.AddProperty(classProperty);
                }
                DomainModelIdentifier.IdentifyDomainModels(classDeclaration, classInfo, domainModelList);
            }

            ViolationList violationList = new ViolationList();

            foreach(var domainModel in domainModelList.DomainModels)
            {
                switch(domainModel.DomainType)
                {
                    case DomainType.ENTITY:
                        DomainObjectValidate validator = ValidatorFactory.GetValidator(DomainType.ENTITY);
                        validator.Validate(domainModel, violationList);
                        break;
                    case DomainType.VALUE_OBJECT:
                        DomainObjectValidate valueObjectValidator = ValidatorFactory.GetValidator(DomainType.VALUE_OBJECT);
                        valueObjectValidator.Validate(domainModel, violationList);
                        break;
                    default:
                        Console.WriteLine($"Unknown Domain Type: {domainModel.ClassName}");
                        break;
                }
            }

            foreach(var violation in violationList.Violations)
            {
                Console.WriteLine($"" +
                    $"Class: {violation.className}, " +
                    $"Violation Code: {violation.Constraint}, " +
                    $"Message: {violation.Message}");
            }
        }
        

        

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
