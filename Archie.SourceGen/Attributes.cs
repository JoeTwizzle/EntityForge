using System;
using System.Collections.Generic;
using System.Text;



namespace Archie.SourceGen
{
    static class Attributes
    {

        public static readonly string InjectTypesAttribute = $$""" 
        //Auto generated
        using System;
        namespace Archie;

        [System.AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
        public sealed class InjectTypesAttribute : global::System.Attribute
        {

        }

        """;
    }
}
