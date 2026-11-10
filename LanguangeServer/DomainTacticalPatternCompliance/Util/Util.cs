using DomainTacticalPatternCompliance.main.Identifier;
using DomainTacticalPatternCompliance.main.StructureInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DomainTacticalPatternCompliance.Util
{
    public static class Util
    {
        public static bool ContainsId(string name)
        {
            return name.Contains("Id", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDomainModelEntityOrValueObjectOrAggregate(DomainModelStruct dm)
        {
            if ((dm is Entity) || (dm is ValueObject) || (dm is Aggregate)) return true;
            return false;
        }

        public static void PrintStereotypes(IEnumerable<ClassStructureInfo> classes)
        {
            foreach (ClassStructureInfo cls in classes)
                PrintStereotypes(cls);
        }

        public static void PrintStereotypes(ClassStructureInfo cls)
        {
            Console.WriteLine($"Class: {cls.ClassName}  [{FormatClassStereotypes(cls)}]");

            foreach (MethodStructureInfo method in cls.Methods)
                Console.WriteLine($"    Method: {method.MethodName,-30} [{FormatMethodStereotypes(method)}]");

            Console.WriteLine();
        }

        private static string FormatClassStereotypes(ClassStructureInfo cls)
        {
            return cls.Stereotype.Count == 0
                ? ClassStereotype.Unclassified.ToText()
                : string.Join(", ", cls.Stereotype.Select(s => s.ToText()));
        }

        private static string FormatMethodStereotypes(MethodStructureInfo method)
        {
            return method.Stereotype.Count == 0
                ? MethodStereotype.Unclassified.ToText()
                : string.Join(", ", method.Stereotype.Select(s => s.ToText()));
        }

        public static string PrintDomainModelList(DomainModelList domainModelList)
        {
            var sb = new StringBuilder();
            foreach (var dm in domainModelList.DomainModels)
            {
                if (dm is Aggregate aggregate)
                {
                    sb.AppendLine($"Domain Model: {dm.Name} [{dm.DomainType} {aggregate.AggregateType.ToText()}]");
                    sb.AppendLine($"  Root: {aggregate.RootName ?? "-"}");
                    continue;
                }
                sb.AppendLine($"Domain Model: {dm.Name} [{dm.DomainType}]");
                if (dm is Entity entity && entity.ClassInfo != null)
                {
                    sb.AppendLine($"  Class: {entity.ClassInfo.ClassName}");
                    foreach (var method in entity.ClassInfo.Methods)
                    {
                        sb.AppendLine($"    Method: {method.MethodName}");
                    }
                }
                else if (dm is ValueObject valueObject && valueObject.ClassInfo != null)
                {
                    sb.AppendLine($"  Class: {valueObject.ClassInfo.ClassName}");
                    foreach (var method in valueObject.ClassInfo.Methods)
                    {
                        sb.AppendLine($"    Method: {method.MethodName}");
                    }
                }
                else if (dm is DomainService domainService && domainService.ClassInfo != null)
                {
                    sb.AppendLine($"  Class: {domainService.ClassInfo.ClassName}");
                    foreach (var method in domainService.ClassInfo.Methods)
                    {
                        sb.AppendLine($"    Method: {method.MethodName}");
                    }
                }
                else if (dm is DomainEvent domainEvent && domainEvent.ClassInfo != null)
                {
                    sb.AppendLine($"  Class: {domainEvent.ClassInfo.ClassName}");
                    foreach (var method in domainEvent.ClassInfo.Methods)
                    {
                        sb.AppendLine($"    Method: {method.MethodName}");
                    }
                }
            }
            return sb.ToString();
        }


    }
}
