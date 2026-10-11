using GettingStartedCS.main.Identifier;
using System;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;

namespace GettingStartedCS.main.CodeValidation
{
    public class AggregateValidator : DomainObjectValidate
    {
        public AggregateValidator(Parser par) : base(par)
        { }

        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            throw new NotImplementedException();
        }
    }
}
