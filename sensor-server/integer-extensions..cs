using System;
using System.Linq;

namespace sensorserver
{
    /// <summary>
    /// Integer extensions utility class.
    /// </summary>
    public static class IntegerExtensions
    {
        public static Int64 AlignUp(this Int64 self, Int64 alignTo) => (self + alignTo - 1) & ~(alignTo - 1);
    }
}
