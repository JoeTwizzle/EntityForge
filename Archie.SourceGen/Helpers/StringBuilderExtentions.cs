//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;

//namespace Archie.SourceGen.Helpers
//{
//    internal static class StringBuilderExtentions
//    {
//        public static StringBuilder WriteNamespace(this StringBuilder sb, string? givenNamespace)
//        {
//            if (givenNamespace != null)
//            {
//                sb.Append("namespace ");
//                sb.Append(givenNamespace);
//                sb.AppendLine(";");
//            }
//            return sb;
//        }

//        public static StringBuilder WriteRefStructDeclaration(this StringBuilder sb, string accessibility, string name)
//        {
//            sb.Append("ref ");
//            sb.Append(accessibility);
//            sb.Append(" struct ");
//            sb.AppendLine(name);
//            return sb;
//        }

//        public static StringBuilder WriteStructDeclaration(this StringBuilder sb, string accessibility, string name)
//        {
//            sb.Append(accessibility);
//            sb.Append(" struct ");
//            sb.AppendLine(name);
//            return sb;
//        }

//        public static StringBuilder WriteClassDeclaration(this StringBuilder sb, string accessibility, string name)
//        {
//            sb.Append(accessibility);
//            sb.Append(" class ");
//            sb.AppendLine(name);
//            return sb;
//        }

//        public static StringBuilder WriteFieldDeclaration(this StringBuilder sb, string accessibility, string type, string name)
//        {
//            sb.Append(accessibility);
//            sb.Append(' ');
//            sb.Append(type);
//            sb.Append(' ');
//            sb.Append(name);
//            sb.Append(';');
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteFieldDeclaration(this StringBuilder sb, string accessibility, Type t, string name)
//        {
//            sb.Append(accessibility);
//            sb.Append(' ');
//            sb.Append(t.FullName);
//            sb.Append(' ');
//            sb.Append(name);
//            sb.Append(';');
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteFieldDeclaration<T>(this StringBuilder sb, string accessibility, string name)
//        {
//            sb.Append(accessibility);
//            sb.Append(' ');
//            sb.Append(nameof(T));
//            sb.Append(' ');
//            sb.Append(name);
//            sb.Append(';');
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteOpenBrace(this StringBuilder sb)
//        {
//            sb.Append('{');
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteCloseBrace(this StringBuilder sb)
//        {
//            sb.Append('}');
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteIf(this StringBuilder sb, string condition)
//        {
//            sb.Append("if(");
//            sb.Append(condition);
//            sb.Append(")");
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteWhile(this StringBuilder sb, string condition)
//        {
//            sb.Append("while(");
//            sb.Append(condition);
//            sb.Append(")");
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteFor(this StringBuilder sb, string contents)
//        {
//            sb.Append("for(");
//            sb.Append(contents);
//            sb.Append(")");
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteBeginMethodDeclaration(this StringBuilder sb, string accessibility, string returnType, string name)
//        {
//            sb.Append($"{accessibility} {returnType} {name}(");
//            return sb;
//        }

//        public static StringBuilder WriteMethodArgument(this StringBuilder sb, string type, string name)
//        {
//            sb.Append($"{type} {name}");
//            return sb;
//        }

//        public static StringBuilder WriteComma(this StringBuilder sb)
//        {
//            sb.Append(", ");
//            return sb;
//        }

//        public static StringBuilder WriteEndMethodDeclaration(this StringBuilder sb)
//        {
//            sb.Append(")");
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteMethodDeclaration(this StringBuilder sb, string accessibility, string name, params string[] args)
//        {
//            sb.Append($"{accessibility} {name}(");
//            for (int i = 0; i < args.Length; i++)
//            {
//                sb.Append(args[i]);
//                if (i != args.Length - 1)
//                {
//                    sb.Append(',');
//                }
//                else
//                {
//                    sb.Append(')');
//                }
//            }
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteGenericMethodDeclaration(this StringBuilder sb, string accessibility, string name, int genericCount, params string[] args)
//        {
//            sb.Append($"{accessibility} {name}<");
//            for (int i = 0; i < genericCount; i++)
//            {
//                sb.Append($"T{i + 1}");
//                if (i != genericCount - 1)
//                {
//                    sb.Append(',');
//                }
//            }
//            sb.Append(">(");
//            for (int i = 0; i < args.Length; i++)
//            {
//                sb.Append(args[i]);
//                if (i != args.Length - 1)
//                {
//                    sb.Append(',');
//                }
//                else
//                {
//                    sb.Append(')');
//                }
//            }
//            sb.AppendLine();
//            return sb;
//        }

//        public static StringBuilder WriteGenericMethodDeclarationWithConstraint(this StringBuilder sb, string accessibility, string name, int genericCount, string[] constraints, params string[] args)
//        {
//            sb.Append($"{accessibility} {name}<");
//            for (int i = 0; i < genericCount; i++)
//            {
//                sb.Append($"T{i + 1}");
//                if (i != genericCount - 1)
//                {
//                    sb.Append(',');
//                }
//            }
//            sb.Append(">(");
//            for (int i = 0; i < args.Length; i++)
//            {
//                sb.Append(args[i]);
//                if (i != args.Length - 1)
//                {
//                    sb.Append(',');
//                }
//                else
//                {
//                    sb.Append(')');
//                }
//            }
//            if (constraints.Length > 0)
//            {
//                sb.Append("where ");
//                for (int i = 0; i < constraints.Length; i++)
//                {
//                    sb.Append(constraints[i]);
//                    if (i != constraints.Length - 1)
//                    {
//                        sb.Append(',');
//                    }
//                }
//            }
//            sb.AppendLine();
//            return sb;
//        }
//    }
//}
