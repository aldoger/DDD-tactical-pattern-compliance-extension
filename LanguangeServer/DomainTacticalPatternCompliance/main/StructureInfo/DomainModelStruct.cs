using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTacticalPatternCompliance.main.StructureInfo
{
    public enum AggregateType
    {
        AGGREGATE_PART,
        AGGREGATE_ROOT
    }

    public static class AggregateTypeExtensions
    {
        public static string ToText(this AggregateType t) => t == AggregateType.AGGREGATE_ROOT ? "Root" : "Part";
    }
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
        public AggregateType AggregateType;
       
        public string? RootName;
        public void SetAggregateType(AggregateType aggregateType)
        {
            AggregateType = aggregateType;
        }
        public void SetRootName(string rootName)
        {
            RootName = rootName;
        }
        public Aggregate(string name, ClassStructureInfo classInfo)
            : base(name, "Aggregate")
        {
            ClassInfo = classInfo;
        }
    }

    public class DomainModelList
    {
        public DomainModelList() { }
        public List<DomainModelStruct> DomainModels { get; } = new List<DomainModelStruct>();
        public void AddDomainModel(DomainModelStruct domainModel)
        {
            DomainModels.Add(domainModel);
        }
        public bool ConsistsOfDomainModels()
        {
            return DomainModels.Count > 0;
        }
        public bool ContainsDomainModel(string name)
        {
            return DomainModels.Any(dm => dm.Name == name);
        }
        public List<DomainModelStruct>? GetDomainModel(string name)
        {
            var domainModel = DomainModels.Where(dm => dm.Name == name).ToList();
            return domainModel;
        }
        public DomainModelStruct? GetDomainModelFirst(string name)
        {
            var domainModel = DomainModels.FirstOrDefault(dm => dm.Name == name);
            return domainModel;
        }

        public List<DomainModelStruct> GetDomainModelByType(DomainModelStruct dms)
        {
            return dms switch
            {
                Entity => Filter<Entity>(),
                ValueObject => Filter<ValueObject>(),
                DomainService => Filter<DomainService>(),
                DomainEvent => Filter<DomainEvent>(),
                Factory => Filter<Factory>(),
                Repository => Filter<Repository>(),
                Aggregate => Filter<Aggregate>(),
                null => throw new ArgumentNullException(nameof(dms)),
                _ => throw new NotSupportedException(
                                     $"Tipe {dms.GetType().Name} belum ditangani")
            };
        }

        private List<DomainModelStruct> Filter<T>() where T : DomainModelStruct
             => DomainModels.OfType<T>().Cast<DomainModelStruct>().ToList();
    }
}
