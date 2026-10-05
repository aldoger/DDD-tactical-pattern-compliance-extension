using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class ValueObjectValidator : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.ValueObject valueObject = (StructureInfo.ValueObject)domainModel;
            bool c3 = ValidateConstraintC3(valueObject);
            bool c4 = ValidateConstraintC4(valueObject);
            if (c3)
            {
                vlist.AddViolation(
                    new Violation(
                        valueObject.Name,
                        Constraint.CONSTRAINT_C3,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C3)
                    )
                );
            }
            if (c4)
            {
                vlist.AddViolation(new 
                    Violation(
                        valueObject.Name,
                        Constraint.CONSTRAINT_C4, 
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C4)    
                    )
                );
            }
        }

        public bool ValidateConstraintC3(StructureInfo.ValueObject valueObject)
        {
            return valueObject.HasIdProperty;
        }

        public bool ValidateConstraintC4(StructureInfo.ValueObject valueObject)
        {
            bool isImmutable = valueObject.Properties.All(p => p.IsReadOnly);
            return isImmutable;
        }
    }
}
