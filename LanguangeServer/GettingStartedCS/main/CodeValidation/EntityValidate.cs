using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    using GettingStartedCS.main.ClassStructureInfo;

    public class EntityValidate : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo entity)
        {
            throw new NotImplementedException();
        }

        public void ValidateEntity(ClassStructureInfo entity)
        {
            var classProperties = entity.Properties;
        }
    }
}
