using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainServiceValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            bool c10 = ValidateConstraintC10(classInfo);
            bool c11 = ValidateConstraintC11(classInfo);
            if(!c10)
            {
                vlist.AddViolation(new Violation(
                    classInfo.ClassName,
                    Constraint.CONSTRAINT_C10,
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C10)
                ));
            }
            if(!c11)
            {
                vlist.AddViolation(new Violation(
                    classInfo.ClassName,
                    Constraint.CONSTRAINT_C11,
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C11)
                ));
            }
        }
        public bool ValidateConstraintC10(ClassStructureInfo.ClassStructureInfo service)
        {
            return service.Properties.Count == 0;
        }
        public bool ValidateConstraintC11(ClassStructureInfo.ClassStructureInfo service)
        {
            // Still confuse
            return true;
        }
    }
}
