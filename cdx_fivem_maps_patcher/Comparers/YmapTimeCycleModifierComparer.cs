using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapTimeCycleModifierComparer : IEqualityComparer<YmapTimeCycleModifier>
{
    public bool Equals(YmapTimeCycleModifier? a, YmapTimeCycleModifier? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.CTimeCycleModifier.name == b.CTimeCycleModifier.name;
    }

    public int GetHashCode(YmapTimeCycleModifier? obj)
    {
        return obj is null ? 0 : obj.CTimeCycleModifier.name.GetHashCode();
    }
}