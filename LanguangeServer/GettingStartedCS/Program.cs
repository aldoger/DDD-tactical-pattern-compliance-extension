using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.main.CodeValidation;
using GettingStartedCS.main.IdentifyDomainModel;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

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

                public static class HumanFactory 
                {

                    public static Human Create(string name, int age)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            throw new ArgumentException(""Name is required."", nameof(name));
                        if (age < 0)
                            throw new ArgumentOutOfRangeException(nameof(age));

                        return new Human(Guid.NewGuid().ToString(), name, age);
                    }
                }   

                public static class AddressFactory
                {
                    public static Address Create(string street, string city, string postalCode)
                    {
                        if (string.IsNullOrWhiteSpace(street))
                            throw new ArgumentException(""Street is required."", nameof(street));
                        if (string.IsNullOrWhiteSpace(city))
                            throw new ArgumentException(""City is required."", nameof(city));
                        if (string.IsNullOrWhiteSpace(postalCode))
                            throw new ArgumentException(""Postal code is required."", nameof(postalCode));

                        return new Address(street, city, postalCode);
                    }
                }
                
                public interface IDiscountPolicy
                {
                    bool IsEligible(Human customer);
                    decimal GetPercentage(Human customer);
                }

                public class SeniorDiscountPolicy : IDiscountPolicy
                {
                    private const int SeniorAgeThreshold = 60;
                    private const decimal SeniorDiscountPercentage = 10m;

                    public bool IsEligible(Human customer) => customer.Age >= SeniorAgeThreshold;

                    public decimal GetPercentage(Human customer) =>
                        IsEligible(customer) ? SeniorDiscountPercentage : 0m;
                }

                public class OrderDiscountService
                {
                    private readonly IDiscountPolicy _discountPolicy;

                    public OrderDiscountService(IDiscountPolicy discountPolicy)
                    {
                        _discountPolicy = discountPolicy ?? throw new ArgumentNullException(nameof(discountPolicy));
                    }

                    public void ApplyCustomerDiscount(Order order, Human customer)
                    {
                        if (order is null) throw new ArgumentNullException(nameof(order));
                        if (customer is null) throw new ArgumentNullException(nameof(customer));

                        if (!_discountPolicy.IsEligible(customer))
                            return;

                        order.ApplyDiscount(_discountPolicy.GetPercentage(customer));
                    }
                }

                public interface IHumanRepository
                {
                    Task<Human?> GetByIdAsync(string id);
                    Task<IReadOnlyList<Human>> GetAllAsync();
                    Task AddAsync(Human human);
                    Task UpdateAsync(Human human);
                    Task DeleteAsync(string id);
                }

                public interface IOrderRepository
                {
                    Task<Order?> GetByIdAsync(Guid id);
                    Task<IReadOnlyList<Order>> GetByCustomerNameAsync(string customerName);
                    Task AddAsync(Order order);
                    Task UpdateAsync(Order order);
                    Task DeleteAsync(Guid id);
                }

                public class InMemoryHumanRepository : IHumanRepository
                {
                    private readonly Dictionary<string, Human> _store = new();

                    public Task<Human?> GetByIdAsync(string id) =>
                        Task.FromResult(_store.TryGetValue(id, out var human) ? human : null);

                    public Task<IReadOnlyList<Human>> GetAllAsync() =>
                        Task.FromResult<IReadOnlyList<Human>>(_store.Values.ToList());

                    public Task AddAsync(Human human)
                    {
                        if (!_store.TryAdd(human.Id, human))
                            throw new InvalidOperationException($""Human {human.Id} already exists."");
                        return Task.CompletedTask;
                    }

                    public Task UpdateAsync(Human human)
                    {
                        if (!_store.ContainsKey(human.Id))
                            throw new KeyNotFoundException($""Human {human.Id} not found."");
                        _store[human.Id] = human;
                        return Task.CompletedTask;
                    }

                    public Task DeleteAsync(string id)
                    {
                        _store.Remove(id);
                        return Task.CompletedTask;
                    }
                }

                public class InMemoryOrderRepository : IOrderRepository
                {
                    private readonly Dictionary<Guid, Order> _store = new();

                    public Task<Order?> GetByIdAsync(Guid id) =>
                        Task.FromResult(_store.TryGetValue(id, out var order) ? order : null);

                    public Task<IReadOnlyList<Order>> GetByCustomerNameAsync(string customerName) =>
                        Task.FromResult<IReadOnlyList<Order>>(
                            _store.Values.Where(o => o.CustomerName == customerName).ToList());

                    public Task AddAsync(Order order)
                    {
                        if (!_store.TryAdd(order.Id, order))
                            throw new InvalidOperationException($""Order {order.Id} already exists."");
                        return Task.CompletedTask;
                    }

                    public Task UpdateAsync(Order order)
                    {
                        if (!_store.ContainsKey(order.Id))
                            throw new KeyNotFoundException($""Order {order.Id} not found."");
                        _store[order.Id] = order;
                        return Task.CompletedTask;
                    }

                    public Task DeleteAsync(Guid id)
                    {
                        _store.Remove(id);
                        return Task.CompletedTask;
                    }
                }
            ";

            ClassStructureInfoList classStructureInfoList = new ClassStructureInfoList();
            var parser = new parser.Parser();
            parser.Parse(input);
            var classes = parser.GetClasses();

            foreach (var classDeclaration in classes)
            {
                var classInfo = new ClassStructureInfo(classDeclaration.Identifier.Text);

                if(parser.HasConstructor(classDeclaration))
                {
                    classInfo.SetHasConstructor(true);
                }

                classInfo.SetIsStatic(parser.IsClassStatic(classDeclaration));

                string baseClassName = parser.GetClassBaseType(classDeclaration);
                if (!string.IsNullOrEmpty(baseClassName))
                {
                    classInfo.SetBaseClassName(baseClassName);
                }

                bool isImmutable = parser.IsClassImmutable(classDeclaration);
                classInfo.SetIsImmutable(isImmutable);

                foreach (var property in parser.GetProperties(classDeclaration))
                {
                    var propertyInfo = new PropertyStructureInfo(property.Identifier.Text, parser.GetPropertyType(property), parser.IsPropertyImmutable(property));
                    classInfo.AddProperty(propertyInfo);
                }
                foreach (var method in parser.GetMethods(classDeclaration))
                {
                    var methodInfo = new MethodStructureInfo(method.Identifier.Text, parser.GetMethodReturnType(method));
                    methodInfo.SetIsStatic(parser.IsMethodStatic(method));
                    var parameters = parser.GetMethodsParameter(method);
                    methodInfo.SetParameters(parameters);
                    classInfo.AddMethod(methodInfo);
                }
                classStructureInfoList.AddClass(classInfo);
            }

            DomainModelList domainModelList = new DomainModelList();

            foreach(var c in classStructureInfoList.Classes)
            {
               DomainModelIdentifier.IdentifyDomainModels(c, classStructureInfoList, domainModelList);
            }

            // TODO: make algorithm to identify aggregates by analyzing the relationships between domain models and other domain models

            foreach (var domainModel in domainModelList.DomainModels)
            {
                Console.WriteLine($"Domain Model: {domainModel.Name} ({domainModel.DomainType})");
                var validator = ValidatorFactory.GetValidator(domainModel);
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
