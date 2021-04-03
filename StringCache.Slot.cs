using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ecl.Collections {
    partial class StringCache {
        [DebuggerDisplay("Entry:{Key}, {HashCode}, {Next}")]
        internal struct Entry {
            public int HashCode;
            public int Next;
            public string Key;

            public void Set( string key, int hashCode, int next ) {
                Key = key;
                HashCode = hashCode;
                Next = next;
            }
        }
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static uint FastMod( uint value, uint divisor, ulong multiplier ) {
            return (uint)( ( ( ( ( multiplier * value ) >> 32 ) + 1 ) * divisor ) >> 32 );
        }
        public static ulong GetFastModMultiplier( uint divisor ) =>
            ulong.MaxValue / divisor + 1;
        public static int NextOdd( int i ) {
            i |= 1;
            while ( i % 3 == 0 || i % 5 == 0 || i % 7 == 0 ) {
                i += 2;
            }
            return i;
        }
        
        private class Slot {
            public int Count;
            private readonly Entry[] _entries;
            private readonly int[] _buckets;
            private readonly ulong _fastModBucketsMultiplier;

            public Slot(int capacity, int prime) {
                _fastModBucketsMultiplier = IntPtr.Size == 8 ? GetFastModMultiplier( (uint)prime ) : 0;
                _buckets = new int[ prime ];
                _entries = new Entry[ capacity ];
            }

            public Slot( HashSet<string> set ) {
                int capacity = set.Count;
                if ( capacity < 16 ) {
                    capacity = 16;
                }
                _entries = new Entry[ capacity ];
                _buckets = new int[ NextOdd(capacity) ];
                _fastModBucketsMultiplier = IntPtr.Size == 8 ? GetFastModMultiplier( (uint)_buckets.Length ) : 0;
                foreach ( string s in set ) {
                    Add( s, GetOrdinalHashCode( s ) );
                }
            }

            public string this[ int index ] => _entries[ index ].Key;

            private ref int GetBucketSlot( int hashCode ) {
                int[] buckets = _buckets;

                if ( IntPtr.Size == 8 ) {
                    return ref buckets[ FastMod( (uint)hashCode, (uint)buckets.Length, _fastModBucketsMultiplier ) ];
                }

                return ref buckets[ (uint)hashCode % (uint)buckets.Length ];
            }

            private void Add( string key, int hashCode ) {
                ref int i = ref GetBucketSlot( hashCode );
                _entries[ Count++ ].Set( key, hashCode, i );
                i = Count;
            }
            private void Add( Entry[] entries ) {
                for ( int j = 0; j < entries.Length; j++ ) {
                    ref Entry ptr = ref entries[ j ];
                    Add( ptr.Key, ptr.HashCode );
                }
            }
            public int IndexOf( ReadOnlySpan<char> key, int hashCode ) {
                int i = GetBucketSlot( hashCode );
                while ( i > 0 ) {
                    i--;
                    ref var ptr = ref _entries[ i ];

                    if ( ptr.HashCode == hashCode && key.SequenceEqual( ptr.Key ) )
                        return i;
                    i = ptr.Next;
                }

                return -1;
            }

            private Slot Resize() {
                int newCapacity = _entries.Length * 2;
                var slot = new Slot( newCapacity, NextOdd( newCapacity ) );
                slot.Add( _entries );
                return slot;
            }

            public string Insert( StringCache owner, ReadOnlySpan<char> key, int hashCode, string str ) {
                ref int lead = ref GetBucketSlot( hashCode );
                int i = lead;
                while ( i > 0 ) {
                    i--;
                    ref var ptr = ref _entries[ i ];

                    if ( ptr.HashCode == hashCode && key.SequenceEqual( ptr.Key ) )
                        return ptr.Key;
                    i = ptr.Next;
                }

                str ??= new string( key );

                if ( Count == _entries.Length ) {
                    var slot = Resize();
                    slot.Add( str, hashCode );
                    owner._slot = slot;
                } else {
                    _entries[ Count++ ].Set( str, hashCode, lead );
                    lead = Count;
                }

                return str;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator( _entries, Count );
            }
        }
    }
}
