using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapLodLightComparer : IEqualityComparer<YmapLODLight>
{
    public bool Equals(YmapLODLight? a, YmapLODLight? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        
        Vector3 aPos = a.Position;
        Vector3 bPos = b.Position;
        
        Vector3 aSize = a.Scale;
        Vector3 bSize = b.Scale;
        
        Color aColor = a.Colour;
        Color bColor = b.Colour;
        
        Quaternion aRot = a.Orientation;
        Quaternion bRot = b.Orientation;

        return Math.Abs(aPos.X - bPos.X) < 0.01f
               && Math.Abs(aPos.Y - bPos.Y) < 0.01f
               && Math.Abs(aPos.Z - bPos.Z) < 0.01f
               && Math.Abs(aSize.X - bSize.X) < 0.1f
               && Math.Abs(aSize.Y - bSize.Y) < 0.1f
               && Math.Abs(aSize.Z - bSize.Z) < 0.1f
               && Math.Abs(aRot.X - bRot.X) < 0.01f
               && Math.Abs(aRot.Y - bRot.Y) < 0.01f
               && Math.Abs(aRot.Z - bRot.Z) < 0.01f
               && Math.Abs(aRot.W - bRot.W) < 0.01f
               && aColor.R == bColor.R
               && aColor.G == bColor.G
               && aColor.B == bColor.B
               && aColor.A == bColor.A;
    }

    public int GetHashCode(YmapLODLight? obj)
    {
        if (obj is null) return 0;
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + obj.Position.X.GetHashCode();
            hash = hash * 23 + obj.Position.Y.GetHashCode();
            hash = hash * 23 + obj.Position.Z.GetHashCode();
            hash = hash * 23 + obj.Scale.X.GetHashCode();
            hash = hash * 23 + obj.Scale.Y.GetHashCode();
            hash = hash * 23 + obj.Scale.Z.GetHashCode();
            hash = hash * 23 + obj.Colour.R.GetHashCode();
            hash = hash * 23 + obj.Colour.G.GetHashCode();
            hash = hash * 23 + obj.Colour.B.GetHashCode();
            hash = hash * 23 + obj.Colour.A.GetHashCode();
            hash = hash * 23 + obj.Orientation.X.GetHashCode();
            hash = hash * 23 + obj.Orientation.Y.GetHashCode();
            hash = hash * 23 + obj.Orientation.Z.GetHashCode();
            hash = hash * 23 + obj.Orientation.W.GetHashCode();
            return hash;
        }
    }
}