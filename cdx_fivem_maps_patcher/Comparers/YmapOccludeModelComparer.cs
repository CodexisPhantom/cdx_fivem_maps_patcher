using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapOccludeModelComparer : IEqualityComparer<YmapOccludeModel>
{
    private const float BOUNDING_BOX_TOLERANCE = 0.01f;
    private const uint DATA_SIZE_TOLERANCE = 4;
    private const ushort VERTEX_COUNT_TOLERANCE = 2;
    private const ushort TRIANGLE_COUNT_TOLERANCE = 1;
    
    public bool Equals(YmapOccludeModel? a, YmapOccludeModel? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        
        OccludeModel x = a.OccludeModel;
        OccludeModel y = b.OccludeModel;
        
        if (!x.flags.Equals(y.flags))
            return false;
        
        if (!IsVector3Equal(x.bmin, y.bmin, BOUNDING_BOX_TOLERANCE) || !IsVector3Equal(x.bmax, y.bmax, BOUNDING_BOX_TOLERANCE))
        {
            return false;
        }
        
        if (Math.Abs((long)x.dataSize - y.dataSize) > DATA_SIZE_TOLERANCE)
            return false;
        if (Math.Abs(x.numVertsInBytes - y.numVertsInBytes) > VERTEX_COUNT_TOLERANCE)
            return false;
            
        ushort xTriCount = (ushort)(x.numTris & 0x7FFF);
        ushort yTriCount = (ushort)(y.numTris & 0x7FFF);
        return Math.Abs(xTriCount - yTriCount) <= TRIANGLE_COUNT_TOLERANCE;
    }

    public int GetHashCode(YmapOccludeModel? obj)
    {
        if (obj is null) return 0;
        
        OccludeModel x = obj.OccludeModel;
        
        int quantizedBMinX = (int)Math.Round(x.bmin.X / BOUNDING_BOX_TOLERANCE);
        int quantizedBMinY = (int)Math.Round(x.bmin.Y / BOUNDING_BOX_TOLERANCE);
        int quantizedBMinZ = (int)Math.Round(x.bmin.Z / BOUNDING_BOX_TOLERANCE);
        
        int quantizedBMaxX = (int)Math.Round(x.bmax.X / BOUNDING_BOX_TOLERANCE);
        int quantizedBMaxY = (int)Math.Round(x.bmax.Y / BOUNDING_BOX_TOLERANCE);
        int quantizedBMaxZ = (int)Math.Round(x.bmax.Z / BOUNDING_BOX_TOLERANCE);
        
        uint quantizedDataSize = x.dataSize / DATA_SIZE_TOLERANCE * DATA_SIZE_TOLERANCE;
        ushort quantizedVertBytes = (ushort)(x.numVertsInBytes / VERTEX_COUNT_TOLERANCE * VERTEX_COUNT_TOLERANCE);
        ushort quantizedTriCount = (ushort)((x.numTris & 0x7FFF) / TRIANGLE_COUNT_TOLERANCE * TRIANGLE_COUNT_TOLERANCE);
        
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + x.flags.GetHashCode();
            hash = hash * 23 + quantizedBMinX;
            hash = hash * 23 + quantizedBMinY;
            hash = hash * 23 + quantizedBMinZ;
            hash = hash * 23 + quantizedBMaxX;
            hash = hash * 23 + quantizedBMaxY;
            hash = hash * 23 + quantizedBMaxZ;
            hash = hash * 23 + quantizedDataSize.GetHashCode();
            hash = hash * 23 + quantizedVertBytes.GetHashCode();
            hash = hash * 23 + quantizedTriCount.GetHashCode();
            return hash;
        }
    }
    
    private static bool IsVector3Equal(Vector3 a, Vector3 b, float tolerance)
    {
        return Math.Abs(a.X - b.X) < tolerance && Math.Abs(a.Y - b.Y) < tolerance && Math.Abs(a.Z - b.Z) < tolerance;
    }
}