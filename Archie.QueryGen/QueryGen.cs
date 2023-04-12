using Archie.QueryGen.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Archie.QueryGen
{

    [Generator(LanguageNames.CSharp)]
    public partial class QueryGen : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(PostInitCallback);
            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(InitialSyntaxFilter, Transform)
                .Where(static x => x.HasValue)
                .Select(static (x, y) => GatherFeatures(x!.Value, y))
                .WithComparer(RefStructDefContextEqualityComparer.Instance);

            context.RegisterSourceOutput(syntaxProvider, WriteQueryMethods);
        }

        /// <summary>
        /// Returns true on any partial ref structs that have attributes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        bool InitialSyntaxFilter(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is ClassDeclarationSyntax structDelc && structDelc.AttributeLists.Count > 0)
            {
                return structDelc.Modifiers.Any(SyntaxKind.PartialKeyword);
            }
            return false;
        }

        private (ClassDeclarationSyntax, INamedTypeSymbol)? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(context.Node is ClassDeclarationSyntax);
            ClassDeclarationSyntax structDelc = (ClassDeclarationSyntax)context.Node;
            var structSymbol = context.SemanticModel.GetDeclaredSymbol(structDelc)!;

            var markerAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Archie.CreateQueriesAttribute");

            var attribs = structSymbol.GetAttributes();
            //Check if our attribute is present
            if (attribs.Any(x => x.AttributeClass == markerAttribute))
            {
                return (structDelc, structSymbol);
            }
            return null;
        }

        static RefStructDefContext GatherFeatures((ClassDeclarationSyntax, INamedTypeSymbol) prev, CancellationToken cancellationToken)
        {
            var structSyntax = prev.Item1;
            var structType = prev.Item2;

            string? typeNamespace = structType.ContainingNamespace.IsGlobalNamespace ?
                null : structType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            string? shortTypeNamespace = structType.ContainingNamespace.IsGlobalNamespace ?
                null : structType.ContainingNamespace.ToDisplayString();

            string name = structType.Name;

            string typeName = structType.ToDisplayString();

            string shortTypeName = structType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return new RefStructDefContext(shortTypeNamespace, shortTypeName, typeName, structSyntax.GetParentClasses());
        }
        const string agressiveInlining = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        void WriteQueryMethods(SourceProductionContext context, RefStructDefContext classContext)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();

            writer.WriteTypeHierarchy(classContext.ShortNamespace, classContext.Parent, writer =>
            {
                writer.WriteClassDeclaration("partial", classContext.ShortName);
                writer.WriteOpenBrace();
                //Query(ComponentMask mask, Action<ComponentView<T1>...> forEach) 
                //ViewQuery(writer);
                //Query(ComponentMask mask, Action<ComponentRef<T1>...> forEach) 
                //RefQuery(writer);
                writer.WriteCloseBrace();
            });

            context.AddSource($"{classContext.FullName}.g.cs", writer.ToString());
        }

        void ViewQuery(CSharpCodeWriter writer)
        {
            for (int length = 1; length <= 10; length++)
            {
                writer.WriteLine(agressiveInlining);
                writer.WriteBeginMethodName("public", "void", "Query");
                writer.WriteGenerics(length);
                writer.WriteOpenParentheses();
                writer.WriteMethodArgument($"Archie.ComponentMask", "mask");
                writer.WriteComma();
                writer.Append($"System.Action<");
                writer.Append($"int");
                writer.WriteComma();
                for (int i = 0; i < length; i++)
                {
                    writer.Append($"T{i + 1}[]");

                    if (i != length - 1)
                    {
                        writer.WriteComma();
                    }
                }
                writer.Append($"> action");
                writer.WriteCloseParentheses();
                writer.WriteRepeatConstraint(length, (i) => $"struct, Archie.IComponent<T{i + 1}> ");
                writer.WriteOpenBrace();
                writer.WriteLine("var filter = GetFilter(mask);");
                writer.WriteLine("for (int i = 0; i < filter.MatchCount; i++)");
                writer.WriteOpenBrace();
                writer.WriteLine("var arch = filter.MatchingArchetypesBuffer[i];");
                writer.WriteLine("arch.Lock();");
                writer.Write("action.Invoke(");
                for (int i = 0; i < length; i++)
                {
                    writer.Append($"arch.GetPoolUnsafe<T{i + 1}>(0)");

                    if (i != length - 1)
                    {
                        writer.WriteComma();
                    }
                }
                writer.AppendLine(");");
                writer.WriteLine("arch.Unlock();");
                writer.WriteCloseBrace();
                writer.WriteCloseBrace();
            }
        }

        void RefQuery(CSharpCodeWriter writer)
        {
            writer.WriteLine("#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type");
            for (int length = 1; length <= 10; length++)
            {
                writer.WriteLine(agressiveInlining);
                writer.WriteBeginMethodName("public", "void", "Query");
                writer.WriteGenerics(length);
                writer.WriteOpenParentheses();
                writer.WriteMethodArgument($"Archie.ComponentMask", "mask");
                writer.WriteComma();
                writer.Append($"System.Action<");
                for (int i = 0; i < length; i++)
                {
                    writer.Append($"Archie.ComponentPoolSegment<T{i + 1}>");

                    if (i != length - 1)
                    {
                        writer.WriteComma();
                    }
                }
                writer.Append($"> action");
                writer.WriteCloseParentheses();
                writer.WriteRepeatConstraint(length, (i) => $"struct, Archie.IComponent<T{i + 1}> ");
                writer.WriteOpenBrace();
                writer.WriteLine("unsafe");
                writer.WriteOpenBrace();
                writer.WriteLine("var filter = GetFilter(mask);");
                writer.WriteLine("for (int i = 0; i < filter.MatchCount; i++)");
                writer.WriteOpenBrace();
                writer.WriteLine("var arch = filter.MatchingArchetypesBuffer[i];");
                writer.WriteLine("arch.Lock();");
                for (int i = 0; i < length; i++)
                {
                    writer.WriteLine($"var p{i + 1} = arch.GetPoolUnsafe<T{i + 1}>();");
                }
                writer.Write("action.Invoke(");
                for (int i = 0; i < length; i++)
                {
                    writer.Append($"p{i + 1}");

                    if (i != length - 1)
                    {
                        writer.WriteComma();
                    }
                }
                writer.AppendLine(");");
                writer.WriteLine("arch.Unlock();");
                writer.WriteCloseBrace();
                for (int i = 0; i < length; i++)
                {
                    writer.WriteCloseBrace();
                }
                writer.WriteCloseBrace();
                writer.WriteCloseBrace();
                writer.WriteCloseBrace();
            }
            writer.WriteLine("#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type");

        }

        void ComponentQuery(CSharpCodeWriter writer)
        {
            for (int length = 1; length <= 10; length++)
            {
                writer.WriteLine(agressiveInlining);
                writer.WriteBeginMethodName("public", "void", "Query");
                writer.WriteGenerics(length);
                writer.WriteOpenParentheses();
                writer.WriteMethodArgument($"Archie.ComponentMask", "mask");
                writer.WriteComma();
                for (int i = 0; i < length; i++)
                {
                    writer.WriteMethodArgument("int", "test");
                    if (i < length - 1)
                    {
                        writer.WriteComma();
                    }
                }
            }
        }

        private void PostInitCallback(IncrementalGeneratorPostInitializationContext obj)
        {
            obj.AddSource("Archie.CreateQueriesAttribute.g.cs", Attributes.CreateQueriesAttribute);
        }
    }
}
