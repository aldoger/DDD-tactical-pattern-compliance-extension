using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.ClassStructureInfo
{
    public class PropertyStructureInfo
    {
        public string PropertyName;
        public bool IsReadOnly;
        public PropertyStructureInfo(
            string propertyName, 
            bool isReadOnly
        )
        {
            this.PropertyName = propertyName;
            this.IsReadOnly = isReadOnly;
        }
    }

    public class ParameterStructureInfo
    {
        public string ParameterName;
        public ParameterStructureInfo(string parameterName)
        {
            this.ParameterName = parameterName;
        }
    }

    public class MethodStructureInfo
    {
        public MethodStructureInfo(string methodName)
        {
            this.MethodName = methodName;
        }
        public string MethodName;
    }

    public class ClassStructureInfo
    {
        public string ClassName;
        public DomainType DomainType;
        public List<PropertyStructureInfo> Properties;
        public List<MethodStructureInfo> Methods;


        public void AddProperty(PropertyStructureInfo property)
        {
            Properties.Add(property);
        }

        public void AddMethod(MethodStructureInfo method)
        {
            Methods.Add(method);
        }

        public ClassStructureInfo(string className)
        {
            this.ClassName = className;
            Properties = new List<PropertyStructureInfo>();
            Methods = new List<MethodStructureInfo>();
        }
    }
}
