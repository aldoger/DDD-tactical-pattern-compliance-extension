using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.ClassStructureInfo
{
    public abstract class DomainModelStruct { }
    public class Entity : DomainModelStruct
    {
        public string ClassName { get; set; } = string.Empty;
        public List<PropertyStructureInfo> Properties { get; } = new List<PropertyStructureInfo>();
        public Entity() { }
    }
    public class ValueObject : DomainModelStruct
    {

    }
    public class DomainService : DomainModelStruct
    {

    }
    public class DomainEvent : DomainModelStruct
    {

    }
    public class Factory : DomainModelStruct
    {

    }
    public class Repository : DomainModelStruct
    {

    }
    public class Aggregate : DomainModelStruct
    {

    }
}
