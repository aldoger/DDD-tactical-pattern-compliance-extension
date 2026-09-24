using GettingStartedCS.main.ClassStructureInfo;
using GettingStartedCS.main.CodeValidation;
using GettingStartedCS.main.IdentifyDomainModel;
using Microsoft.Build.Locator;
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
                    class Human
                    {
                        public string Id { get; set; }
                        public string Name { get; set; }
                        public int Age { get; set; }

                        public Human(string name, int age)
                        {
                            Name = name;
                            Age = age;
                        }

                        public string Introduce()
                        {
                            return $""Hello, my name is {Name} and I am {Age} years old."";
                        }
                    }";

            // SyntaxTree tree = CSharpSyntaxTree.ParseText(input);

            DomainModelList domainModelList = new DomainModelList();
            var parser = new parser.Parser();
            parser.Parse(input);
            var classes = parser.GetClasses();

            foreach (var classDeclaration in classes)
            {
                var classInfo = new ClassStructureInfo(classDeclaration.Identifier.Text);
                var methods = parser.GetMethods(classDeclaration);
                foreach (var method in methods)
                {
                    var classMethodInfo = new MethodStructureInfo(method.Identifier.Text);
                    classInfo.AddMethod(classMethodInfo);
                }
                var properties = parser.GetProperties(classDeclaration);
                foreach (var property in properties)
                {
                    var classPropertyInfo = new PropertyStructureInfo(property.Identifier.Text);
                    classInfo.AddProperty(classPropertyInfo);
                }
                DomainModelIdentifier.IdentifyDomainModels(classDeclaration, classInfo, domainModelList);
            }

            ViolationList violationList = new ViolationList();

            foreach(var domainModel in domainModelList.DomainModels)
            {
                switch(domainModel.DomainType)
                {
                    case DomainType.ENTITY:
                        Console.WriteLine($"Entity: {domainModel.ClassName}");
                        DomainObjectValidate validator = ValidatorFactory.GetValidator(DomainType.ENTITY);
                        validator.Validate(domainModel, violationList);
                        break;
                    default:
                        Console.WriteLine($"Unknown Domain Type: {domainModel.ClassName}");
                        break;
                }
            }

            foreach(var violation in violationList.Violations)
            {
                Console.WriteLine($"Violation Code: {violation.Constraint}, Message: {violation.Message}");
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
