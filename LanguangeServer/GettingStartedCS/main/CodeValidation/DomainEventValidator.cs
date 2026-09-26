using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainEventValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            throw new NotImplementedException();
        }

        public bool ValidateConstraintC5(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            // C5. A domain event has and only has one identity.
            throw new NotImplementedException();
        }
    }
}
