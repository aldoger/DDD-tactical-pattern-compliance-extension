using System;
using System.Linq;


namespace GettingStartedCS.main.CodeValidation
{
    using GettingStartedCS.main.ClassStructureInfo;

    public class EntityValidate : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo entity, ViolationList vlist)
        {
            bool c1 = ValidateEntityC1(entity);
            bool c2 = ValidateEntityC2(entity);

            if (c1)
            {
                vlist.AddViolation(
                    new Violation(
                        Constraint.CONSTRAINT_C1,
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C1)
                    )
                );
            }

            if (c2)
            {
                vlist.AddViolation(
                    new Violation(
                        Constraint.CONSTRAINT_C2,
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C2)
                    )
                );
            }
        }

        public bool ValidateEntityC1(ClassStructureInfo entity)
        {
            bool hasIdProperty = entity.Properties.Any(p =>
                string.Equals(
                    p.PropertyName,
                    "Id",
                    StringComparison.OrdinalIgnoreCase
                ));

            return !hasIdProperty;
        }

        public bool ValidateEntityC2(ClassStructureInfo entity)
        {
            bool hasIdProperty = entity.Properties.Any(p =>
                string.Equals(
                    p.PropertyName,
                    "Id",
                    StringComparison.OrdinalIgnoreCase
                ));

            return !hasIdProperty;
        }
    }
}
