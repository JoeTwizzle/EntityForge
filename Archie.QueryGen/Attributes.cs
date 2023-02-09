namespace Archie.QueryGen
{
    internal class Attributes
    {
        public static readonly string CreateQueriesAttribute = $$""" 
        //Auto generated
        using System;
        namespace Archie;

        [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        public sealed class CreateQueriesAttribute : global::System.Attribute
        {

        }

        """;
    }
}
