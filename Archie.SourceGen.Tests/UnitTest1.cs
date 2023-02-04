//using Archie;
//using CommunityToolkit.HighPerformance;
//using System;
//using System.Buffers;
//using System.Diagnostics;
//using System.Diagnostics.CodeAnalysis;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Security.AccessControl;
//using System.Threading.Tasks;
//using static Archie.EntityFilter;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace Archie.SourceGen.Tests
//{


//    public partial class Tests
//    {
//        [SetUp]
//        public void Setup()
//        {
//        }

//        [Test]
//        public void InjectTypesAttributeExists()
//        {
//            Assert.NotNull(Type.GetType("Archie.InjectTypesAttribute"));

            
//        }



//        ref partial struct TestGroup
//        {
//            public readonly ref readonly TestData da;
//            public readonly ref TestData waswau;
//            public readonly ref TestData wosaw;

//            public unsafe void QueryAll(delegate*<in TestData, ref TestData, ref TestData> data)
//            {

//            }
//        }
//        struct TestData : IComponent<TestData>
//        {
//            int a;
//        }


//    }
//}