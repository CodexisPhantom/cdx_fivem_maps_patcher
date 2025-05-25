using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapCarGenComparer : IEqualityComparer<YmapCarGen>
{
    public bool Equals(YmapCarGen? a, YmapCarGen? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return Math.Abs(a._CCarGen.perpendicularLength - b._CCarGen.perpendicularLength) < float.Epsilon
               && a.Position == b.Position;
    }

    public int GetHashCode(YmapCarGen? obj)
    {
        if (obj is null) return 0;
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + obj._CCarGen.perpendicularLength.GetHashCode();
            hash = hash * 23 + obj.Position.GetHashCode();
            return hash;
        }
    }
}