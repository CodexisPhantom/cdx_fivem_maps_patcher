using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapBoxOccluderComparer : IEqualityComparer<YmapBoxOccluder>
{
    public bool Equals(YmapBoxOccluder? a, YmapBoxOccluder? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        Vector3 aPos = a.Position;
        Vector3 bPos = b.Position;
        Vector3 aSize = a.Size;
        Vector3 bSize = b.Size;
        return Math.Abs(aPos.X - bPos.X) < 0.1f
               && Math.Abs(aPos.Y - bPos.Y) < 0.1f
               && Math.Abs(aPos.Z - bPos.Z) < 0.1f
               && Math.Abs(aSize.X - bSize.X) < 0.1f
               && Math.Abs(aSize.Y - bSize.Y) < 0.1f
               && Math.Abs(aSize.Z - bSize.Z) < 0.1f;
    }

    public int GetHashCode(YmapBoxOccluder? obj)
    {
        if (obj is null) return 0;
        BoxOccluder x = obj.Box;
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + x.iCenterX.GetHashCode();
            hash = hash * 23 + x.iCenterY.GetHashCode();
            hash = hash * 23 + x.iCenterZ.GetHashCode();
            hash = hash * 23 + x.iLength.GetHashCode();
            hash = hash * 23 + x.iWidth.GetHashCode();
            hash = hash * 23 + x.iHeight.GetHashCode();
            hash = hash * 23 + x.iSinZ.GetHashCode();
            hash = hash * 23 + x.iCosZ.GetHashCode();
            return hash;
        }
    }
}