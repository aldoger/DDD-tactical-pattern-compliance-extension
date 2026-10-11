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
        public ClassStructureInfo? ClassInfo;
        public Entity(string name, ClassStructureInfo classInfo)
            : base(name, "Entity")
        {
            ClassInfo = classInfo;
        }
    }
    public class ValueObject : DomainModelStruct
    {
        public ClassStructureInfo? ClassInfo;
        public ValueObject(string name, ClassStructureInfo classInfo)
            : base(name, "Value Object")
        {
            ClassInfo = classInfo;
        }
    }
    public class DomainService : DomainModelStruct
    {
        public ClassStructureInfo? ClassInfo;
        public DomainService(string name, ClassStructureInfo? classInfo)
            : base(name, "Domain Service")
        {
            ClassInfo = classInfo;
        }
    }
    public class DomainEvent : DomainModelStruct
    {
        public ClassStructureInfo? ClassInfo;
        public DomainEvent(string name, ClassStructureInfo? classInfo)
            : base(name, "Domain Event")
        {
            ClassInfo = classInfo;
        }
    }
    public class Factory : DomainModelStruct
    {
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
        public Factory(string name)
            : base(name, "Factory")
        {

        }
    }
    public class Repository : DomainModelStruct
    {
        public ClassStructureInfo? ClassInfo;
        public Repository(string name, ClassStructureInfo classInfo)
            : base(name, "Repository")
        {
            ClassInfo = classInfo;
        }
    }
    public class Aggregate : DomainModelStruct
    {
        public ClassStructureInfo? ClassInfo;
        public string AggregateType;
        public void SetAggregateType(string aggregateType)
        {
            AggregateType = aggregateType;
        }
        public Aggregate(string name, ClassStructureInfo classInfo)
            : base(name, "Aggregate")
        {
            ClassInfo = classInfo;
        }
    }
}
