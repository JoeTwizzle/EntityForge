using Archie.SourceGen.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Archie.SourceGen
{

    [Generator(LanguageNames.CSharp)]
    public partial class GroupGen : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(PostInitCallback);
            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(InitialSyntaxFilter, Transform)
                .Where(static x => x.HasValue && x.Value.Item3.Length > 0)
                .Select(static (x, y) => GatherFeatures(x!.Value, y))
                .WithComparer(RefStructDefContextEqualityComparer.Instance);

            context.RegisterSourceOutput(syntaxProvider, WriteGroupConstructor);
        }

        /// <summary>
        /// Returns true on any partial ref structs that have attributes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        bool InitialSyntaxFilter(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is StructDeclarationSyntax structDelc && structDelc.AttributeLists.Count > 0)
            {
                return structDelc.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                    structDelc.Modifiers.Any(SyntaxKind.RefKeyword);
            }
            return false;
        }

        private (StructDeclarationSyntax, INamedTypeSymbol, ImmutableArray<IFieldSymbol>)? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(context.Node is StructDeclarationSyntax);
            StructDeclarationSyntax structDelc = (StructDeclarationSyntax)context.Node;
            var structSymbol = context.SemanticModel.GetDeclaredSymbol(structDelc)!;

            var markerAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Archie.InjectTypesAttribute");

            var attribs = structSymbol.GetAttributes();
            //Check if our attribute is present
            if (attribs.Any(x => x.AttributeClass == markerAttribute))
            {
                //if (!Debugger.IsAttached)
                //{
                //    Debugger.Launch();
                //}
                var componentTypeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName($"Archie.IComponent`1")!;
                var injectedFields = structSymbol.GetMembers()
                    .Where(x =>
                    {
                        if (x.Kind == SymbolKind.Field)
                        {
                            IFieldSymbol? field = (IFieldSymbol)x;

                            var interfaces = field.Type.AllInterfaces;
                            return !x.IsStatic
                            && (field.RefKind == RefKind.Ref || field.RefKind == RefKind.RefReadOnly)
                            && interfaces.Any(x => x.ConstructedFrom == componentTypeSymbol);
                        }
                        return false;

                    }).Select(x => (IFieldSymbol)x).ToImmutableArray();


                return (structDelc, structSymbol, injectedFields);
            }
            return null;
        }

        static RefStructDefContext GatherFeatures((StructDeclarationSyntax, INamedTypeSymbol, ImmutableArray<IFieldSymbol>) prev, CancellationToken cancellationToken)
        {
            var structSyntax = prev.Item1;
            var structType = prev.Item2;
            var injectedFields = prev.Item3;

            string? typeNamespace = structType.ContainingNamespace.IsGlobalNamespace ?
                null : structType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            string? shortTypeNamespace = structType.ContainingNamespace.IsGlobalNamespace ?
                null : structType.ContainingNamespace.ToDisplayString();

            string name = structType.Name;

            string typeName = structType.ToDisplayString();

            string shortTypeName = structType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var memberNames = injectedFields.Select(x => x.Name).ToImmutableArray();

            var memberTypes = injectedFields.Select(x => (x.Type.ToDisplayString())).ToImmutableArray();

            return new RefStructDefContext(shortTypeNamespace, shortTypeName, typeName, structSyntax.GetParentClasses(), new MemberContext(memberNames, memberTypes));
        }

        void WriteGroupConstructor(SourceProductionContext context, RefStructDefContext structContext)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            string agressiveInlining = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
            writer.WriteTypeHierarchy(structContext.ShortNamespace, structContext.Parent, writer =>
            {
                writer.WriteRefStructDeclaration("", true, structContext.ShortName);
                writer.WriteOpenBrace();
                writer.WriteLine(agressiveInlining);
                writer.WriteBeginConstructorDeclaration("public", structContext.ShortName);
                var memberContext = structContext.MemberContext;
                int length = memberContext.MemberNames.Length;
                for (int i = 0; i < length; i++)
                {
                    writer.WriteMethodArgument($"ref {memberContext.MemberTypes[i]}", memberContext.MemberNames[i]);
                    if (i != length - 1)
                    {
                        writer.WriteComma();
                    }
                }
                writer.WriteEndConstructorDeclaration();
                writer.WriteOpenBrace();
                for (int i = 0; i < length; i++)
                {
                    writer.WriteLine($"this.{memberContext.MemberNames[i]} = ref {memberContext.MemberNames[i]};");
                }
                writer.WriteCloseBrace();
                WriteCreateMethod(writer, structContext);
                WriteIterator(writer, structContext);
                writer.WriteCloseBrace();
            });

            context.AddSource($"{structContext.FullName}.g.cs", writer.ToString());
        }

        void WriteCreateMethod(CSharpCodeWriter writer, RefStructDefContext structContext)
        {
            var distinctTypes = structContext.MemberContext.MemberTypes.Distinct().ToImmutableArray();

            writer.WriteBeginFieldDeclaration("public static readonly", "Archie.ComponentMask", "Mask")
            .WriteAssignment("Archie.ComponentMask.Create()");
            for (int i = 0; i < distinctTypes.Length; i++)
            {
                writer.Append($".Inc<{distinctTypes[i]}>()");
            }
            writer.Append($".End()");
            writer.WriteEndFieldDeclaration();

            writer.WriteLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteBeginMethodDeclaration("public static", structContext.ShortName, "Create");
            var memberContext = structContext.MemberContext;

            writer.WriteMethodArgument("Archie.Archetype", "archetype").WriteComma()
               .WriteMethodArgument("int", "index");

            writer.WriteEndMethodDeclaration();
            writer.WriteOpenBrace();
            writer.Write($"return new {structContext.ShortName}(");
            int length = memberContext.MemberTypes.Length;
            for (int i = 0; i < length; i++)
            {
                writer.Append($"ref archetype.GetComponent<{memberContext.MemberTypes[i]}>(index)");
                if (i != length - 1)
                {
                    writer.WriteComma();
                }
            }
            writer.AppendLine(");");
            writer.WriteCloseBrace();
        }

        void WriteIterator(CSharpCodeWriter writer, RefStructDefContext structContext)
        {
            var distinctTypes = structContext.MemberContext.MemberTypes.Distinct().ToImmutableArray();
            string iteratorBaseName = $"{structContext.ShortName}IteratorBase";
            string iteratorName = $"{structContext.ShortName}Iterator";
            string agressiveInlining = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
            //struct
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginMethodDeclaration("public static", iteratorBaseName, "GetIterator")
            .WriteMethodArgument("Archie.Archetype", "archetype")
            .WriteEndMethodDeclaration();

            var memberContext = structContext.MemberContext;
            writer.WriteOpenBrace();
            writer.WriteLine($"return new {iteratorBaseName}(archetype);");
            writer.WriteCloseBrace();

            //IteratorBase
            writer.WriteLine();
            writer.WriteRefStructDeclaration("public", false, iteratorBaseName);
            writer.WriteOpenBrace();
            writer.WriteFieldDeclaration("readonly", "Archie.Archetype", "archetype");
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginConstructorDeclaration("public", iteratorBaseName)
            .WriteMethodArgument("Archie.Archetype", "archetype")
            .WriteEndConstructorDeclaration();
            writer.WriteOpenBrace();
            writer.WriteLine("this.archetype = archetype;");
            writer.WriteCloseBrace();
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginMethodDeclaration("public", iteratorName, "GetEnumerator")
            .WriteEndMethodDeclaration();
            writer.WriteOpenBrace();
            writer.WriteLine($"return new {iteratorName}(archetype);");
            writer.WriteCloseBrace();

            writer.WriteCloseBrace();
            //Iterator
            writer.WriteLine();
            writer.WriteRefStructDeclaration("public", false, iteratorName);
            writer.WriteOpenBrace();
            //fields
            for (int i = 0; i < memberContext.MemberTypes.Length; i++)
            {
                writer.WriteFieldDeclaration("private", $"ref {memberContext.MemberTypes[i]}", $"data{i}");
            }
            writer.WriteFieldDeclaration("private", $"ref {memberContext.MemberTypes[0]}", "end");
            writer.WriteFieldDeclaration("private", "int", "length");
            //ctor
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginConstructorDeclaration("public", iteratorName)
            .WriteMethodArgument("Archie.Archetype", "archetype")
            .WriteEndConstructorDeclaration();
            writer.WriteOpenBrace();
            writer.WriteLine($"length = archetype.EntityCount;");
            for (int i = 0; i < memberContext.MemberTypes.Length; i++)
            {
                writer.WriteLine($"data{i} = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(archetype.GetPool<{memberContext.MemberTypes[i]}>());");
            }
            writer.WriteLine($"end = ref System.Runtime.CompilerServices.Unsafe.Add(ref data0, length);");
            writer.WriteCloseBrace();
            writer.WriteLine();
            //Current
            writer.WriteLine($"public {structContext.ShortName} Current");
            writer.WriteOpenBrace();
            writer.WriteLine(agressiveInlining);
            writer.WriteLine("get");
            writer.WriteOpenBrace();
            writer.Write($"return new {structContext.ShortName}(");
            for (int i = 0; i < memberContext.MemberTypes.Length; i++)
            {
                writer.Append($"ref data{i}");
                if (i != memberContext.MemberTypes.Length - 1)
                {
                    writer.WriteComma();
                }
            }
            writer.AppendLine(");");
            writer.WriteCloseBrace();
            writer.WriteCloseBrace();
            writer.WriteLine();
            //MoveNext
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginMethodDeclaration("public", "bool", "MoveNext")
            .WriteEndMethodDeclaration();
            writer.WriteOpenBrace();
            writer.WriteLine($"bool result = System.Runtime.CompilerServices.Unsafe.IsAddressLessThan(ref data0, ref end);");
            writer.WriteIf("result");
            writer.WriteOpenBrace();
            for (int i = 0; i < memberContext.MemberTypes.Length; i++)
            {
                writer.WriteLine($"data{i} = ref System.Runtime.CompilerServices.Unsafe.Add(ref data{i}, 1);");
            }
            writer.WriteCloseBrace();
            writer.WriteLine($"return result;");
            writer.WriteCloseBrace();
            writer.WriteLine();
            //Reset
            writer.WriteLine(agressiveInlining);
            writer.WriteBeginMethodDeclaration("public", "void", "Reset")
            .WriteEndMethodDeclaration();
            writer.WriteOpenBrace();
            for (int i = 0; i < memberContext.MemberTypes.Length; i++)
            {
                writer.WriteLine($"data{i} = ref System.Runtime.CompilerServices.Unsafe.Subtract(ref data{i}, length);");
            }
            writer.WriteCloseBrace();

            writer.WriteCloseBrace();
        }

        //void WriteWorldIterator(CSharpCodeWriter writer, RefStructDefContext structContext)
        //{
        //    var distinctTypes = structContext.MemberContext.MemberTypes.Distinct().ToImmutableArray();
        //    string iteratorBaseName = $"{structContext.ShortName}WorldIteratorBase";
        //    string iteratorName = $"{structContext.ShortName}WorldIterator";
        //    string agressiveInlining = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        //    //struct
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginMethodDeclaration("public static", iteratorBaseName, "GetIterator")
        //    .WriteMethodArgument("Archie.World", "world")
        //    .WriteComma()
        //    .WriteMethodArgument("Archie.ComponentMask", "mask")
        //    .WriteEndMethodDeclaration();

        //    var memberContext = structContext.MemberContext;
        //    writer.WriteOpenBrace();
        //    writer.WriteLine($"return new {iteratorBaseName}(world, mask);");
        //    writer.WriteCloseBrace();

        //    //IteratorBase
        //    writer.WriteLine();
        //    writer.WriteRefStructDeclaration("public", false, iteratorBaseName);
        //    writer.WriteOpenBrace();
        //    writer.WriteFieldDeclaration("readonly", "Archie.EntityFilter", "filter");
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginConstructorDeclaration("public", iteratorBaseName)
        //    .WriteMethodArgument("Archie.World", "world")
        //    .WriteComma()
        //    .WriteMethodArgument("Archie.ComponentMask", "mask")
        //    .WriteEndConstructorDeclaration();
        //    writer.WriteOpenBrace();
        //    writer.WriteLine("this.filter = world.GetFilter(mask);");
        //    writer.WriteCloseBrace();
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginMethodDeclaration("public", iteratorName, "GetEnumerator")
        //    .WriteEndMethodDeclaration();
        //    writer.WriteOpenBrace();
        //    writer.WriteLine($"return new {iteratorName}(filter.MatchingArchetypes);");
        //    writer.WriteCloseBrace();

        //    writer.WriteCloseBrace();
        //    //Iterator
        //    writer.WriteLine();
        //    writer.WriteRefStructDeclaration("public", false, iteratorName);
        //    writer.WriteOpenBrace();
        //    //fields
        //    for (int i = 0; i < memberContext.MemberTypes.Length; i++)
        //    {
        //        writer.WriteFieldDeclaration("private", $"ref {memberContext.MemberTypes[i]}", $"data{i}");
        //    }
        //    writer.WriteFieldDeclaration("private", $"ref {memberContext.MemberTypes[0]}", "end");
        //    writer.WriteFieldDeclaration("private", "System.Span<Archie.Archetype>", "archetypes");
        //    writer.WriteFieldDeclaration("private", "int", "currentArchetype");
        //    //ctor
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginConstructorDeclaration("public", iteratorName)
        //    .WriteMethodArgument("System.Span<Archie.Archetype>", "archetypes")
        //    .WriteEndConstructorDeclaration();
        //    writer.WriteOpenBrace();
        //    writer.WriteLine("this.archetypes = archetypes;");
        //    writer.WriteLine("currentArchetype = 0;");
        //    writer.WriteLine("InitSelectedArchetype();");
        //    writer.WriteCloseBrace();
        //    writer.WriteLine();
        //    //InitSelectedArchetype
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginMethodDeclaration("private", "void", "InitSelectedArchetype")
        //    .WriteEndMethodDeclaration();
        //    writer.WriteOpenBrace();
        //    writer.WriteLine($"var archetype = archetypes[currentArchetype];");
        //    writer.WriteLine($"var length = archetype.EntityCount;");
        //    for (int i = 0; i < memberContext.MemberTypes.Length; i++)
        //    {
        //        writer.WriteLine($"data{i} = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(archetype.GetPool<{memberContext.MemberTypes[i]}>());");
        //    }
        //    writer.WriteLine($"end = ref System.Runtime.CompilerServices.Unsafe.Add(ref data0, length);");
        //    writer.WriteCloseBrace();
        //    //Current
        //    writer.WriteLine($"public {structContext.ShortName} Current");
        //    writer.WriteOpenBrace();
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteLine("get");
        //    writer.WriteOpenBrace();
        //    writer.Write($"return new {structContext.ShortName}(");
        //    for (int i = 0; i < memberContext.MemberTypes.Length; i++)
        //    {
        //        writer.Append($"ref data{i}");
        //        if (i != memberContext.MemberTypes.Length - 1)
        //        {
        //            writer.WriteComma();
        //        }
        //    }
        //    writer.AppendLine(");");
        //    writer.WriteCloseBrace();
        //    writer.WriteCloseBrace();
        //    writer.WriteLine();
        //    //MoveNext
        //    writer.WriteLine(agressiveInlining);
        //    writer.WriteBeginMethodDeclaration("public", "bool", "MoveNext")
        //    .WriteEndMethodDeclaration();
        //    writer.WriteOpenBrace();
        //    writer.WriteLine($"bool result = System.Runtime.CompilerServices.Unsafe.IsAddressLessThan(ref data0, ref end);");
        //    writer.WriteIf("result");
        //    writer.WriteOpenBrace();
        //    for (int i = 0; i < memberContext.MemberTypes.Length; i++)
        //    {
        //        writer.WriteLine($"data{i} = ref System.Runtime.CompilerServices.Unsafe.Add(ref data{i}, 1);");
        //    }
        //    writer.WriteCloseBrace();
        //    writer.WriteLine("else");
        //    writer.WriteOpenBrace();
        //    writer.WriteLine("result = ++currentArchetype < archetypes.Length;");
        //    writer.WriteIf("result");
        //    writer.WriteOpenBrace();
        //    writer.WriteLine("InitSelectedArchetype();");
        //    writer.WriteLine("result &= System.Runtime.CompilerServices.Unsafe.IsAddressLessThan(ref data0, ref end);");
        //    writer.WriteCloseBrace();
        //    writer.WriteCloseBrace();
        //    writer.WriteLine($"return result;");
        //    writer.WriteCloseBrace();
        //    writer.WriteLine();
        //    writer.WriteCloseBrace();
        //}


        private void PostInitCallback(IncrementalGeneratorPostInitializationContext obj)
        {
            obj.AddSource("Archie.InjectTypesAttribute.g.cs", Attributes.InjectTypesAttribute);
        }
    }
}
