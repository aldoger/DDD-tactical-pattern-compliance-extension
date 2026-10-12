using GettingStartedCS.main.Identifier;
using System;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;
using System.Linq;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainServiceValidator : DomainObjectValidate
    {
        public DomainServiceValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            DomainService ds = (DomainService)domainModel;
            bool c10 = ValidateConstraintC10(ds);
            bool c11 = ValidateConstraintC11(ds, domainList);

            if(!c10)
            {
                vlist.AddViolation(
                    new Violation(
                        ds.Name,
                        Constraint.CONSTRAINT_C10,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C10)
                    )
                );
            }

            if (!c11)
            {
                vlist.AddViolation(
                    new Violation(
                        ds.Name,
                        Constraint.CONSTRAINT_C11,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C11)
                    )
                );
            }
        }

        private bool ValidateConstraintC10(DomainService ds)
        {
            if (ds.ClassInfo == null) return false;
            var properties = parser.GetProperties(ds.ClassInfo.ClassDeclaration);
            if (properties.Count() > 0) return false;
            return true;
        }

        private bool ValidateConstraintC11(DomainService ds, DomainModelList domainModelList)
        {
            var domainModels = domainModelList.GetDomainModel(ds.Name);
            if (domainModels.Count() > 0) return false;
            return true;
        }
    }
}
