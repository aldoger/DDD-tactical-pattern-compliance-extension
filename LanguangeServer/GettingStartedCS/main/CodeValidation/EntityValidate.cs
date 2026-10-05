using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Linq;


namespace GettingStartedCS.main.CodeValidation
{
    public class EntityValidate : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.Entity entity = (StructureInfo.Entity)domainModel;
            bool c1 = ValidateConstraintC1(entity);
            bool c2 = ValidateConstraintC2(entity);

            if (!c1)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.Name,
                        Constraint.CONSTRAINT_C1,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C1)
                    )
                );
            }

            if (c2)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.Name,
                        Constraint.CONSTRAINT_C2,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C2)
                    )
                );
            }
        }

        public bool ValidateConstraintC1(StructureInfo.Entity entity)
        {
            return entity.HasIdProperty;
        }

        public bool ValidateConstraintC2(StructureInfo.Entity entity)
        {
            var idProperty = entity.Properties.FirstOrDefault(p =>
                p.PropertyName.Contains("Id", StringComparison.OrdinalIgnoreCase));

            if (idProperty == null) return false;

            return !idProperty.IsReadOnly;
        }
    }
}
