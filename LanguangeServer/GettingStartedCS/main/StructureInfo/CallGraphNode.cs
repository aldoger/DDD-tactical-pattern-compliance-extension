using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.Main.StructureInfo
{
    public class CallGraphNode
    {
        public string ClassName;
        public ClassDeclarationSyntax ClassDeclaration;
        public bool IsEntityMethod;

        public CallGraphNode(string className, ClassDeclarationSyntax classDeclaration, bool isEntityMethod)
        {
            ClassName = className;
            ClassDeclaration = classDeclaration;
            IsEntityMethod = isEntityMethod;
        }
    }
}
