using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.IdentifyDomainModel
{
    using GettingStartedCS.main.ClassStructureInfo;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class DomainModelIdentifier
    {
        private DomainType _domainModelType;
        public DomainModelIdentifier() 
        {
            this._domainModelType = new DomainType();
        }

        public void IdentifyDomainModels(
            ClassDeclarationSyntax classInfo,
            ClassStructureInfo classStructureInfo,
            DomainModelList domainModelList)
        {
            var ctors = classInfo.Members.OfType<ConstructorDeclarationSyntax>();

            if (ctors.Any())
            {
                foreach (var property in classStructureInfo.Properties)
                {
                    string name = property.PropertyName;

                    if (name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        classStructureInfo.DomainType = _domainModelType.GetDomainType("ENTITY");
                        domainModelList.AddDomainModel(classStructureInfo);
                        return;
                    }
                }
            }

        }
    }
}
