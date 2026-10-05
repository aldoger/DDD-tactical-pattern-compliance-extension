using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.Util
{
    using GettingStartedCS.main.StructureInfo;
    public static class Util
    {
        public static bool HasIdProperty(ClassStructureInfo classInfo)
        {
            return classInfo.Properties.Any(p => HasIdProperty(p));
        }

        public static bool HasIdProperty(PropertyStructureInfo property)
        {
            return HasIdProperty(property.PropertyName);
        }

        public static bool HasIdProperty(string name)
        {
            return name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsClassNameContaineFactory(ClassStructureInfo classInfo)
        {
            return classInfo.ClassName.Contains("Factory", StringComparison.OrdinalIgnoreCase);
        }
    }
}
