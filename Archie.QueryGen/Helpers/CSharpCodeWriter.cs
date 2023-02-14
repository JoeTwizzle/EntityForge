using System;
using System.Text;

namespace Archie.QueryGen.Helpers
{
    internal sealed class CSharpCodeWriter
    {
        private readonly StringBuilder sb = new StringBuilder();
        public int IndentationLevel;

        public CSharpCodeWriter Indent()
        {
            IndentationLevel++;
            return this;
        }

        public CSharpCodeWriter Unindent()
        {
            IndentationLevel--;
            return this;
        }

        public CSharpCodeWriter WriteLine()
        {
            WriteIndent();
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteLine(string text)
        {
            WriteIndent();
            sb.AppendLine(text);
            return this;
        }

        public CSharpCodeWriter WriteIndent()
        {
            for (int i = 0; i < IndentationLevel; i++)
            {
                sb.Append("    ");
            }
            return this;
        }
        public CSharpCodeWriter Write(char c)
        {
            WriteIndent();
            sb.Append(c);
            return this;
        }

        public CSharpCodeWriter Write(string? text)
        {
            WriteIndent();
            sb.Append(text);
            return this;
        }

        public CSharpCodeWriter Append(char c)
        {
            sb.Append(c);
            return this;
        }

        public CSharpCodeWriter Append(string? text)
        {
            sb.Append(text);
            return this;
        }

        public CSharpCodeWriter AppendLine(string? text)
        {
            sb.AppendLine(text);
            return this;
        }

        public CSharpCodeWriter WriteNamespace(string? givenNamespace)
        {
            WriteIndent();
            if (!string.IsNullOrEmpty(givenNamespace))
            {
                WriteIndent();
                sb.Append("namespace ");
                sb.Append(givenNamespace);
                WriteLine(";");
            }
            return this;
        }

        public CSharpCodeWriter WriteTypeDeclaration(string accessibility, string keyword, string name, string constraints = "")
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(' ');
            sb.Append(keyword);
            sb.Append(' ');
            sb.Append(name);
            sb.Append(' ');
            sb.AppendLine(constraints);
            return this;
        }

        public CSharpCodeWriter WriteRefStructDeclaration(string accessibility, bool partial, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            if (!string.IsNullOrWhiteSpace(accessibility))
            {
                sb.Append(' ');
            }
            sb.Append("ref");
            if (partial)
            {
                sb.Append(' ');
            }
            sb.Append(partial ? "partial" : "");
            sb.Append(" struct ");
            sb.AppendLine(name);
            return this;
        }

        public CSharpCodeWriter WriteStructDeclaration(string accessibility, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(" struct ");
            sb.AppendLine(name);
            return this;
        }

        public CSharpCodeWriter WriteClassDeclaration(string accessibility, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(" class ");
            sb.AppendLine(name);
            return this;
        }

        public CSharpCodeWriter WriteBeginFieldDeclaration(string accessibility, string type, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(' ');
            sb.Append(type);
            sb.Append(' ');
            sb.Append(name);
            sb.Append(' ');
            return this;
        }

        public CSharpCodeWriter WriteAssignment(string content)
        {
            sb.Append('=');
            sb.Append(' ');
            sb.Append(content);
            return this;
        }

        public CSharpCodeWriter WriteEndFieldDeclaration()
        {
            sb.Append(';');
            WriteLine();
            return this;
        }

        public CSharpCodeWriter WriteFieldDeclaration(string accessibility, string type, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(' ');
            sb.Append(type);
            sb.Append(' ');
            sb.Append(name);
            sb.Append(';');
            WriteLine();
            return this;
        }

