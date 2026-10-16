using System;
using System.Linq;
using GettingStartedCS.main.StructureInfo;
using System.Collections.Generic;

namespace GettingStartedCS.main.Identifier
{
    public static class DomainModelIdentifier
    {
        public static void IdentifyDomainModels(
            ClassStructureInfo classStructureInfo,
            DomainModelList domainModelList
        )
        {
            var cls = ClassMemberContext.From(classStructureInfo.ClassDeclaration);
            foreach (var m in classStructureInfo.Methods) m.Analyze(cls);
            new ClassStereotypeIdentifier().IdentifyStereotype(classStructureInfo);

            if(IsFactory(classStructureInfo.Stereotype))
            {
                Factory newFactory = new Factory(classStructureInfo.ClassName);
                newFactory.SetClassInfo(classStructureInfo);
                domainModelList.AddDomainModel(newFactory);
                return;
            }

            if (IsRepository(classStructureInfo.Stereotype))
            {
                Repository newRepository = new Repository(classStructureInfo.ClassName, classStructureInfo);
                domainModelList.AddDomainModel(newRepository);
                return;
            }

            if (IsDomainService(classStructureInfo.Stereotype))
            {
                DomainService newDomainService = new DomainService(classStructureInfo.ClassName, classStructureInfo);
                domainModelList.AddDomainModel(newDomainService);
                return;
            }

            if (IsEntity(classStructureInfo.Stereotype))
            {
                Entity newEntity = new Entity(classStructureInfo.ClassName, classStructureInfo);
                domainModelList.AddDomainModel(newEntity);
                return;
            }

            if (IsValueObject(classStructureInfo.Stereotype))
            {
                ValueObject newValueObject = new ValueObject(classStructureInfo.ClassName, classStructureInfo);
                domainModelList.AddDomainModel(newValueObject);
            }
        }

        private static bool IsFactory(List<ClassStereotype> ls)
        {
            if (ls.Contains(ClassStereotype.Factory) && ls.Count() == 1) return true;
            return false;
        }

        private static bool IsEntity(List<ClassStereotype> ls)
        {
            var isEntity = false;
            if (ls.Contains(ClassStereotype.Entity)) isEntity = true;
            return isEntity;
        }

        private static bool IsValueObject(List<ClassStereotype> ls)
        {
            var isValueObject = false;
            if (ls.Contains(ClassStereotype.DataClass)) isValueObject = true;
            if (ls.Contains(ClassStereotype.DataProvider)) isValueObject = true;
            return isValueObject;
        }

        private static bool IsDomainService(List<ClassStereotype> ls)
        {
            var isDomainService = false;
            if (ls.Contains(ClassStereotype.Boundary)) isDomainService = true;
            if (ls.Contains(ClassStereotype.Commander)) isDomainService = true;
            if (ls.Contains(ClassStereotype.PureController)) isDomainService = true;
            if (ls.Contains(ClassStereotype.Controller)) isDomainService = true;
            return isDomainService;
        }

        private static bool IsRepository(List<ClassStereotype> ls)
        {
            if (ls.Contains(ClassStereotype.Boundary) && !ls.Contains(ClassStereotype.Entity)) return true;
            return false;
        }
    }
}
