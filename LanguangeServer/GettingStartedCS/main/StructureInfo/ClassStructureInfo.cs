using GettingStartedCS.main.Identifier;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.StructureInfo
{
    public class MethodStructureInfo
    {
        public string MethodName;
        public MethodDeclarationSyntax MethodDeclaration;
        public bool IsStatic;
        public string? ReturnType;
        public List<MethodStereotype> Stereotype =  new();
        public MethodFacts? Facts;
        public void SetIsStatic(bool isStatic)
        {
            IsStatic = isStatic;
        }
        public void SetReturnType(string? returnType)
        {
            ReturnType = returnType;
        }
        public void AddStereotype(MethodStereotype stereotype)
        {
            if (!Stereotype.Contains(stereotype))
                Stereotype.Add(stereotype);
        }
        public MethodStructureInfo(string methodName, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            MethodName = methodName;
            MethodDeclaration = methodDeclarationSyntax;
        }

        public void Analyze(ClassMemberContext cls, MethodAnalysisOptions? options = null)
        {
            Facts = MethodFactsExtractor.Extract(MethodDeclaration, cls, options);
            SetIsStatic(Facts.IsStatic);
            SetReturnType(Facts.ReturnType);

            Stereotype.Clear();
            foreach (var s in MethodStereotypeClassifier.Classify(Facts))
                AddStereotype(s);
        }
    }

    public class ClassStructureInfo
    {
        public string ClassName;
        public ClassDeclarationSyntax ClassDeclaration; 
        public string? BaseClassName;
        public bool HasConstructor;
        public bool IsStatic;
        public int TotalAccesor;
        public int TotalMutator;
        public int TotalCollaborator;
        public bool HasFactoryMethods;
        public List<MethodStructureInfo> Methods;
        public List<ClassStereotype> Stereotype = new();
        public void AddMethod(MethodStructureInfo method)
        {
            Methods.Add(method);
        }
        public void SetHasConstructor(bool hasConstructor)
        {
            HasConstructor = hasConstructor;
        }
        public void SetBaseClassName(string baseClassName)
        {
            BaseClassName = baseClassName;
        }
        public void SetIsStatic(bool isStatic)
        {
            IsStatic = isStatic;
        }
        public void AddStereotype(ClassStereotype stereotype)
        {
            Stereotype.Add(stereotype);
        }
        public ClassStructureInfo(string className, string? baseClass, ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassName = className;
            ClassDeclaration = classDeclarationSyntax;
            BaseClassName = baseClass;
            Methods = new List<MethodStructureInfo>();
        }
    }

    public class ClassStructureInfoList
    {
        public List<ClassStructureInfo> Classes { get; } = new List<ClassStructureInfo>();
        public void AddClass(ClassStructureInfo classInfo)
        {
            Classes.Add(classInfo);
        }
        public bool ContainsClass(string className)
        {
            return Classes.Any(c => c.ClassName == className);
        }
        public ClassStructureInfo? GetClass(string className)
        {
            return Classes.FirstOrDefault(c => c.ClassName == className);
        }
    }
}
