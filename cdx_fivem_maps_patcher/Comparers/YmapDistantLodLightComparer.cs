using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapDistantLodLightComparer : IEqualityComparer<MetaVECTOR3>
{
    public bool Equals(MetaVECTOR3 a, MetaVECTOR3 b)
    {
        Vector3 aPos = a.ToVector3();
        Vector3 bPos = b.ToVector3();

        return Math.Abs(aPos.X - bPos.X) < 0.01f
               && Math.Abs(aPos.Y - bPos.Y) < 0.01f
               && Math.Abs(aPos.Z - bPos.Z) < 0.01f;
    }

    public int GetHashCode(MetaVECTOR3 obj)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + obj.x.GetHashCode();
            hash = hash * 23 + obj.y.GetHashCode();
            hash = hash * 23 + obj.z.GetHashCode();
            return hash;
        }
    }
}