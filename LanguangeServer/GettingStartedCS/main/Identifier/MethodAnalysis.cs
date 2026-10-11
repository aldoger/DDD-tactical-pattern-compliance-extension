using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.main.StructureInfo
{
    // =====================================================================
    //  Type classification  (PrimitiveTypes + checkNonPrimitiveType)
    // =====================================================================

    /// <summary>NonPrimitive: not in the primitive list. External: non-primitive AND not the current class.</summary>
    public readonly record struct TypeTraits(bool NonPrimitive, bool External);

    public sealed class TypeClassifier
    {
        private readonly HashSet<string> _primitives = new()
        {
            "void", "bool", "byte", "sbyte", "char", "decimal", "double", "float",
            "int", "uint", "nint", "nuint", "long", "ulong", "short", "ushort", "string",
            "Void", "Boolean", "Byte", "SByte", "Char", "Decimal", "Double", "Single",
            "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "String",
            // value-like BCL types that should not make a method a "collaborator"
            "DateTime", "DateTimeOffset", "TimeSpan", "Guid"
        };

        public TypeClassifier(IEnumerable<string>? extraPrimitives = null)
        {
            if (extraPrimitives != null)
                foreach (var p in extraPrimitives) _primitives.Add(p);
        }

        /// <summary>List&lt;Order&gt;[] -> List, System.Foo? -> Foo, int[] -> int</summary>
        public static string BaseName(string type)
        {
            string t = type.Trim();
            int lt = t.IndexOf('<');
            if (lt >= 0) t = t.Substring(0, lt);
            t = t.TrimEnd('?', '*', '[', ']', ',', ' ');
            int dot = t.LastIndexOfAny(new[] { '.', ':' });
            return dot >= 0 ? t.Substring(dot + 1) : t;
        }

        public TypeTraits Classify(string? type, string className)
        {
            if (string.IsNullOrWhiteSpace(type)) return default;
            string t = type.Trim();
            if (t == "var" || t == "dynamic" || t[0] == '(') return default;   // unknown / tuple

            string b = BaseName(t);
            if (b.Length == 0 || _primitives.Contains(b)) return default;
            return new TypeTraits(NonPrimitive: true, External: b != className);
        }

        public bool IsArrayOrPointer(string type) => type.Contains("[") || type.Contains("*");
    }

    public sealed class MethodAnalysisOptions
    {
        /// <summary>Calls ignored by the analysis (IgnorableCalls). Matches the full name or the last segment.</summary>
        public ISet<string> IgnoredCalls { get; init; } =
            new HashSet<string> { "nameof", "ToString", "GetType", "GetHashCode" };

        public TypeClassifier Types { get; init; } = new();
    }

    // =====================================================================
    //  Class context  (what classModel hands to findDataAfterCollection)
    // =====================================================================

    public sealed class ClassMemberContext
    {
        public string ClassName { get; }
        /// <summary>Fields + properties (+ positional record parameters): name -> declared type.</summary>
        public IReadOnlyDictionary<string, string> DataMembers { get; }
        public ISet<string> MethodNames { get; }

        public ClassMemberContext(string className, IReadOnlyDictionary<string, string> dataMembers, ISet<string> methodNames)
        {
            ClassName = className;
            DataMembers = dataMembers;
            MethodNames = methodNames;
        }

        public static ClassMemberContext From(TypeDeclarationSyntax type)
        {
            var members = new Dictionary<string, string>();
            var methods = new HashSet<string>();

            foreach (var m in type.Members)
            {
                switch (m)
                {
                    case FieldDeclarationSyntax f:
                        foreach (var v in f.Declaration.Variables)
                            members[v.Identifier.ValueText] = f.Declaration.Type.ToString();
                        break;
                    case PropertyDeclarationSyntax p:
                        members[p.Identifier.ValueText] = p.Type.ToString();
                        break;
                    case MethodDeclarationSyntax md:
                        methods.Add(md.Identifier.ValueText);
                        break;
                }
            }

            if (type is RecordDeclarationSyntax { ParameterList: not null } rec)
                foreach (var p in rec.ParameterList.Parameters)
                    members[p.Identifier.ValueText] = p.Type?.ToString() ?? "";

            return new ClassMemberContext(type.Identifier.ValueText, members, methods);
        }
    }

    // =====================================================================
    //  Extracted facts  (the private fields of methodModel)
    // =====================================================================

    public sealed class MethodFacts
    {
        public string Name = "";
        public string Signature = "";            // name/arity
        public string ReturnType = "";           // returnTypeParsed
        public bool IsStatic;
        public bool IsVoidPointer;               // return type is void*

        public int NonCommentStatements;
        public int DataMembersModified;          // distinct
        public int CallsOnDataMembers;           // field.Foo()
        public int CallsOnClassMethods;          // Foo(), this.Foo()
        public int ExternalMethodCalls;          // local.Foo(), param.Foo()
        public int ExternalFunctionCalls;        // Math.Round(), static / unknown calls
        public int NewConstructorCalls;          // new X()

        public bool DataMemberUsed;
        public bool ParameterUsed;
        public bool ParameterRefModified;
        public bool NonPrimitiveLocalOrParameterModified;

        public bool NonPrimitiveReturnType;
        public bool NonPrimitiveReturnTypeExternal;
        public bool NonPrimitiveDataMemberExternal;
        public bool NonPrimitiveLocalExternal;
        public bool NonPrimitiveParameterExternal;

        public bool SimpleReturn;                // return dm;
        public bool ComplexReturn;               // return dm + 5;
        public bool NewReturned;                 // return new X(...)
        public bool VariableCreatedAndReturnedWithNew;

        public List<string> ModifiedDataMembers { get; } = new();
        public List<string> CalledClassMethods { get; } = new();
    }

    // =====================================================================
    //  Extractor
    // =====================================================================

    public static class MethodFactsExtractor
    {
        public static MethodFacts Extract(MethodDeclarationSyntax method, ClassMemberContext cls,
                                          MethodAnalysisOptions? options = null)
            => new Analyzer(method, cls, options ?? new MethodAnalysisOptions()).Run();

        private enum Kind { None, Local, Parameter, DataMember, Other }

        private sealed record ParamInfo(string Type, bool IsRefOrOut);

        private sealed class Analyzer
        {
            private readonly MethodDeclarationSyntax _method;
            private readonly ClassMemberContext _cls;
            private readonly MethodAnalysisOptions _opt;
            private readonly MethodFacts _f = new();

            private readonly Dictionary<string, ParamInfo> _params = new();
            private readonly Dictionary<string, string> _locals = new();
            private readonly HashSet<string> _localFunctions = new();
            private readonly HashSet<string> _createdWithNew = new();
            private readonly HashSet<string> _modifiedMembers = new();
            private List<SyntaxNode> _nodes = new();

            public Analyzer(MethodDeclarationSyntax method, ClassMemberContext cls, MethodAnalysisOptions opt)
            {
                _method = method; _cls = cls; _opt = opt;
            }

            public MethodFacts Run()
            {
                ReadHeader();            // findName / findNameSignature / findReturnType
                ReadParameters();        // findParameterName / findParameterType

                SyntaxNode? body = (SyntaxNode?)_method.Body ?? _method.ExpressionBody;
                if (body is null) return _f;   // abstract / extern / interface -> 0 statements -> "empty"

                _nodes = body.DescendantNodes().ToList();

                CountStatements();       // findNonCommentStatements
                CollectLocals();         // findLocalVariableName / Type
                ClassifyTypes();         // findNonPrimitive
                CollectNewAssigned();    // findNewAssignedVariables
                CollectReturns();        // findReturnExpression / findReturnedVariables
                CollectCalls();          // findCallName / findIgnorableCalls / findCallsOnDataMembers
                CollectUsage();          // findExpressionNames / findVariablesInExpressions
                CollectModifications();  // findExpressionAssignments / findModifiedVariables
                return _f;
            }

            // ---------------- header ----------------

            private void ReadHeader()
            {
                _f.Name = _method.Identifier.ValueText;
                _f.Signature = $"{_f.Name}/{_method.ParameterList.Parameters.Count}";
                _f.IsStatic = _method.Modifiers.Any(SyntaxKind.StaticKeyword);

                TypeSyntax rt = _method.ReturnType;
                if (rt is RefTypeSyntax r) rt = r.Type;
                _f.ReturnType = rt.ToString().Trim();
                _f.IsVoidPointer = _f.ReturnType.Replace(" ", "").Contains("void*");
            }

            private void ReadParameters()
            {
                foreach (var p in _method.ParameterList.Parameters)
                {
                    bool refOut = p.Modifiers.Any(SyntaxKind.RefKeyword) || p.Modifiers.Any(SyntaxKind.OutKeyword);
                    _params[p.Identifier.ValueText] = new ParamInfo(p.Type?.ToString().Trim() ?? "", refOut);
                }
            }

            private void CountStatements()
            {
                _f.NonCommentStatements = _method.ExpressionBody != null
                    ? 1
                    : _nodes.OfType<StatementSyntax>().Count(s => s is not BlockSyntax and not EmptyStatementSyntax);
            }

            // ---------------- locals & types ----------------

            private void CollectLocals()
            {
                foreach (var n in _nodes)
                {
                    switch (n)
                    {
                        case VariableDeclaratorSyntax v when v.Parent is VariableDeclarationSyntax vd:
                            _locals.TryAdd(v.Identifier.ValueText, DeclaredType(vd.Type, v.Initializer?.Value));
                            break;

                        case ForEachStatementSyntax fe:
                            _locals.TryAdd(fe.Identifier.ValueText, fe.Type.IsVar ? "var" : fe.Type.ToString());
                            break;

                        // out var x, is Foo x, deconstruction
                        case SingleVariableDesignationSyntax d:
                            _locals.TryAdd(d.Identifier.ValueText,
                                d.Parent is DeclarationExpressionSyntax de && !de.Type.IsVar ? de.Type.ToString() : "var");
                            break;

                        // lambda and local-function parameters shadow data members too
                        case ParameterSyntax p:
                            _locals.TryAdd(p.Identifier.ValueText, p.Type?.ToString() ?? "var");
                            break;

                        case CatchDeclarationSyntax c when c.Identifier.ValueText.Length > 0:
                            _locals.TryAdd(c.Identifier.ValueText, c.Type.ToString());
                            break;

                        case LocalFunctionStatementSyntax lf:
                            _localFunctions.Add(lf.Identifier.ValueText);
                            break;
                    }
                }
            }

            private static string DeclaredType(TypeSyntax type, ExpressionSyntax? init)
            {
                if (!type.IsVar) return type.ToString();
                // var x = new Foo();  -> Foo      (anything else stays "var" = unknown)
                return Unwrap(init) is ObjectCreationExpressionSyntax oc ? oc.Type.ToString() : "var";
            }

            private void ClassifyTypes()
            {
                string cn = _cls.ClassName;
                var types = _opt.Types;

                var rt = types.Classify(_f.ReturnType, cn);
                _f.NonPrimitiveReturnType = rt.NonPrimitive;
                _f.NonPrimitiveReturnTypeExternal = rt.External;

                // Original overwrites these flags on each loop iteration (last one wins); "any" is what was intended.
                _f.NonPrimitiveLocalExternal = _locals.Values.Any(t => types.Classify(t, cn).External);
                _f.NonPrimitiveParameterExternal = _params.Values.Any(p => types.Classify(p.Type, cn).External);
            }

            // ---------------- new-assigned variables ----------------

            private void CollectNewAssigned()
            {
                foreach (var n in _nodes)
                {
                    if (n is VariableDeclaratorSyntax v && IsCreation(v.Initializer?.Value))
                    {
                        _createdWithNew.Add(v.Identifier.ValueText);
                    }
                    else if (n is AssignmentExpressionSyntax a && a.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsCreation(a.Right))
                    {
                        var (kind, name, _) = Resolve(Flatten(a.Left));
                        if (kind is Kind.Local or Kind.Parameter or Kind.DataMember)
                            _createdWithNew.Add(name);
                    }
                }
            }

            private static bool IsCreation(ExpressionSyntax? e)
                => Unwrap(e) is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax;

            // ---------------- returns ----------------

            private IEnumerable<ExpressionSyntax> ReturnExpressions()
            {
                if (_method.ExpressionBody is { } arrow)
                {
                    yield return arrow.Expression;
                    yield break;
                }
                foreach (var r in _nodes.OfType<ReturnStatementSyntax>())
                    if (r.Expression is { } e && BelongsToMethod(r))
                        yield return e;
            }

            // A return inside a lambda / local function returns from *that* function
            private bool BelongsToMethod(SyntaxNode n)
                => !n.Ancestors()
                     .TakeWhile(a => !ReferenceEquals(a, _method))
                     .Any(a => a is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

            private void CollectReturns()
            {
                foreach (var raw in ReturnExpressions())
                {
                    var expr = Unwrap(raw);
                    if (expr is null) continue;

                    if (expr is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax
                              or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax)
                        _f.NewReturned = true;

                    // `return this;` is neither a simple nor a complex return
                    if (expr is ThisExpressionSyntax) continue;

                    var segs = PlainReference(expr);
                    if (segs is not null)
                    {
                        var (kind, name, _) = Resolve(segs);

                        if ((kind is Kind.Local or Kind.Parameter or Kind.DataMember) && _createdWithNew.Contains(name))
                            _f.VariableCreatedAndReturnedWithNew = true;

                        if (kind == Kind.DataMember)   // return dm; / return this.dm; / return Foo.dm;
                        {
                            _f.SimpleReturn = true;
                            MarkDataMemberUsed(name);
                            continue;
                        }
                    }
                    _f.ComplexReturn = true;           // everything else, including `return localVar;`
                }
            }

            /// <summary>`a`, `this.a`, `base.a`, `ClassName.a` only. Anything longer is not a "simple" reference.</summary>
            private List<string>? PlainReference(ExpressionSyntax e)
            {
                switch (e)
                {
                    case IdentifierNameSyntax id:
                        return new() { id.Identifier.ValueText };

                    case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } ma
                        when ma.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                        if (ma.Expression is ThisExpressionSyntax or BaseExpressionSyntax)
                            return new() { "this", name.Identifier.ValueText };
                        if (ma.Expression is IdentifierNameSyntax cn && cn.Identifier.ValueText == _cls.ClassName)
                            return new() { _cls.ClassName, name.Identifier.ValueText };
                        return null;

                    default:
                        return null;
                }
            }

            // ---------------- calls ----------------

            private void CollectCalls()
            {
                foreach (var n in _nodes)
                {
                    if (n is InvocationExpressionSyntax inv)
                    {
                        if (IsIgnored(inv.Expression.ToString())) continue;

                        var segs = CallSegments(inv);
                        if (segs.Count == 0)                    // new X().Foo(), (expr)(), ...
                        {
                            _f.ExternalMethodCalls++;
                            continue;
                        }

                        string method = segs[^1];
                        var receiver = segs.Take(segs.Count - 1).ToList();

                        if (receiver.Count == 0)                // Foo()
                        {
                            if (_localFunctions.Contains(method)) continue;
                            ClassCall(method);
                        }
                        else if (receiver.Count == 1 &&
                                 (receiver[0] == "this" || receiver[0] == "base" || receiver[0] == _cls.ClassName))
                        {
                            ClassCall(method);                  // this.Foo(), base.Foo(), ClassName.Foo()
                        }
                        else
                        {
                            switch (Resolve(receiver).Kind)     // a.Foo(), this.a.Foo(), Math.Round()
                            {
                                case Kind.DataMember: _f.CallsOnDataMembers++; break;
                                case Kind.Local:
                                case Kind.Parameter: _f.ExternalMethodCalls++; break;
                                default: _f.ExternalFunctionCalls++; break;   // static / type
                            }
                        }
                    }
                    else if (n is ObjectCreationExpressionSyntax oc)
                    {
                        if (!IsIgnored(oc.Type.ToString())) _f.NewConstructorCalls++;
                    }
                    else if (n is ImplicitObjectCreationExpressionSyntax)
                    {
                        _f.NewConstructorCalls++;
                    }
                }
            }

            private void ClassCall(string name)
            {
                // Name-only match tolerates optional / params arguments (arity would not).
                if (_cls.MethodNames.Contains(name))
                {
                    _f.CallsOnClassMethods++;
                    _f.CalledClassMethods.Add(name);
                }
                else _f.ExternalFunctionCalls++;
            }

            private bool IsIgnored(string callText)
            {
                string t = string.Concat(callText.Where(c => !char.IsWhiteSpace(c)));
                int lt = t.IndexOf('<');
                if (lt >= 0) t = t.Substring(0, lt);
                if (_opt.IgnoredCalls.Contains(t)) return true;

                int dot = t.LastIndexOf('.');
                if (dot >= 0) t = t.Substring(dot + 1);
                return _opt.IgnoredCalls.Contains(t);
            }

            /// <summary>Identifier chain of the callee, e.g. this.a.Foo() -> [this, a, Foo]; a?.Foo() -> [a, Foo]</summary>
            private static List<string> CallSegments(InvocationExpressionSyntax inv)
            {
                var segs = Flatten(inv.Expression);
                if (segs.Count == 0 || !RootIsBinding(inv.Expression)) return segs;

                // a?.b?.Foo(): the receiver lives in the enclosing ConditionalAccess nodes
                foreach (var ca in inv.Ancestors().OfType<ConditionalAccessExpressionSyntax>())
                {
                    if (!ca.WhenNotNull.Span.Contains(inv.Span)) break;
                    segs.InsertRange(0, Flatten(ca.Expression));
                    if (!RootIsBinding(ca.Expression)) break;
                }
                return segs;
            }

            private static bool RootIsBinding(ExpressionSyntax? e)
            {
                while (true)
                {
                    switch (e)
                    {
                        case MemberBindingExpressionSyntax:
                        case ElementBindingExpressionSyntax: return true;
                        case MemberAccessExpressionSyntax ma: e = ma.Expression; break;
                        case InvocationExpressionSyntax i: e = i.Expression; break;
                        case ElementAccessExpressionSyntax ea: e = ea.Expression; break;
                        case ParenthesizedExpressionSyntax p: e = p.Expression; break;
                        default: return false;
                    }
                }
            }

            // ---------------- usage ("expression names") ----------------

            private void CollectUsage()
            {
                foreach (var id in _nodes.OfType<IdentifierNameSyntax>())
                {
                    if (!TryGetUsageSegments(id, out var segs)) continue;

                    var (kind, name, _) = Resolve(segs);
                    if (kind == Kind.DataMember) MarkDataMemberUsed(name);
                    else if (kind == Kind.Parameter) _f.ParameterUsed = true;
                }

                // `this` on its own counts as using the object state (return this; p = this;)
                foreach (var t in _nodes.OfType<ThisExpressionSyntax>())
                    if (t.Parent is not MemberAccessExpressionSyntax ma || !ReferenceEquals(ma.Expression, t))
                        _f.DataMemberUsed = true;
            }

            private bool TryGetUsageSegments(IdentifierNameSyntax id, out List<string> segs)
            {
                segs = new List<string>();
                string name = id.Identifier.ValueText;
                var p = id.Parent;

                // right-hand side of a member access: only this.a / ClassName.a name a member of *this* class
                if (p is MemberAccessExpressionSyntax ma && ReferenceEquals(ma.Name, id))
                {
                    if (ma.Expression is ThisExpressionSyntax or BaseExpressionSyntax)
                    {
                        segs.Add("this"); segs.Add(name); return true;
                    }
                    if (ma.Expression is IdentifierNameSyntax cn && cn.Identifier.ValueText == _cls.ClassName)
                    {
                        segs.Add(_cls.ClassName); segs.Add(name); return true;
                    }
                    return false;
                }

                if (p is MemberBindingExpressionSyntax) return false;                             // ?.Name
                if (p is InvocationExpressionSyntax inv && ReferenceEquals(inv.Expression, id)) return false; // Foo()
                if (p is NameColonSyntax or NameEqualsSyntax) return false;                       // named args
                if (IsTypePosition(id)) return false;

                // new X { Prop = 1 }  -> Prop belongs to X, not to this class
                if (p is AssignmentExpressionSyntax asg && ReferenceEquals(asg.Left, id) &&
                    asg.Parent is InitializerExpressionSyntax ie && ie.IsKind(SyntaxKind.ObjectInitializerExpression))
                    return false;

                segs.Add(name);
                return true;
            }

            private static bool IsTypePosition(IdentifierNameSyntax id) => id.Parent switch
            {
                VariableDeclarationSyntax vd => ReferenceEquals(vd.Type, id),
                ObjectCreationExpressionSyntax oc => ReferenceEquals(oc.Type, id),
                CastExpressionSyntax ce => ReferenceEquals(ce.Type, id),
                ForEachStatementSyntax fe => ReferenceEquals(fe.Type, id),
                DeclarationExpressionSyntax de => ReferenceEquals(de.Type, id),
                BinaryExpressionSyntax be => (be.IsKind(SyntaxKind.IsExpression) || be.IsKind(SyntaxKind.AsExpression))
                                                         && ReferenceEquals(be.Right, id),
                TypeOfExpressionSyntax or DefaultExpressionSyntax or SizeOfExpressionSyntax => true,
                ParameterSyntax or CatchDeclarationSyntax => true,
                TypeArgumentListSyntax or ArrayTypeSyntax or NullableTypeSyntax
                    or QualifiedNameSyntax or PointerTypeSyntax or RefTypeSyntax => true,
                DeclarationPatternSyntax or TypePatternSyntax or RecursivePatternSyntax => true,
                _ => false
            };

            private void MarkDataMemberUsed(string name)
            {
                _f.DataMemberUsed = true;
                if (_cls.DataMembers.TryGetValue(name, out var type) &&
                    _opt.Types.Classify(type, _cls.ClassName).External)
                    _f.NonPrimitiveDataMemberExternal = true;
            }

            // ---------------- modifications ----------------

            private void CollectModifications()
            {
                foreach (var n in _nodes)
                {
                    switch (n)
                    {
                        case AssignmentExpressionSyntax a:                                  // =, +=, ??=, ...
                            Target(a.Left);
                            break;

                        case PrefixUnaryExpressionSyntax pre when
                            pre.IsKind(SyntaxKind.PreIncrementExpression) || pre.IsKind(SyntaxKind.PreDecrementExpression):
                            Target(pre.Operand);
                            break;

                        case PostfixUnaryExpressionSyntax post when
                            post.IsKind(SyntaxKind.PostIncrementExpression) || post.IsKind(SyntaxKind.PostDecrementExpression):
                            Target(post.Operand);
                            break;

                        case ArgumentSyntax arg when
                            arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword):
                            Target(arg.Expression);
                            break;
                    }
                }

                _f.DataMembersModified = _modifiedMembers.Count;   // distinct, like the 'checked' set
                _f.ModifiedDataMembers.AddRange(_modifiedMembers);
            }

            private void Target(ExpressionSyntax e)
            {
                switch (e)
                {
                    case ParenthesizedExpressionSyntax p: Target(p.Expression); return;
                    case TupleExpressionSyntax t:
                        foreach (var arg in t.Arguments) Target(arg.Expression);
                        return;
                }

                var segs = Flatten(e);                       // empty for `out var x` etc. -> Kind.None
                var (kind, name, hasMember) = Resolve(segs);

                switch (kind)
                {
                    case Kind.DataMember:
                        _f.DataMemberUsed = true;
                        _modifiedMembers.Add(name);
                        break;

                    case Kind.Local:
                        if (_opt.Types.Classify(_locals[name], _cls.ClassName).NonPrimitive)
                            _f.NonPrimitiveLocalOrParameterModified = true;
                        break;

                    case Kind.Parameter:
                        ParameterModified(name, hasMember);
                        break;
                }
            }

            // findModifiedRefParameter (C# branch)
            private void ParameterModified(string name, bool propertyCheck)
            {
                var p = _params[name];
                var traits = _opt.Types.Classify(p.Type, _cls.ClassName);

                _f.ParameterUsed = true;
                if (traits.NonPrimitive) _f.NonPrimitiveLocalOrParameterModified = true;

                // ref/out/array/pointer: even `p = value` is visible to the caller.
                // Otherwise only `p.Member = value` on a reference type is (propertyCheck).
                if (p.IsRefOrOut || _opt.Types.IsArrayOrPointer(p.Type) || (traits.NonPrimitive && propertyCheck))
                    _f.ParameterRefModified = true;
            }

            // ---------------- name resolution (isVariableUsed) ----------------

            /// <summary>
            /// segs = [this,a,b] / [Foo,a] / [a,b] / [a].  Resolution order is the same as the original:
            /// this./ClassName. -> data member; otherwise local, then parameter, then data member, else Other.
            /// </summary>
            private (Kind Kind, string Name, bool HasMember) Resolve(IReadOnlyList<string> segs)
            {
                if (segs.Count == 0) return (Kind.None, "", false);

                string first = segs[0];
                bool viaThis = first == "this" || first == "base" || (first == _cls.ClassName && segs.Count > 1);
                if (viaThis)
                {
                    if (segs.Count == 1) return (Kind.None, first, false);   // bare this / base
                    string m = segs[1];
                    return (_cls.DataMembers.ContainsKey(m) ? Kind.DataMember : Kind.Other, m, segs.Count > 2);
                }

                bool hasMember = segs.Count > 1;
                if (_locals.ContainsKey(first)) return (Kind.Local, first, hasMember);
                if (_params.ContainsKey(first)) return (Kind.Parameter, first, hasMember);
                if (_cls.DataMembers.ContainsKey(first)) return (Kind.DataMember, first, hasMember);
                return (Kind.Other, first, hasMember);
            }

            // ---------------- syntax helpers ----------------

            private static ExpressionSyntax? Unwrap(ExpressionSyntax? e)
            {
                while (true)
                {
                    switch (e)
                    {
                        case ParenthesizedExpressionSyntax p: e = p.Expression; continue;
                        case RefExpressionSyntax r: e = r.Expression; continue;
                        case PrefixUnaryExpressionSyntax u when u.IsKind(SyntaxKind.PointerIndirectionExpression):
                            e = u.Operand; continue;
                        default: return e;
                    }
                }
            }

            /// <summary>Flattens an access chain into identifiers. Returns an empty list if the root is not a name.</summary>
            private static List<string> Flatten(ExpressionSyntax? e)
            {
                var list = new List<string>();
                return FlattenInto(e, list) ? list : new List<string>();
            }

            private static bool FlattenInto(ExpressionSyntax? e, List<string> list)
            {
                switch (e)
                {
                    case null: return false;
                    case IdentifierNameSyntax id: list.Add(id.Identifier.ValueText); return true;
                    case GenericNameSyntax g: list.Add(g.Identifier.ValueText); return true;
                    case ThisExpressionSyntax: list.Add("this"); return true;
                    case BaseExpressionSyntax: list.Add("base"); return true;
                    case ParenthesizedExpressionSyntax p: return FlattenInto(p.Expression, list);
                    case CastExpressionSyntax c: return FlattenInto(c.Expression, list);
                    case ElementAccessExpressionSyntax ea: return FlattenInto(ea.Expression, list);
                    case InvocationExpressionSyntax inv: return FlattenInto(inv.Expression, list);
                    case PostfixUnaryExpressionSyntax pu when pu.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                        return FlattenInto(pu.Operand, list);
                    case PrefixUnaryExpressionSyntax pre when pre.IsKind(SyntaxKind.PointerIndirectionExpression):
                        return FlattenInto(pre.Operand, list);
                    case MemberAccessExpressionSyntax ma:
                        if (!FlattenInto(ma.Expression, list)) return false;
                        list.Add(ma.Name.Identifier.ValueText);
                        return true;
                    case MemberBindingExpressionSyntax mb:
                        list.Add(mb.Name.Identifier.ValueText);
                        return true;
                    case ElementBindingExpressionSyntax:
                        return true;
                    case ConditionalAccessExpressionSyntax ca:
                        if (!FlattenInto(ca.Expression, list)) return false;
                        return FlattenInto(ca.WhenNotNull, list);
                    default:
                        return false;
                }
            }
        }
    }
}
