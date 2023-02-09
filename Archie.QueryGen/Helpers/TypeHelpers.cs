using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Archie.QueryGen.Helpers
{
    internal class ParentClass
    {
        public ParentClass(string keyword, string name, string constraints, ParentClass? child)
        {
            Keyword = keyword;
            Name = name;
            Constraints = constraints;
            Child = child;
        }

        public ParentClass? Child { get; }
        public string Keyword { get; }
        public string Name { get; }
        public string Constraints { get; }
    }

    internal static class TypeHelpers
    {
        // determine the namespace the class/enum/struct is declared in, if any
        public static string? GetNamespace(this BaseTypeDeclarationSyntax syntax)
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            string? nameSpace = null;

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we 
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace			
                    namespaceParent = parent; // Set first the namepaceParent to parent
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}"; // Should now work correctly
                }
            }

            // return the final namespace
            return nameSpace;
        }


        public static ParentClass? GetParentClasses(this BaseTypeDeclarationSyntax typeSyntax)
        {
            // Try and get the parent syntax. If it isn't a type like class/struct, this will be null
            TypeDeclarationSyntax? parentSyntax = typeSyntax.Parent as TypeDeclarationSyntax;
            ParentClass? parentClassInfo = null;

            // Keep looping while we're in a supported nested type
            while (parentSyntax != null && IsAllowedKind(parentSyntax.Kind()))
            {
                // Record the parent type keyword (class/struct etc), name, and constraints
                parentClassInfo = new ParentClass(
                    keyword: parentSyntax.Keyword.ValueText,
                    name: parentSyntax.Identifier.ToString() + parentSyntax.TypeParameterList,
                    constraints: parentSyntax.ConstraintClauses.ToString(),
                    child: parentClassInfo); // set the child link (null initially)

                // Move to the next outer type
                parentSyntax = (parentSyntax.Parent as TypeDeclarationSyntax);
            }

            // return a link to the outermost parent type
            return parentClassInfo;

        }

        // We can only be nested in class/struct/record
        public static bool IsAllowedKind(SyntaxKind kind) =>
            kind == SyntaxKind.ClassDeclaration ||
            kind == SyntaxKind.StructDeclaration ||
            kind == SyntaxKind.RecordDeclaration;

        public static void WriteTypeHierarchy(this CSharpCodeWriter writer, string? nameSpace, ParentClass? parentClass, Action<CSharpCodeWriter> body)
        {
            // If we don't have a namespace, generate the code in the "default"
            // namespace, either global:: or a different <RootNamespace>
            writer.WriteNamespace(nameSpace);
            int parentsCount = 0;
            // Loop through the full parent type hiearchy, starting with the outermost
            while (parentClass is not null)
            {
                writer.WriteTypeDeclaration("partial", parentClass.Keyword, parentClass.Name, parentClass.Constraints)
                .WriteOpenBrace();
                parentsCount++; // keep track of how many layers deep we are
                parentClass = parentClass.Child; // repeat with the next child
            }

            body.Invoke(writer);

            // We need to "close" each of the parent types, so write
            // the required number of '}'
            for (int i = 0; i < parentsCount; i++)
            {
                writer.WriteCloseBrace();
            }
        }
    }
}
