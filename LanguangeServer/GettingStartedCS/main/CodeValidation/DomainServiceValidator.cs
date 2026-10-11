using GettingStartedCS.main.Identifier;
using System;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainServiceValidator : DomainObjectValidate
    {
        public DomainServiceValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            throw new NotImplementedException();
        }
    }
}
