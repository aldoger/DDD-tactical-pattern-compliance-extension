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
            DomainModelList domainModelList)
        {
            // Check if the class has any constructors
            var ctors = classInfo.Members.OfType<ConstructorDeclarationSyntax>();
            if (ctors.Any())
            {
                // Look up for the Properties and Methods of the class
                var hasReadonlyProperties = classStructureInfo.Properties.All(p => p.IsReadOnly);
                if (hasReadonlyProperties)
                {
                    classStructureInfo.DomainType = DomainType.VALUE_OBJECT;
                }
                else
                {
                    classStructureInfo.DomainType = DomainType.ENTITY;
                }
                domainModelList.AddDomainModel(classStructureInfo);
            }

        }
    }
}
