using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.Util
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

        public static string GetClassDomainModelInfo()
        {
            return "";
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
    }
}
