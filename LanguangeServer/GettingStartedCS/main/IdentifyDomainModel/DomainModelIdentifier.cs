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
            var ctors = classInfo.Members.OfType<ConstructorDeclarationSyntax>();

            if (ctors.Any())
            {
                classStructureInfo.DomainType = DomainType.ENTITY;
                domainModelList.AddDomainModel(classStructureInfo);
            }

        }
    }
}
