using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        /// <summary>
        /// Prints every class in the collection with its stereotype(s),
        /// followed by each of its methods and their stereotype(s).
        /// </summary>
        public static void PrintStereotypes(IEnumerable<ClassStructureInfo> classes)
        {
            foreach (ClassStructureInfo cls in classes)
                PrintStereotypes(cls);
        }

        /// <summary>
        /// Prints one class with its stereotype(s), followed by its methods.
        /// </summary>
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
    }
}