        public CSharpCodeWriter WriteFieldDeclaration(string accessibility, Type t, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(' ');
            sb.Append(t.FullName);
            sb.Append(' ');
            sb.Append(name);
            sb.Append(';');
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteFieldDeclaration<T>(string accessibility, string name)
        {
            WriteIndent();
            sb.Append(accessibility);
            sb.Append(' ');
            sb.Append(nameof(T));
            sb.Append(' ');
            sb.Append(name);
            sb.Append(';');
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteOpenBrace()
        {
            WriteIndent();
            sb.Append('{');
            Indent();
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteCloseBrace()
        {
            Unindent();
            WriteIndent();
            sb.Append('}');
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteLessThan()
        {
            sb.Append('<');
            return this;
        }

        public CSharpCodeWriter WriteGreaterThan()
        {
            sb.Append('>');
            return this;
        }

        public CSharpCodeWriter WriteOpenParentheses()
        {
            sb.Append('(');
            return this;
        }

        public CSharpCodeWriter WriteCloseParentheses()
        {
            sb.Append(')');
            return this;
        }

        public CSharpCodeWriter WriteIf(string condition)
        {
            WriteIndent();
            sb.Append("if(");
            sb.Append(condition);
            sb.Append(")");
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteWhile(string condition)
        {
            WriteIndent();
            sb.Append("while(");
            sb.Append(condition);
            sb.Append(")");
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteFor(string contents)
        {
            WriteIndent();
            sb.Append("for(");
            sb.Append(contents);
            sb.Append(")");
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteBeginConstructorDeclaration(string accessibility, string name)
        {
            WriteIndent();
            sb.Append($"{accessibility} {name}(");
            return this;
        }

        public CSharpCodeWriter WriteEndConstructorDeclaration()
        {
            return WriteEndMethodDeclaration();
        }

        public CSharpCodeWriter WriteBeginMethodDeclaration(string accessibility, string returnType, string name)
        {
            WriteIndent();
            sb.Append($"{accessibility} {returnType} {name}(");
            return this;
        }

        public CSharpCodeWriter WriteBeginMethodName(string accessibility, string returnType, string name)
        {
            WriteIndent();
            sb.Append($"{accessibility} {returnType} {name}");
            return this;
        }

        public CSharpCodeWriter WriteGenerics(int count)
        {
            WriteLessThan();
            for (int i = 0; i < count; i++)
            {
                sb.Append($"T{i + 1}");
                if (i != count - 1)
                {
                    WriteComma();
                }
            }
            WriteGreaterThan();
            return this;
        }

        public CSharpCodeWriter WriteRepeatConstraint(int count, string constraint)
        {
            for (int i = 0; i < count; i++)
            {
                sb.Append($" where T{i + 1} : {constraint}");
            }
            WriteLine();
            return this;
        }

        public CSharpCodeWriter WriteRepeatConstraint(int count, Func<int, string> builder)
        {
            for (int i = 0; i < count; i++)
            {
                sb.Append($" where T{i + 1} : {builder.Invoke(i)}");
            }
            WriteLine();
            return this;
        }

        public CSharpCodeWriter WriteMethodArgument(string type, string name)
        {
            sb.Append($"{type} {name}");
            return this;
        }

        public CSharpCodeWriter WriteComma()
        {
            sb.Append(", ");
            return this;
        }

        public CSharpCodeWriter WriteEndMethodDeclaration()
        {
            sb.Append(")");
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteMethodDeclaration(string accessibility, string name, params string[] args)
        {
            WriteIndent();
            sb.Append($"{accessibility} {name}(");
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i]);
                if (i != args.Length - 1)
                {
                    sb.Append(',');
                }
                else
                {
                    sb.Append(')');
                }
            }
            sb.AppendLine();
            return this;
        }

        public CSharpCodeWriter WriteGenericMethodDeclaration(string accessibility, string name, int genericCount, params string[] args)
        {
            WriteIndent();
            sb.Append($"{accessibility} {name}<");
            for (int i = 0; i < genericCount; i++)
            {
                sb.Append($"T{i + 1}");
                if (i != genericCount - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(">(");
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i]);
                if (i != args.Length - 1)
                {
                    sb.Append(',');
                }
                else
                {
                    sb.Append(')');
                }
            }
            sb.AppendLine();
            return this;
        }


        public CSharpCodeWriter WriteGenericMethodDeclarationWithConstraint(string accessibility, string name, int genericCount, string[] constraints, params string[] args)
        {
            WriteIndent();
            sb.Append($"{accessibility} {name}<");
            for (int i = 0; i < genericCount; i++)
            {
                sb.Append($"T{i + 1}");
                if (i != genericCount - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(">(");
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i]);
                if (i != args.Length - 1)
                {
                    sb.Append(',');
                }
                else
                {
                    sb.Append(')');
                }
            }
            if (constraints.Length > 0)
            {
                sb.Append("where ");
                for (int i = 0; i < constraints.Length; i++)
                {
                    sb.Append(constraints[i]);
                    if (i != constraints.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
            }
            sb.AppendLine();
            return this;
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}
