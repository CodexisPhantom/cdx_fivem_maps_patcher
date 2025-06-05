using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Utils;

public class YmapBoxOccluderComparer : IEqualityComparer<YmapBoxOccluder>
{
    private const float POSITION_TOLERANCE = 0.05f;
    private const float SIZE_TOLERANCE = 0.02f;
    private const float ANGLE_TOLERANCE = 0.1f;
    
    public bool Equals(YmapBoxOccluder? a, YmapBoxOccluder? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        
        Vector3 aPos = a.Position;
        Vector3 bPos = b.Position;
        if (Math.Abs(aPos.X - bPos.X) >= POSITION_TOLERANCE ||
            Math.Abs(aPos.Y - bPos.Y) >= POSITION_TOLERANCE ||
            Math.Abs(aPos.Z - bPos.Z) >= POSITION_TOLERANCE)
        {
            return false;
        }
        
        Vector3 aSize = a.Size;
        Vector3 bSize = b.Size;
        if (Math.Abs(aSize.X - bSize.X) >= SIZE_TOLERANCE ||
            Math.Abs(aSize.Y - bSize.Y) >= SIZE_TOLERANCE ||
            Math.Abs(aSize.Z - bSize.Z) >= SIZE_TOLERANCE)
        {
            return false;
        }
        
        BoxOccluder aBox = a.Box;
        BoxOccluder bBox = b.Box;
        float aSinZ = aBox.iSinZ / 32767.0f;
        float bSinZ = bBox.iSinZ / 32767.0f;
        float aCosZ = aBox.iCosZ / 32767.0f;
        float bCosZ = bBox.iCosZ / 32767.0f;
        
        return Math.Abs(aSinZ - bSinZ) < ANGLE_TOLERANCE && 
               Math.Abs(aCosZ - bCosZ) < ANGLE_TOLERANCE;
    }

    public int GetHashCode(YmapBoxOccluder? obj)
    {
        if (obj is null) return 0;
        
        Vector3 pos = obj.Position;
        Vector3 size = obj.Size;
        
        int quantizedPosX = (int)Math.Round(pos.X / POSITION_TOLERANCE);
        int quantizedPosY = (int)Math.Round(pos.Y / POSITION_TOLERANCE);
        int quantizedPosZ = (int)Math.Round(pos.Z / POSITION_TOLERANCE);
        
        int quantizedSizeX = (int)Math.Round(size.X / SIZE_TOLERANCE);
        int quantizedSizeY = (int)Math.Round(size.Y / SIZE_TOLERANCE);
        int quantizedSizeZ = (int)Math.Round(size.Z / SIZE_TOLERANCE);
        
        BoxOccluder box = obj.Box;
        int quantizedSinZ = (int)Math.Round((box.iSinZ / 32767.0f) / ANGLE_TOLERANCE);
        int quantizedCosZ = (int)Math.Round((box.iCosZ / 32767.0f) / ANGLE_TOLERANCE);
        
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + quantizedPosX;
            hash = hash * 23 + quantizedPosY;
            hash = hash * 23 + quantizedPosZ;
            hash = hash * 23 + quantizedSizeX;
            hash = hash * 23 + quantizedSizeY;
            hash = hash * 23 + quantizedSizeZ;
            hash = hash * 23 + quantizedSinZ;
            hash = hash * 23 + quantizedCosZ;
            return hash;
        }
    }
}