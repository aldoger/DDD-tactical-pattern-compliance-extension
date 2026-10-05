using System;
using System.Linq;

namespace GettingStartedCS.main.IdentifyDomainModel
{
    using GettingStartedCS.main.StructureInfo;
    using GettingStartedCS.Util;

    public static class DomainModelIdentifier
    {
        public static void IdentifyDomainModels(
            ClassStructureInfo classStructureInfo,
            ClassStructureInfoList classStructureInfoList,
            DomainModelList domainModelList
        )
        {
            // check for base class
            string baseClassName = classStructureInfo.BaseClassName;
            if (baseClassName != null && domainModelList.ContainsDomainModel(baseClassName))
            {
                // check if base class entity has Id property
                var baseClass = classStructureInfoList.GetClass(baseClassName);
                if (baseClass != null && Util.HasIdProperty(baseClass))
                {
                    var entity = new Entity(classStructureInfo.ClassName, classStructureInfo.Properties, true);
                    domainModelList.AddDomainModel(entity);
                }
            }

            // check if class has constructor and id property
            if (classStructureInfo.HasConstructor && Util.HasIdProperty(classStructureInfo))
            {
                var entity = new Entity(classStructureInfo.ClassName, classStructureInfo.Properties, true);
                domainModelList.AddDomainModel(entity);
            }
            if (classStructureInfo.HasConstructor && !Util.HasIdProperty(classStructureInfo))
            {
                if (classStructureInfo.IsImmutable)
                {
                    var vo = new ValueObject(classStructureInfo.ClassName, classStructureInfo.Properties, false);
                    domainModelList.AddDomainModel(vo);
                }
            }

            // check if class is a factory class
            if (Util.IsClassNameContaineFactory(classStructureInfo))
            {
                var countProperties = classStructureInfo.Properties.Count;
                var factory = new Factory(classStructureInfo.ClassName, countProperties == 0);
                factory.SetClassInfo(classStructureInfo);
                domainModelList.AddDomainModel(factory);
            }
        }
    }
}
