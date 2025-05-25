using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapOccludeModelComparer : IEqualityComparer<YmapOccludeModel>
{
    public bool Equals(YmapOccludeModel? a, YmapOccludeModel? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        OccludeModel x = a.OccludeModel; // underlying OccludeModel struct
        OccludeModel y = b.OccludeModel;
        return x.flags.Equals(y.flags)
               && x.bmin == y.bmin
               && x.bmax == y.bmax
               && x.dataSize == y.dataSize
               && x.numVertsInBytes == y.numVertsInBytes
               && x.numTris == y.numTris;
    }

    public int GetHashCode(YmapOccludeModel? obj)
    {
        if (obj is null) return 0;
        OccludeModel x = obj.OccludeModel;
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + x.flags.GetHashCode();
            hash = hash * 23 + x.bmin.GetHashCode();
            hash = hash * 23 + x.bmax.GetHashCode();
            hash = hash * 23 + x.dataSize.GetHashCode();
            hash = hash * 23 + x.numVertsInBytes.GetHashCode();
            hash = hash * 23 + x.numTris.GetHashCode();
            return hash;
        }
    }
}