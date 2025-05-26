using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapEntityDefComparer : IEqualityComparer<YmapEntityDef>
{
    public bool Equals(YmapEntityDef? a, YmapEntityDef? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.CEntityDef.guid == b.CEntityDef.guid;
    }

    public int GetHashCode(YmapEntityDef? obj)
    {
        return obj is null ? 0 : obj.GetHashCode();
    }
}