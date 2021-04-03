using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace ecl.Collections {
    public partial class StringCache : IEnumerable<string> {
        public readonly StringCache Parent;
        private Slot _slot;

        public StringCache( StringCache parent ) {
            Parent = parent;
            _slot = new Slot( 16, 13 );
        }

        public StringCache()
            : this( (StringCache)null ) {
        }

        public int Count => _slot.Count;

        public StringCache( IEnumerable<string> str, StringCache parent ) {
            HashSet<string> set;
            if ( parent != null ) {
                Parent = parent;
                set = new HashSet<string>();
                foreach ( var s in str ) {
                    if ( parent.Get( s ) == null ) {
                        set.Add( s );
                    }
                }
            } else {
                set = new HashSet<string>( str );
            }

            _slot = set.Count > 0 ? new Slot( set ) : new Slot( 16, 13 );
        }

        public StringCache( params string[] args )
            : this( args, (StringCache)null ) {
        }

        internal static readonly uint CaseSensitiveSeed = GetRandomUint( 0x15051505 );

        private static uint GetRandomUint( int someSeed ) {
#if DEBUG
            return (uint)someSeed;
#else
            var rnd = new Random( Environment.TickCount ^ someSeed );
            return (uint)HashCode.Combine( rnd.Next(), rnd.Next() );
#endif
        }

        public static int GetOrdinalHashCode( ReadOnlySpan<char> span ) {
            uint hash = CaseSensitiveSeed;
            for ( int i = 0; i < span.Length; i++ ) {
                hash = ( BitOperations.RotateLeft( hash, 5 ) + hash ) ^ span[ i ];
            }

            return (int)hash;
        }


        public string Get( ReadOnlySpan<byte> key ) {
            if ( key.Length < 256 ) {
                Span<char> name = stackalloc char[ 512 ];
                if ( Encoding.UTF8.GetChars( key, name ) == key.Length ) {
                    return Get( name );
                }
            }

            return null;
        }

        private string Get( ReadOnlySpan<char> key, int hashCode ) {
            var slot = Volatile.Read( ref _slot );
            int idx = slot.IndexOf( key, hashCode );
            return idx >= 0 ? slot[ idx ] : Parent?.Get( key, hashCode );
        }

        private string GetOrAdd( ReadOnlySpan<char> key, int hashCode ) {
            var slot = Volatile.Read( ref _slot );
            int idx = slot.IndexOf( key, hashCode );
            return idx >= 0 ? slot[ idx ] : Parent?.Get( key, hashCode );
        }

        public string Get( ReadOnlySpan<char> key ) {
            return Get( key, GetOrdinalHashCode( key ) );
        }

        public string GetOrAdd( ReadOnlySpan<char> key ) {
            var hashCode = GetOrdinalHashCode( key );
            return Get( key, hashCode ) ?? Add( key, hashCode );
        }

        public string GetOrAdd( ReadOnlySpan<byte> key ) {
            if ( key.Length < 256 ) {
                Span<char> name = stackalloc char[ 512 ];
                if ( Encoding.UTF8.GetChars( key, name ) == key.Length ) {
                    return GetOrAdd( name );
                }
            }

            return null;
        }

        public string GetOrAdd( string key ) {
            var hashCode = GetOrdinalHashCode( key );
            return Get( key, hashCode ) ?? Add( key, hashCode );
        }

        private readonly object _addition = new();

        private string Add( ReadOnlySpan<char> key, int hashCode ) {
            lock ( _addition ) {
                return _slot.Insert( this, key, hashCode );
            }
        }

        private string Add( string key, int hashCode ) {
            lock ( _addition ) {
                return _slot.Insert( this, key, hashCode );
            }
        }

        public struct Enumerator : IEnumerator<string> {
            private readonly Entry[] _entries;
            private readonly int _count;
            private int _index;
            internal Enumerator( Entry[] entries, int count ) {
                _entries = entries;
                _count = count;
                _index = -1;
            }

            public bool MoveNext() {
                int idx = _index + 1;
                if ( idx < _count ) {
                    _index = idx;
                    return true;
                }

                return false;
            }

            public void Reset() {
                _index = -1;
            }

            public string Current => (uint)_index < (uint)_entries.Length? _entries[ _index ].Key:null;

            object IEnumerator.Current => Current;


            void IDisposable.Dispose() {
            }

        }
        public Enumerator GetEnumerator() {
            return _slot.GetEnumerator();
        }

        #region Implementation of IEnumerable

        IEnumerator<string> IEnumerable<string>.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion
    }
}