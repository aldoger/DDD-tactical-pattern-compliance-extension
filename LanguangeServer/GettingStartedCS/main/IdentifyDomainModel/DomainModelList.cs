using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.main.IdentifyDomainModel
{
    using GettingStartedCS.main.ClassStructureInfo;
    public class DomainModelList
    {
        public DomainModelList() { }
        public List<ClassStructureInfo> DomainModels { get; } = new List<ClassStructureInfo>();

        public void AddDomainModel(ClassStructureInfo domainModel)
        {
            DomainModels.Add(domainModel);
        }
        public bool ConsistsOfDomainModels()
        {
            return DomainModels.Count > 0;
        }
        public bool ContainsDomainModel(string className)
        {
            return DomainModels.Any(dm => dm.ClassName == className);
        }
    }
}
