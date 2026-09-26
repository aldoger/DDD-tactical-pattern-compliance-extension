using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class ValueObjectValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo valueObject, ViolationList vlist)
        {
            bool c3 = ValidateConstraintC3(valueObject);
            bool c4 = ValidateConstraintC4(valueObject);
            if (c3)
            {
                vlist.AddViolation(
                    new Violation(
                        valueObject.ClassName,
                        Constraint.CONSTRAINT_C3,
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C3)
                    )
                );
            }
            if (c4)
            {
                vlist.AddViolation(new 
                    Violation(
                        valueObject.ClassName,
                        Constraint.CONSTRAINT_C4, 
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C4)    
                    )
                );
            }
        }

        public bool ValidateConstraintC3(ClassStructureInfo.ClassStructureInfo valueObject)
        {
            bool hasIdProperty = valueObject.Properties.Any(p =>
               string.Equals(
                   p.PropertyName,
                   "Id",
                   StringComparison.OrdinalIgnoreCase
               ));

            return hasIdProperty;
        }

        public bool ValidateConstraintC4(ClassStructureInfo.ClassStructureInfo valueObject)
        {
            bool isImmutable = valueObject.Properties.All(p => p.IsReadOnly);
            return isImmutable;
        }
    }
}
