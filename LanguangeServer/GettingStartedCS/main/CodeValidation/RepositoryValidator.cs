using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;
using System;
using System.Linq;
using System.Xml.Linq;

namespace GettingStartedCS.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public RepositoryValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            Repository rp = (Repository)domainModel;
            bool c12 = ValidateConstraintC12(rp);
            bool c13 = ValidateConstraintC13(rp);
            bool c14 = ValidateConstraintC14(rp, domainList);

            if (!c12)
            {
                vlist.AddViolation(
                    new Violation(
                        rp.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C12,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C12)
                    )
                );
            }

            if(!c13)
            {
                vlist.AddViolation(
                    new Violation(
                        rp.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C13,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C13)
                    )
                );
            }

            if (!c14)
            {
                vlist.AddViolation(
                    new Violation(
                        rp.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C14,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C14)
                    )
                );
            }
        }

        private bool ValidateConstraintC12(Repository rp)
        {
            // TODO: check every methods parameter to know parameter type is 
            return true;
        }

        private bool ValidateConstraintC13(Repository rp)
        {
            if (rp.ClassInfo == null) return false;
            var properties = parser.GetProperties(rp.ClassInfo.ClassDeclaration);
            if (properties.Count() > 0) return false;
            return true;
        }

        private bool ValidateConstraintC14(Repository rp, DomainModelList domainList)
        {
            if (rp.ClassInfo == null) return false;
            var domainModels = domainList.GetDomainModel(rp.ClassInfo.ClassName);
            if (domainModels == null || domainModels.Count() > 1) return false;
            return true;
        }
    }
}
