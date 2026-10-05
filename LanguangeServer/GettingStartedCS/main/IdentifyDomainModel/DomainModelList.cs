using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.main.IdentifyDomainModel
{
    using GettingStartedCS.main.StructureInfo;
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
        public DomainModelStruct? GetDomainModel(string name)
        {
            var domainModel = DomainModels.FirstOrDefault(dm => dm.Name == name);
            return domainModel;
        }
    }
}
