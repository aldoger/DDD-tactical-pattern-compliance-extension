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
        public override bool Validate(ClassStructureInfo entity)
        {
            return ValidateEntity(entity);
        }

        public bool ValidateEntity(ClassStructureInfo entity)
        {
            bool hasIdProperty = entity.Properties.Any(p =>
                string.Equals(p.PropertyName, "Id", StringComparison.OrdinalIgnoreCase));
            return hasIdProperty;
        }
    }
}
