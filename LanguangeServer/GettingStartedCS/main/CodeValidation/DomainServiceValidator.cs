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
            bool c11 = ValidateConstraintC11(service, domainList);
            if(c10)
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
            if(service.ClassInfo == null)
            {
                return false;
            }
            return service.ClassInfo.Properties.Count == 0;
        }
        public bool ValidateConstraintC11(StructureInfo.DomainService service, DomainModelList domainModelList)
        {
            var domainModels = domainModelList.GetDomainModel(service.Name);
            if(domainModels == null || domainModels.Count == 0)
            {
                return false;
            }
            return true;
        }
    }
}
