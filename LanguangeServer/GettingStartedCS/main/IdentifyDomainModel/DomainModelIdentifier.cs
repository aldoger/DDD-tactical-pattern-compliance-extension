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
                    classStructureInfo.SetHasIdProperty(true);
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
                        classStructureInfo.SetHasIdProperty(true);
                        return;
                    }
                }

                // Value Object TODO: Buat lebih spesifik algoritma identifikasi
                classStructureInfo.DomainType = DomainType.VALUE_OBJECT;
                classStructureInfo.SetHasIdProperty(false);
                domainModelList.AddDomainModel(classStructureInfo);
                return;
            }

        }
    }
}
