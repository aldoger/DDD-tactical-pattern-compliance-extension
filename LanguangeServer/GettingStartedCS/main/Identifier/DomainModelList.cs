using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.main.Identifier
{
    using GettingStartedCS.main.StructureInfo;
    using System;

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
