using System;
using System.Linq;

namespace GettingStartedCS.main.IdentifyDomainModel
{
    using GettingStartedCS.main.ClassStructureInfo;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class DomainModelIdentifier
    {

        public static void IdentifyDomainModels(

            ClassDeclarationSyntax classInfo,
            ClassStructureInfo classStructureInfo,
            DomainModelList domainModelList
        )
        { 
            // Check if the class has any constructors
            var ctors = classInfo.Members.OfType<ConstructorDeclarationSyntax>();
            if (ctors.Any())
            {
                // For Entity, check if it has base class
                if (classStructureInfo.BaseClassName != null
                && classStructureInfo.BaseClassName != "object"
                && classStructureInfo.BaseClassName.Contains("Entity", StringComparison.OrdinalIgnoreCase))
                {
                    classStructureInfo.DomainType = DomainType.ENTITY;
                    domainModelList.AddDomainModel(classStructureInfo);
                    return;
                }

                // For Entity, check if it has an Id
                foreach (var property in classStructureInfo.Properties)
                {
                    if(property.PropertyName.Contains("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        classStructureInfo.DomainType = DomainType.ENTITY;
                        domainModelList.AddDomainModel(classStructureInfo);
                        return;
                    }
                }

                // Value Object
                classStructureInfo.DomainType = DomainType.VALUE_OBJECT;
                domainModelList.AddDomainModel(classStructureInfo);
                return;
            }

        }
    }
}
