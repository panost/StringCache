using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;

namespace Bench {
    class Program {
        static void Main( string[] args ) {
#if DEBUG
            Test();
#else
            BenchmarkRunner.Run( typeof( InternTest ) );
#endif
        }

        static void Test() {
            var t = new InternTest();
            t.GlobalSetup();
            if ( t.Dictionary() != InternTest.Count ) {
                Debug.WriteLine( "Dictionary" );
            }
            if ( t.Intern() != InternTest.Count ) {
                Debug.WriteLine( "Intern" );
            }
            if ( t.StringCache() != InternTest.Count ) {
                Debug.WriteLine( "StringCache" );
            }
        }
    }
}
