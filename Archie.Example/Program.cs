
using System.Runtime.CompilerServices;

namespace Archie.Example
{
    internal class Program
    {
        interface ITest<T> where T : struct, ITest<T>
        {
            public static virtual bool Test { get; set; }
        }

        struct Test1 : ITest<Test1>
        {

        }

        struct Test2 : IComponent<Test2>
        {

        }

        static void Main(string[] args)
        {

            Console.WriteLine(RuntimeHelpers.IsReferenceOrContainsReferences<Test2>());

            Console.ReadLine();
        }


        static void Test<T>() where T : struct, ITest<T>
        {
            T.Test = true;
        }

        static void PrintTest<T>() where T : struct, ITest<T>
        {
            Console.WriteLine(T.Test);
        }
    }
}