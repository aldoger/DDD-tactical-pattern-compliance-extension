#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GettingStartedCS.main.Identifier
{
    public enum NameKind { None, Service, Factory, Repository }

    public enum DomainModelKind { Factory, Repository, DomainService, Entity, ValueObject }

    /// <summary>
    /// Structural evidence about a class, extracted from syntax. Used by
    /// DomainModelIdentifier next to the class stereotypes.
    /// </summary>
    public sealed record ClassSignals(
        bool HasIdentity,                  // Id or <ClassName>Id member
        int StateMemberCount,              // instance fields / auto-properties / record parameters
        bool AllStateReadOnly,             // >=1 state member and all immutable (get-only/init/readonly, non-collection)
        bool HasStructuralEquality,        // record, struct, IEquatable<T>, or BOTH Equals and GetHashCode overridden
        bool IsStateless,                  // no instance state besides injected dependencies
        int DomainTypesInPublicParams,     // distinct domain types in public method parameters
        bool ImplementsRepositoryInterface,
        int CrudMethodCount,               // public Add/Get/Find/Update/Remove/Delete/List/Save... methods
        int QueryVerbMethodCount,          // public methods whose name starts with Get/Find/Update
        double QueryVerbRatio,             // QueryVerbMethodCount / public methods
        double FactoryMethodRatio,         // public methods returning a new instance of ANOTHER type / public methods
        int SelfReturningMethods,          // With*-style methods returning a new instance of the own type
        NameKind NameKind,
        DomainModelKind? ExplicitKind      // [ValueObject], [Entity], ... attribute, if any
    );

    public static class ClassSignalExtractor
    {
        private readonly record struct StateMember(string Name, string TypeName, bool Mutable, bool Injected);

        private static readonly HashSet<string> CrudVerbs = new(StringComparer.Ordinal)
        {
            "Add", "Get", "Find", "Update", "Remove", "Delete", "List", "Save", "Insert", "Query", "Exists"
        };

        private static readonly HashSet<string> MutableCollections = new(StringComparer.Ordinal)
        {
            "List", "HashSet", "Dictionary", "ConcurrentDictionary", "SortedSet", "SortedDictionary",
            "SortedList", "Queue", "Stack", "LinkedList", "ConcurrentBag", "ConcurrentQueue",
            "ConcurrentStack", "ISet", "IList", "IDictionary", "ICollection", "Collection",
            "ObservableCollection"
        };

        // Only used when no project type list is supplied.
        private static readonly HashSet<string> NonDomainTypes = new(StringComparer.Ordinal)
        {
            "Guid", "DateTime", "DateTimeOffset", "TimeSpan", "DateOnly", "TimeOnly",
            "CancellationToken", "Uri", "Type", "Object", "Action", "Func", "Exception",
            "Decimal", "String", "Int32", "Int64", "Boolean", "Double", "Single", "Byte", "Char"
        };

        public static ClassSignals Extract(TypeDeclarationSyntax type, ISet<string>? projectTypeNames = null)
        {
            string ownName = type.Identifier.Text;

            var state = GetStateMembers(type).ToList();
            var publicMethods = GetPublicMethods(type).ToList();

            bool allReadOnly = state.Count > 0 && state.All(s => !s.Mutable);
            bool stateless = state.All(s => s.Injected);

            int domainTypes = publicMethods
                .SelectMany(m => m.ParameterList.Parameters)
                .Where(p => p.Type != null)
                .SelectMany(p => TypeNamesIn(p.Type!))
                .Where(n => n != ownName)
                .Where(n => projectTypeNames != null ? projectTypeNames.Contains(n) : LooksLikeDomainType(n))
                .Distinct()
                .Count();

            int crud = publicMethods.Count(m => IsCrudName(m.Identifier.Text));
            int queryCount = publicMethods.Count(m => IsQueryVerbName(m.Identifier.Text));
            double queryRatio = publicMethods.Count == 0 ? 0 : queryCount / (double)publicMethods.Count;

            double factoryRatio = publicMethods.Count == 0
                ? 0
                : publicMethods.Count(m => CreatesOtherType(m, ownName)) / (double)publicMethods.Count;

            int selfReturning = publicMethods.Count(m =>
                !m.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                UnwrappedReturnName(m.ReturnType) == ownName &&
                (ReturnsNewInstanceOf(m, ownName) ||
                 ReturnedExpressions(m).Any(e => Unparen(e) is WithExpressionSyntax)));

            bool repoInterface = type.BaseList?.Types.Any(t =>
            {
                string n = TypeName(t.Type);
                return n.EndsWith("Repository") || n.EndsWith("RepositoryBase");
            }) ?? false;

            return new ClassSignals(
                HasIdentity: HasIdentity(type, ownName),
                StateMemberCount: state.Count,
                AllStateReadOnly: allReadOnly,
                HasStructuralEquality: HasStructuralEquality(type),
                IsStateless: stateless,
                DomainTypesInPublicParams: domainTypes,
                ImplementsRepositoryInterface: repoInterface,
                CrudMethodCount: crud,
                QueryVerbMethodCount: queryCount,
                QueryVerbRatio: queryRatio,
                FactoryMethodRatio: factoryRatio,
                SelfReturningMethods: selfReturning,
                NameKind: GetNameKind(ownName),
                ExplicitKind: GetExplicitKind(type));
        }

        // ------------------------------------------------------------ identity / equality

        private static bool HasIdentity(TypeDeclarationSyntax type, string ownName)
        {
            bool Match(string n) => n == "Id" || n == ownName + "Id";

            bool props = type.Members.OfType<PropertyDeclarationSyntax>()
                .Any(p => !p.Modifiers.Any(SyntaxKind.StaticKeyword) && Match(p.Identifier.Text));

            bool fields = type.Members.OfType<FieldDeclarationSyntax>()
                .Any(f => !f.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                          f.Declaration.Variables.Any(v => Match(v.Identifier.Text)));

            bool recordParam = type is RecordDeclarationSyntax { ParameterList: { } pl } &&
                               pl.Parameters.Any(p => Match(p.Identifier.Text));

            return props || fields || recordParam;
        }

        private static bool HasStructuralEquality(TypeDeclarationSyntax type)
        {
            if (type is RecordDeclarationSyntax || type is StructDeclarationSyntax) return true;

            var overridden = type.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword))
                .Select(m => m.Identifier.Text)
                .ToHashSet();
            bool overrides = overridden.Contains("Equals") && overridden.Contains("GetHashCode");

            bool equatable = type.BaseList?.Types.Any(t => TypeName(t.Type) == "IEquatable") ?? false;

            return overrides || equatable;
        }

        // ------------------------------------------------------------ state

        private static IEnumerable<StateMember> GetStateMembers(TypeDeclarationSyntax type)
        {
            // record primary-constructor parameters are init-only properties
            if (type is RecordDeclarationSyntax { ParameterList: { } rpl })
            {
                foreach (var p in rpl.Parameters)
                {
                    if (p.Type is null) continue;
                    yield return new StateMember(p.Identifier.Text, TypeName(p.Type), IsMutableCollection(p.Type), false);
                }
            }

            foreach (var p in type.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (p.Modifiers.Any(SyntaxKind.StaticKeyword) || p.ExplicitInterfaceSpecifier != null) continue;
                if (p.ExpressionBody != null || p.AccessorList is null) continue;   // computed property

                var accessors = p.AccessorList.Accessors;
                bool isAuto = accessors.Any(a => a.Body == null && a.ExpressionBody == null);
                if (!isAuto) continue;                                               // explicit bodies => computed

                bool hasSet = accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                bool collection = IsMutableCollection(p.Type);
                string tn = TypeName(p.Type);
                bool injected = !hasSet && !collection && LooksLikeDependency(tn);

                yield return new StateMember(p.Identifier.Text, tn, hasSet || collection, injected);
            }

            foreach (var f in type.Members.OfType<FieldDeclarationSyntax>())
            {
                if (f.Modifiers.Any(SyntaxKind.StaticKeyword) || f.Modifiers.Any(SyntaxKind.ConstKeyword)) continue;

                bool readOnly = f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
                bool collection = IsMutableCollection(f.Declaration.Type);
                string tn = TypeName(f.Declaration.Type);
                bool injected = readOnly && !collection && LooksLikeDependency(tn);

                foreach (var v in f.Declaration.Variables)
                    yield return new StateMember(v.Identifier.Text, tn, !readOnly || collection, injected);
            }
        }

        private static bool IsMutableCollection(TypeSyntax t) =>
            t is ArrayTypeSyntax || MutableCollections.Contains(TypeName(t));

        private static bool LooksLikeDependency(string typeName) =>
            Regex.IsMatch(typeName, "^I[A-Z]") ||
            typeName.EndsWith("Service") || typeName.EndsWith("Repository") ||
            typeName.EndsWith("Factory") || typeName.EndsWith("Logger") ||
            typeName.EndsWith("Context") || typeName.EndsWith("Client") ||
            typeName.EndsWith("Provider") || typeName.EndsWith("Handler") ||
            typeName.EndsWith("Mediator") || typeName.EndsWith("Bus");

        // ------------------------------------------------------------ methods

        private static IEnumerable<MethodDeclarationSyntax> GetPublicMethods(TypeDeclarationSyntax type) =>
            type.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
                .Where(m => !(m.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                              (m.Identifier.Text is "ToString" or "Equals" or "GetHashCode")));

        private static bool IsCrudName(string name)
        {
            if (name.EndsWith("Async")) name = name.Substring(0, name.Length - "Async".Length);
            var m = Regex.Match(name, "^[A-Z][a-z0-9]*");
            return m.Success && CrudVerbs.Contains(m.Value);
        }

        private static bool IsQueryVerbName(string name)
        {
            if (name.EndsWith("Async")) name = name.Substring(0, name.Length - "Async".Length);
            var m = Regex.Match(name, "^[A-Z][a-z0-9]*");
            return m.Success && (m.Value is "Get" or "Find" or "Update");
        }

        private static bool CreatesOtherType(MethodDeclarationSyntax m, string ownName)
        {
            string ret = UnwrappedReturnName(m.ReturnType);
            if (ret is "void" or "bool" or "string" or "int" or "long" or "double" or "decimal" or "object")
                return false;
            if (ret == ownName) return false;
            return ReturnsNewInstanceOf(m, ret);
        }

        private static string UnwrappedReturnName(TypeSyntax t)
        {
            if (t is QualifiedNameSyntax q) return UnwrappedReturnName(q.Right);

            if (t is GenericNameSyntax g &&
                (g.Identifier.Text is "Task" or "ValueTask") &&
                g.TypeArgumentList.Arguments.Count == 1)
                return TypeName(g.TypeArgumentList.Arguments[0]);

            string n = TypeName(t);
            return n is "Task" or "ValueTask" ? "void" : n;
        }

        private static IEnumerable<ExpressionSyntax> ReturnedExpressions(MethodDeclarationSyntax m)
        {
            if (m.ExpressionBody != null) yield return m.ExpressionBody.Expression;

            if (m.Body != null)
            {
                foreach (var r in m.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
                    if (r.Expression != null) yield return r.Expression;
            }
        }

        // True if the method returns `new T(...)`, `new(...)`, Task.FromResult(new T(...)),
        // or a local variable that was initialised that way.
        private static bool ReturnsNewInstanceOf(MethodDeclarationSyntax m, string typeName)
        {
            var returned = ReturnedExpressions(m).ToList();
            if (returned.Count == 0) return false;

            var createdLocals = new HashSet<string>();
            if (m.Body != null)
            {
                foreach (var v in m.Body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    if (v.Initializer?.Value is { } init && IsCreation(init, typeName))
                        createdLocals.Add(v.Identifier.Text);
            }

            return returned.Any(e =>
                IsCreation(e, typeName) ||
                (Unparen(e) is IdentifierNameSyntax id && createdLocals.Contains(id.Identifier.Text)));
        }

        private static bool IsCreation(ExpressionSyntax? e, string typeName)
        {
            if (e is null) return false;
            e = Unparen(e);

            switch (e)
            {
                case ObjectCreationExpressionSyntax oc:
                    return TypeName(oc.Type) == typeName;
                case ImplicitObjectCreationExpressionSyntax:
                    return true;
                case InvocationExpressionSyntax inv
                    when inv.Expression is MemberAccessExpressionSyntax ma && ma.Name.Identifier.Text == "FromResult":
                    return inv.ArgumentList.Arguments.Any(a => IsCreation(a.Expression, typeName));
                default:
                    return false;
            }
        }

        private static ExpressionSyntax Unparen(ExpressionSyntax e)
        {
            while (e is ParenthesizedExpressionSyntax p) e = p.Expression;
            return e;
        }

        // ------------------------------------------------------------ names / attributes / types

        private static NameKind GetNameKind(string name) =>
            name.EndsWith("Repository") ? NameKind.Repository :
            name.EndsWith("Factory") ? NameKind.Factory :
            name.EndsWith("Service") ? NameKind.Service :
            NameKind.None;

        private static DomainModelKind? GetExplicitKind(TypeDeclarationSyntax type)
        {
            foreach (var attr in type.AttributeLists.SelectMany(l => l.Attributes))
            {
                string n = TypeName(attr.Name);
                if (n.EndsWith("Attribute")) n = n.Substring(0, n.Length - "Attribute".Length);

                switch (n)
                {
                    case "ValueObject": return DomainModelKind.ValueObject;
                    case "Entity":
                    case "AggregateRoot": return DomainModelKind.Entity;
                    case "DomainService": return DomainModelKind.DomainService;
                    case "Factory": return DomainModelKind.Factory;
                    case "Repository": return DomainModelKind.Repository;
                }
            }
            return null;
        }

        private static bool LooksLikeDomainType(string n) =>
            !NonDomainTypes.Contains(n) &&
            !Regex.IsMatch(n, "^I[A-Z]") &&
            !n.EndsWith("Exception") &&
            !n.EndsWith("Attribute");

        private static string TypeName(TypeSyntax t) => t switch
        {
            GenericNameSyntax g => g.Identifier.Text,
            IdentifierNameSyntax i => i.Identifier.Text,
            QualifiedNameSyntax q => TypeName(q.Right),
            AliasQualifiedNameSyntax aq => TypeName(aq.Name),
            NullableTypeSyntax nt => TypeName(nt.ElementType),
            PredefinedTypeSyntax pt => pt.Keyword.Text,
            ArrayTypeSyntax at => TypeName(at.ElementType) + "[]",
            _ => t.ToString()
        };

        // Simple type names referenced by a type, including generic arguments but not the
        // generic type itself (so Task<Human?> -> Human, List<Guid> -> Guid, System.Guid -> Guid).
        private static IEnumerable<string> TypeNamesIn(TypeSyntax t)
        {
            switch (t)
            {
                case IdentifierNameSyntax id:
                    yield return id.Identifier.Text;
                    break;
                case QualifiedNameSyntax q:
                    foreach (var n in TypeNamesIn(q.Right)) yield return n;
                    break;
                case GenericNameSyntax g:
                    foreach (var arg in g.TypeArgumentList.Arguments)
                        foreach (var n in TypeNamesIn(arg)) yield return n;
                    break;
                case NullableTypeSyntax nt:
                    foreach (var n in TypeNamesIn(nt.ElementType)) yield return n;
                    break;
                case ArrayTypeSyntax at:
                    foreach (var n in TypeNamesIn(at.ElementType)) yield return n;
                    break;
            }
        }
    }
}