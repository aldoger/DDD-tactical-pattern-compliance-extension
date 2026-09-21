using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
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
    }
}
