using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.StructureInfo
{
    public class PropertyStructureInfo
    {
        public string PropertyName;
        public string PropertyType;
        public bool IsReadOnly;
        public void SetIsReadOnly(bool readOnly)
        {
            IsReadOnly = readOnly;
        }
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
        public bool IsStatic;
        public string ReturnType;
        public void SetIsStatic(bool isStatic)
        {
            IsStatic = isStatic;
        }
        public MethodStructureInfo(string methodName, string returnType)
        {
            MethodName = methodName;
            ReturnType = returnType;
        }
    }

    public class ClassStructureInfo
    {
        public string ClassName;
        public string BaseClassName;
        public bool HasConstructor;
        public List<PropertyStructureInfo> Properties;
        public List<MethodStructureInfo> Methods;
        public bool IsStatic;
        public bool IsImmutable;
        public void AddProperty(PropertyStructureInfo property)
        {
            Properties.Add(property);
        }

        public void AddMethod(MethodStructureInfo method)
        {
            Methods.Add(method);
        }
        public void SetHasConstructor(bool hasConstructor)
        {
            HasConstructor = hasConstructor;
        }
        public void SetIsImmutable(bool isImmutable)
        {
            IsImmutable = isImmutable;
        }
        public void SetBaseClassName(string baseClassName)
        {
            BaseClassName = baseClassName;
        }
        public void SetIsStatic(bool isStatic)
        {
            IsStatic = isStatic;
        }

        public ClassStructureInfo(string className)
        {
            ClassName = className;
            BaseClassName = string.Empty;
            Properties = new List<PropertyStructureInfo>();
            Methods = new List<MethodStructureInfo>();
        }
    }

    public class ClassStructureInfoList
    {
        public List<ClassStructureInfo> Classes { get; } = new List<ClassStructureInfo>();
        public void AddClass(ClassStructureInfo classInfo)
        {
            Classes.Add(classInfo);
        }
        public bool ContainsClass(string className)
        {
            return Classes.Any(c => c.ClassName == className);
        }
        public ClassStructureInfo? GetClass(string className)
        {
            return Classes.FirstOrDefault(c => c.ClassName == className);
        }
    }
}
