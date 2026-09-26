using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.ClassStructureInfo
{
    public class ConstructorStructureInfo
    {
        public List<ParameterStructureInfo> Parameters;
        public bool IsPublic;
    }
    public class PropertyStructureInfo
    {
        public string PropertyName;
        public string PropertyType;
        public bool IsReadOnly;
        public PropertyStructureInfo(string propertyName, string propertyType, bool readOnly)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            IsReadOnly = readOnly;
        }
    }

    public class ParameterStructureInfo
    {
        public string ParameterName;
        public string ParameterType;   
        public ParameterStructureInfo(string parameterName, string parameterType)
        {
            ParameterName = parameterName;
            ParameterType = parameterType;
        }
    }

    public class MethodStructureInfo
    {
        public string MethodName;
        public string ReturnType;
        public List<ParameterStructureInfo> Parameters;
        public MethodStructureInfo(string methodName, string returnType)
        {
            MethodName = methodName;
            ReturnType = returnType;
            Parameters = new List<ParameterStructureInfo>();
        }
    }

    public class ClassStructureInfo
    {
        public string ClassName;
        public string BaseClassName;              
        public DomainType DomainType;
        public bool HasIdProperty;
        public List<PropertyStructureInfo> Properties;
        public List<MethodStructureInfo> Methods;
        public List<ConstructorStructureInfo> Constructors;
        public bool OverridesEquals;               
        public bool OverridesGetHashCode;

        public void AddProperty(PropertyStructureInfo property)
        {
            Properties.Add(property);
        }

        public void AddMethod(MethodStructureInfo method)
        {
            Methods.Add(method);
        }

        public void SetBaseClassName(string baseClassName)
        {
            BaseClassName = baseClassName;
        }
        public void SetHasIdProperty(bool isId)
        {
            HasIdProperty = isId;
        }

        public ClassStructureInfo(string className)
        {
            ClassName = className;
            Properties = new List<PropertyStructureInfo>();
            Methods = new List<MethodStructureInfo>();
        }
    }
}
