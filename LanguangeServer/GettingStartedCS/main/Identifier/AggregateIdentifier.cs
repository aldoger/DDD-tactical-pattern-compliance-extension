using System.Collections.Generic;
using System.Security.Permissions;
using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GettingStartedCS.Main.Identifier
{
    public class AggregateIdentifier
    {
        public AggregateIdentifier() { }
        public void Identify(DomainModelList dms)
        {
            
        }
    }

    public class CallGraphBuilder
    {
        public List<ClassDeclarationSyntax> Classes;
        public List<Entity> Entities;
        public CallGraphBuilder(List<ClassDeclarationSyntax> classes, List<Entity> entities)
        {
            Classes = classes;
            Entities = entities;
        }
    }
}
