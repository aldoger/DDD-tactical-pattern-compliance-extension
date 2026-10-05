using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.StructureInfo
{
    public abstract class DomainModelStruct 
    {
        public string Name { get; }
        public string DomainType { get; }

        protected DomainModelStruct(string name, string domainType)
        {
            Name = name;
            DomainType = domainType;
        }
    }
    public class Entity : DomainModelStruct
    {
        public bool HasIdProperty;
        public List<PropertyStructureInfo> Properties { get; }
        public Entity(string name, List<PropertyStructureInfo> properties, bool hasIdProperty)
            : base(name, "Entity")
        {
            Properties = properties;
            HasIdProperty = hasIdProperty;
        }
    }
    public class ValueObject : DomainModelStruct
    {
        public bool HasIdProperty;
        public List<PropertyStructureInfo> Properties { get; }
        public ValueObject(string name, List<PropertyStructureInfo> properties, bool hasIdProperty)
            : base(name, "Value Object")
        {
            Properties = properties;
            HasIdProperty = hasIdProperty;
        }
    }
    public class DomainService : DomainModelStruct
    {
        public bool HasProperties;
        public DomainService(string name, bool hasProperties)
            : base(name, "Domain Service")
        {
            HasProperties = hasProperties;
        }
    }
    public class DomainEvent : DomainModelStruct
    {
        public bool HasIdProperty;
        public bool IsReadOnly;
        public List<PropertyStructureInfo> Properties { get; }
        public DomainEvent(string name, List<PropertyStructureInfo> properties, bool hasIdProperty, bool isReadOnly)
            : base(name, "Domain Event")
        {
            Properties = properties;
            HasIdProperty = hasIdProperty;
            IsReadOnly = isReadOnly;
        }
    }
    public class Factory : DomainModelStruct
    {
        public bool HasProperties;
        public bool IsClass;
        public bool IsMethod;
        public ClassStructureInfo? ClassInfo;
        public MethodStructureInfo? MethodInfo;
        public void SetClassInfo(ClassStructureInfo classInfo)
        {
            ClassInfo = classInfo;
            IsClass = true;
            IsMethod = false;
        }
        public void SetMethodInfo(MethodStructureInfo methodInfo)
        {
            MethodInfo = methodInfo;
            IsClass = false;
            IsMethod = true;
        }
        public Factory(string name, bool hasProperties)
            : base(name, "Factory")
        {
            HasProperties = hasProperties;
        }
    }
    public class Repository : DomainModelStruct
    {
        public bool HasProperties;
        public Repository(string name, bool hasProperties)
            : base(name, "Repository")
        {
            HasProperties = hasProperties;
        }
    }
    public class Aggregate : DomainModelStruct
    {
        public Aggregate(string name)
            : base(name, "Aggregate")
        {
        }
    }
}
