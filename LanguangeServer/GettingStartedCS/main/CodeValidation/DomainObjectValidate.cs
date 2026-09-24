using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    using GettingStartedCS.main.ClassStructureInfo;
    public abstract class DomainObjectValidate
    {
        public abstract void Validate(ClassStructureInfo classInfo, ViolationList vlist);
    }
}
