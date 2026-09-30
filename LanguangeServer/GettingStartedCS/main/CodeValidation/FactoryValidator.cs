using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class FactoryValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            bool c15 = ValidateConstraintC15(classInfo);
            bool c16 = ValidateConstraintC16(classInfo);
            if (c15)
            {

            }
        }
        public bool ValidateConstraintC15(ClassStructureInfo.ClassStructureInfo factory)
        {
            // TODO: Access the parameter or return type to see what object it create
            return true;
        }
        public bool ValidateConstraintC16(ClassStructureInfo.ClassStructureInfo factory)
        {
            // Still confused
            return true;
        }
    }
}
