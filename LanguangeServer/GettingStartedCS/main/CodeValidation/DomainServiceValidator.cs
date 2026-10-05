using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainServiceValidator : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.DomainService service = (StructureInfo.DomainService)domainModel;
            bool c10 = ValidateConstraintC10(service);
            bool c11 = ValidateConstraintC11(service);
            if(!c10)
            {
                vlist.AddViolation(new Violation(
                    service.Name,
                    Constraint.CONSTRAINT_C10,
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C10)
                ));
            }
            if(!c11)
            {
                vlist.AddViolation(new Violation(
                    service.Name,
                    Constraint.CONSTRAINT_C11,
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C11)
                ));
            }
        }
        public bool ValidateConstraintC10(StructureInfo.DomainService service)
        {
            return service.HasProperties;
        }
        public bool ValidateConstraintC11(StructureInfo.DomainService service)
        {
            // Still confuse
            return true;
        }
    }
}
