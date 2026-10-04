using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            bool c12 = ValidateConstrainC12(classInfo);
            bool c13 = ValidateConstraintC13(classInfo);
            bool c14 = ValidateConstraintC14(classInfo);
            if (!c12)
            {
                vlist.AddViolation(new Violation(
                    classInfo.ClassName, 
                    Constraint.CONSTRAINT_12, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_12))
                );
            }
            if (!c13)
            {
                vlist.AddViolation(new Violation(
                    classInfo.ClassName, 
                    Constraint.CONSTRAINT_13, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_13))
                );
            }
            if (!c14)
            {
                vlist.AddViolation(new Violation(
                    classInfo.ClassName, 
                    Constraint.CONSTRAINT_14, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_14))
                );
            }
        }
        public bool ValidateConstrainC12(ClassStructureInfo.ClassStructureInfo repository)
        {
            // TODO: Access method parameter and get the parameter type
            return true;   
        }
        public bool ValidateConstraintC13(ClassStructureInfo.ClassStructureInfo repository)
        {
            return repository.Properties.Count == 0;
        }
        public bool ValidateConstraintC14(ClassStructureInfo.ClassStructureInfo repository)
        {
            // Still confused C14
            return true;
        }
    }
}
