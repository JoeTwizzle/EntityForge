
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

        struct Test2 : ITest<Test2>
        {

        }

        static void Main(string[] args)
        {
            Test1 a = new();
            Test2 b = new();

            PrintTest<Test1>();
            PrintTest<Test2>();

            Test<Test2>();

            PrintTest<Test1>();
            PrintTest<Test2>();
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