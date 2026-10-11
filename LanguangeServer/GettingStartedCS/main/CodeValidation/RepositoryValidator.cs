using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using System;
using GettingStartedCS.parser;

namespace GettingStartedCS.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public RepositoryValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            throw new NotImplementedException();
        }
    }
}
