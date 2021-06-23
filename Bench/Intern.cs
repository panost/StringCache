using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ecl.Collections;

namespace Bench {
    [SimpleJob( RuntimeMoniker.Net50 )]
    public class InternTest {
        private string[] _array;
        public const int Count = 100;
        private Dictionary<string, string> _map;
        private StringCache _cache;
        [ GlobalSetup]
        public void GlobalSetup() {
            List<string> list = new List<string>();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random( 1253 );
            _map = new Dictionary<string, string>();
            _cache = new StringCache();
            char[] buffer = new char[ 32 ];
            while (true ) {
                int len = random.Next( 4, 30 );

                for ( int i = 0; i < len; i++ ) {
                    buffer[ i ] = chars[ random.Next( chars.Length ) ];
                }

                string str = new string( buffer, 0, len );
                if ( string.IsInterned( str ) == null ) {
                    if ( list.Count < Count ) {
                        string.Intern( str );
                        str = new string( buffer, 0, len );
                        list.Add( str );
                        _map.Add( str, str );
                        _cache.GetOrAdd( str );
                    } else if ( list.Count < Count*2 ) {
                        list.Add( str );
                    } else {
                        break;
                    }
                }
            }

            _array = list.ToArray();
        }

        [Benchmark]
        public int Dictionary() {
            int found = 0;
            foreach ( string s in _array ) {
                if ( _map.TryGetValue( s, out string f ) ) {
                    found++;
                }
            }
            return found;
        }
        [Benchmark]
        public int Intern() {
            int found = 0;
            foreach ( string s in _array ) {
                if ( string.IsInterned( s ) != null ) {
                    found++;
                }
            }
            return found;
        }
        [Benchmark]
        public int StringCache() {
            int found = 0;
            foreach ( string s in _array ) {
                if ( _cache.GetF( s ) != null ) {
                    found++;
                }
            }

            return found;
        }
    }
}
