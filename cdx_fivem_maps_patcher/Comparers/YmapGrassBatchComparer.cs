using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapGrassBatchComparer : IEqualityComparer<YmapGrassInstanceBatch>
{
    public bool Equals(YmapGrassInstanceBatch? a, YmapGrassInstanceBatch? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Batch.BatchAABB.min == b.Batch.BatchAABB.min
               && a.Batch.BatchAABB.max == b.Batch.BatchAABB.max;
    }

    public int GetHashCode(YmapGrassInstanceBatch? obj)
    {
        if (obj is null) return 0;
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + obj.Batch.BatchAABB.min.GetHashCode();
            hash = hash * 23 + obj.Batch.BatchAABB.max.GetHashCode();
            return hash;
        }
    }
}