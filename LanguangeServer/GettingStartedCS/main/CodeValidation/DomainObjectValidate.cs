using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public abstract class DomainObjectValidate
    {
        public abstract void Validate(StructureInfo.DomainModelStruct classInfo, ViolationList vlist, DomainModelList domainList);
    }
}
