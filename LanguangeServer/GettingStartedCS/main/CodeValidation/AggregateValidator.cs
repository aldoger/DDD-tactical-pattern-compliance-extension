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

        private void ValidateConstraintC17(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
            
        }

        private void ValidateConstraintC18(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;

        }

        private void ValidateConstraintC19(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }

        private void ValidateConstraintC20(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }

        private void ValidateConstraintC21(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }

        private void ValidateConstraintC22(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }

        private void ValidateConstraintC23(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }

        private void ValidateConstraintC24(Aggregate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return;
        }
    }
}
