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
            throw new NotImplementedException();
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
