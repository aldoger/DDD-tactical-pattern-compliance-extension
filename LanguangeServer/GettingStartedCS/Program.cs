using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.main.CodeValidation;
using GettingStartedCS.main.Identifier;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                    public class Address
                    {
                        public string Street { get; }
                        public string City { get; }
                        public string Province { get; }
                        public string PostalCode { get; }
                        public string Country { get; }
 
                        // Validation lives here so every instance is always valid.
                        private Address(string street, string city, string province, string postalCode, string country)
                        {
                            if (string.IsNullOrWhiteSpace(street))
                                throw new ArgumentException(""Street is required."", nameof(street));
                            if (string.IsNullOrWhiteSpace(city))
                                throw new ArgumentException(""City is required."", nameof(city));
                            if (string.IsNullOrWhiteSpace(province))
                                throw new ArgumentException(""Province is required."", nameof(province));
                            if (string.IsNullOrWhiteSpace(postalCode))
                                throw new ArgumentException(""Postal code is required."", nameof(postalCode));
                            if (string.IsNullOrWhiteSpace(country))
                                throw new ArgumentException(""Country is required."", nameof(country));
 
                            Street = street.Trim();
                            City = city.Trim();
                            Province = province.Trim();
                            PostalCode = postalCode.Trim();
                            Country = country.Trim();
                        }
 
                        // ---------- creational ----------
                        public static Address Create(string street, string city, string province, string postalCode, string country)
                        {
                            return new Address(street, city, province, postalCode, country);
                        }
 
                        // ""Mutation"" returns a new value instead of changing this one.
                        public Address WithStreet(string street)
                        {
                            return new Address(street, City, Province, PostalCode, Country);
                        }
 
                        public Address WithCity(string city)
                        {
                            return new Address(Street, city, Province, PostalCode, Country);
                        }
 
                        public Address WithProvince(string province)
                        {
                            return new Address(Street, City, province, PostalCode, Country);
                        }
 
                        public Address WithPostalCode(string postalCode)
                        {
                            return new Address(Street, City, Province, postalCode, Country);
                        }
 
                        public Address WithCountry(string country)
                        {
                            return new Address(Street, City, Province, PostalCode, country);
                        }
 
                        // ---------- accessors ----------
                        public bool IsInCountry(string country)
                        {
                            return string.Equals(Country, country?.Trim(), StringComparison.OrdinalIgnoreCase);
                        }
 
                        public bool IsInCity(string city)
                        {
                            return string.Equals(City, city?.Trim(), StringComparison.OrdinalIgnoreCase);
                        }
 
                        public string ToSingleLine()
                        {
                            return $""{Street}, {City}, {Province} {PostalCode}, {Country}"";
                        }
 
                        public override string ToString() => ToSingleLine();
                    }
                }

                public class Human
                {
                    public Guid Id { get; }
                    public string Name { get; private set; }
                    public int Age { get; private set; }
                    public Guid? ResidenceId { get; private set; }
 
                    public Human(Guid id, string name, int age)
                    {
                        Id = id;
                        Name = name;
                        Age = age;
                    }
 
                    public string Introduce()
                    {
                        return $""Hello, my name is {Name} and I am {Age} years old."";
                    }
 
                    public bool IsAdult()
                    {
                        return Age >= 18;
                    }
 
                    public bool HasResidence()
                    {
                        return ResidenceId.HasValue;
                    }
 
                    public void UpdateProfile(string name, int age)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            throw new ArgumentException(""Name is required."", nameof(name));
                        if (age < 0)
                            throw new ArgumentOutOfRangeException(nameof(age));
 
                        Name = name;
                        Age = age;
                    }
 
                    public void AssignResidence(Guid residenceId)
                    {
                        ResidenceId = residenceId;
                    }
 
                    public void ClearResidence()
                    {
                        ResidenceId = null;
                    }
                }
 
                public class Residence
                {
                    private readonly HashSet<Guid> _residentIds = new();
 
                    public Guid Id { get; }
                    public Address Address { get; private set; }
                    public int Capacity { get; }
                    public Guid? HeadOfHouseholdId { get; private set; }
                    public IReadOnlyCollection<Guid> ResidentIds => _residentIds;
 
                    public Residence(Guid id, Address address, int capacity)
                    {
                        if (capacity <= 0)
                            throw new ArgumentOutOfRangeException(nameof(capacity), ""Capacity must be greater than zero."");
 
                        Id = id;
                        Address = address ?? throw new ArgumentNullException(nameof(address));
                        Capacity = capacity;
                    }
 
                    public int ResidentCount()
                    {
                        return _residentIds.Count;
                    }
 
                    public bool IsEmpty()
                    {
                        return _residentIds.Count == 0;
                    }
 
                    public bool IsFull()
                    {
                        return _residentIds.Count >= Capacity;
                    }
 
                    public bool HasResident(Guid humanId)
                    {
                        return _residentIds.Contains(humanId);
                    }
 
                    public bool IsHead(Guid humanId)
                    {
                        return HeadOfHouseholdId == humanId;
                    }
 
                    public void ChangeAddress(Address newAddress)
                    {
                        Address = newAddress ?? throw new ArgumentNullException(nameof(newAddress));
                    }
 
                    // The first resident automatically becomes the head of the household.
                    public void AddResident(Guid humanId)
                    {
                        if (IsFull())
                            throw new InvalidOperationException(""Residence is full."");
                        if (!_residentIds.Add(humanId))
                            throw new InvalidOperationException(""Human already lives in this residence."");
 
                        HeadOfHouseholdId ??= humanId;
                    }
 
                    public void RemoveResident(Guid humanId)
                    {
                        if (!HasResident(humanId))
                            throw new InvalidOperationException(""Human does not live in this residence."");
                        if (IsHead(humanId) && _residentIds.Count > 1)
                            throw new InvalidOperationException(""Appoint a new head of household before the current head leaves."");
 
                        _residentIds.Remove(humanId);
 
                        if (IsHead(humanId))
                            HeadOfHouseholdId = null;
                    }
 
                    public void AssignHead(Guid humanId)
                    {
                        if (!HasResident(humanId))
                            throw new InvalidOperationException(""Head of household must be a resident."");
 
                        HeadOfHouseholdId = humanId;
                    }
                }

                public class ResidencyService
                {
                    public void MoveIn(Human human, Residence residence)
                    {
                        Require(human, residence);
 
                        if (human.HasResidence())
                            throw new InvalidOperationException(""Human already has a residence. Use Transfer instead."");
                        if (residence.IsFull())
                            throw new InvalidOperationException(""Residence is full."");
                        if (residence.IsEmpty() && !human.IsAdult())
                            throw new InvalidOperationException(""The first resident (head of household) must be an adult."");
 
                        residence.AddResident(human.Id);
                        human.AssignResidence(residence.Id);
                    }
 
                    public void MoveOut(Human human, Residence residence)
                    {
                        Require(human, residence);
 
                        if (human.ResidenceId != residence.Id)
                            throw new InvalidOperationException(""Human does not live in this residence."");
 
                        // Throws if the head tries to leave while others remain; nothing has changed yet.
                        residence.RemoveResident(human.Id);
                        human.ClearResidence();
                    }
 
                    public void Transfer(Human human, Residence from, Residence to)
                    {
                        Require(human, from);
                        if (to is null) throw new ArgumentNullException(nameof(to));
 
                        if (from.Id == to.Id)
                            throw new InvalidOperationException(""Source and destination residence are the same."");
                        if (human.ResidenceId != from.Id)
                            throw new InvalidOperationException(""Human does not live in the source residence."");
 
                        // Validate the destination first, so we never leave the human homeless on failure.
                        if (to.IsFull())
                            throw new InvalidOperationException(""Destination residence is full."");
                        if (to.IsEmpty() && !human.IsAdult())
                            throw new InvalidOperationException(""The first resident (head of household) must be an adult."");
 
                        from.RemoveResident(human.Id);
                        to.AddResident(human.Id);
                        human.AssignResidence(to.Id);
                    }
 
                    public void AppointHead(Human human, Residence residence)
                    {
                        Require(human, residence);
 
                        if (!human.IsAdult())
                            throw new InvalidOperationException(""Head of household must be an adult."");
 
                        residence.AssignHead(human.Id);
                    }
 
                    private static void Require(Human human, Residence residence)
                    {
                        if (human is null) throw new ArgumentNullException(nameof(human));
                        if (residence is null) throw new ArgumentNullException(nameof(residence));
                    }
                }


            ";

            ClassStructureInfoList classStructureInfoList = new ClassStructureInfoList();
            var parser = new parser.Parser();
            parser.Parse(input);
            var classes = parser.GetClasses();

            foreach (var classDeclaration in classes)
            {
                var baseClassName = parser.GetBaseClassNameType(classDeclaration);
                var classInfo = new ClassStructureInfo(classDeclaration.Identifier.Text, baseClassName, classDeclaration);

                classInfo.SetHasConstructor(parser.HasConstructor(classDeclaration));
                classInfo.SetIsStatic(parser.IsClassStatic(classDeclaration));
                foreach (var method in parser.GetMethods(classDeclaration))
                {
                    var methodInfo = new MethodStructureInfo(method.Identifier.Text, method);
                    methodInfo.SetIsStatic(parser.IsMethodStatic(method));
                    var returnType = parser.GetMethodReturnType(method);
                    methodInfo.SetReturnType(returnType);
                    classInfo.AddMethod(methodInfo);
                }
                classStructureInfoList.AddClass(classInfo);
            }

            DomainModelList domainModelList = new DomainModelList();

            foreach(var c in classStructureInfoList.Classes)
            {
               DomainModelIdentifier.IdentifyDomainModels(c, domainModelList);
            }

            // TODO: make algorithm to identify publisher and subscriber from domain events

            // TODO: make algorithm to identify aggregates by analyzing the relationships between domain models and other domain models

            Util.Util.PrintStereotypes(classStructureInfoList.Classes);

            foreach (var domainModel in domainModelList.DomainModels)
            {
                Console.WriteLine($"Domain Model: {domainModel.Name} ({domainModel.DomainType})");
                var validator = ValidatorFactory.GetValidator(domainModel, parser);
                if (validator != null)
                {
                    ViolationList vlist = new ViolationList();
                    validator.Validate(domainModel, vlist, domainModelList);
                    if (vlist.Violations.Count > 0)
                    {
                        Console.WriteLine($"Violations for {domainModel.Name} ({domainModel.DomainType}):");
                        foreach (var violation in vlist.Violations)
                        {
                            Console.WriteLine($"- {violation.Constraint}: {violation.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No violations for {domainModel.Name} ({domainModel.DomainType}).");
                    }
                }
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
