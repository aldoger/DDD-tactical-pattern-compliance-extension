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

        private bool ValidateConstraintC17(Aggregate aggregate, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return false;
            var name = aggregate.ClassInfo.ClassName;
            var domainModel = domainList.GetDomainModelFirst(name);
            if(domainModel == null || domainModel is not Entity)
            {
                return false;
            }
            return true;
        }

        private bool ValidateConstraintC18(Aggregate aggregate, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return false;
            var name = aggregate.ClassInfo.ClassName;
            var domainModel = domainList.GetDomainModelFirst(name);
            if (domainModel == null || (domainModel is not Entity && domainModel is not ValueObject))
            {
                return false;
            }
            return true;
        }

        private bool ValidateConstraintC19(Aggregate aggregate, DomainModelList domainList)
        {
            /*
             The reference of an aggregate part cannot
            be held by the outside objects.     
             */
            return true;
        }

        private bool ValidateConstraintC20(Aggregate aggregate, DomainModelList domainList)
        {
            if (aggregate.ClassInfo == null) return false;

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
