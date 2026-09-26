using System;
using System.Linq;


namespace GettingStartedCS.main.CodeValidation
{
    using GettingStartedCS.main.ClassStructureInfo;

    public class EntityValidate : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo entity, ViolationList vlist)
        {
            bool c1 = ValidateConstraintC1(entity);
            bool c2 = ValidateConstraintC2(entity);

            if (c1)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.ClassName,
                        Constraint.CONSTRAINT_C1,
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C1)
                    )
                );
            }

            if (c2)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.ClassName,
                        Constraint.CONSTRAINT_C2,
                        ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C2)
                    )
                );
            }
        }

        public bool ValidateConstraintC1(ClassStructureInfo entity)
        {
            bool hasIdProperty = entity.Properties.Any(p =>
                p.PropertyName.Contains("Id", StringComparison.OrdinalIgnoreCase)
            );

            return !hasIdProperty;
        }

        public bool ValidateConstraintC2(ClassStructureInfo entity)
        {
            var idProperty = entity.Properties.FirstOrDefault(p =>
                p.PropertyName.Contains("Id", StringComparison.OrdinalIgnoreCase));

            if (idProperty == null) return false;

            return !idProperty.IsReadOnly;
        }
    }
}
