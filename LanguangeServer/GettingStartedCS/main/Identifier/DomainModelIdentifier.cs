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

        public static bool IsEntity(List<ClassStereotype> ls)
        {
            var isEntity = false;
            if (ls.Contains(ClassStereotype.Entity)) isEntity = true;
            if (ls.Contains(ClassStereotype.Commander)) isEntity = true;
            return isEntity;
        }

        public static bool IsValueObject(List<ClassStereotype> ls)
        {
            var isValueObject = false;
            if (ls.Contains(ClassStereotype.DataClass)) isValueObject = true;
            if (ls.Contains(ClassStereotype.DataProvider)) isValueObject = true;
            return isValueObject;
        }
    }
}
