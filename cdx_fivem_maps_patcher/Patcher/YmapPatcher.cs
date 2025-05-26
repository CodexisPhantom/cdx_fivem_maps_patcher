using cdx_fivem_maps_patcher.Classes;
using cdx_fivem_maps_patcher.Pages;
using cdx_fivem_maps_patcher.Utils;
using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Patcher;

public class YmapPatcher(GameFileCache gameFileCache, string serverPath) : Patcher(gameFileCache, serverPath)
{
    protected override void Patch()
    {
        Dictionary<string, List<string>> duplicates = FindDuplicateYmapFiles(ServerPath);
        if (duplicates.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_duplicates_found"));
            return;
        }

        Console.WriteLine(Messages.Get("duplicates_found"));
        foreach (KeyValuePair<string, List<string>> entry in duplicates) PatchYmap(entry.Key, entry.Value);
    }

    private static Dictionary<string, List<string>> FindDuplicateYmapFiles(string directoryPath)
    {
        Dictionary<string, List<string>> nameToFiles = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory {directoryPath} does not exist.");

            string[] ymapFiles = Directory.GetFiles(directoryPath, "*.ymap", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "cdx_fivem_maps_patcher" +
                                        Path.DirectorySeparatorChar))
                .ToArray();
            if (ymapFiles.Length == 0) return nameToFiles;

            foreach (string filePath in ymapFiles)
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    if (!nameToFiles.TryGetValue(fileName, out List<string>? value))
                    {
                        value = [];
                        nameToFiles[fileName] = value;
                    }

                    value.Add(filePath);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Erreur d'accès au fichier {filePath} : {ex.Message}");
                }

            return nameToFiles
                .Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void PatchYmap(string name, List<string> files)
    {
        Console.WriteLine($"Patching {name}...");
        Dictionary<uint, RpfFileEntry> ymapDict = GameFileCache.YmapDict;

        uint ymapHash =
            (from entry in GameFileCache.YmapDict
                where entry.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                select entry.Key).FirstOrDefault();
        
        if (ymapHash == 0) return;

        RpfFileEntry ymapEntry = ymapDict[ymapHash];
        YmapFile? mainYmap = RpfManager.GetFile<YmapFile>(ymapEntry);

        List<YmapFile> ymapFiles = [];
        foreach (string filePath in files)
            try
            {
                YmapFile ymap = OpenYmapFile(filePath);
                ymapFiles.Add(ymap);
                //File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception)
            {
                //Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ymapFiles.Count == 0) return;

        if (mainYmap.AllEntities != null && mainYmap.AllEntities.Length != 0)
        {
            YmapEntityDef[] result = MergeYmapEntities(mainYmap, ymapFiles);
            mainYmap.AllEntities = result;
            mainYmap.RootEntities = result.Where(e => e.Parent == null || e.Parent.Ymap != mainYmap).ToArray();
            mainYmap.BuildCEntityDefs();
        }

        if (mainYmap.BoxOccluders != null && mainYmap.BoxOccluders.Length != 0)
            mainYmap.BoxOccluders = MergeWithRemovals(mainYmap, ymapFiles, y => y.BoxOccluders, new YmapBoxOccluderComparer());

        if (mainYmap.OccludeModels != null && mainYmap.OccludeModels.Length != 0)
            mainYmap.OccludeModels = MergeWithRemovals(mainYmap, ymapFiles, y => y.OccludeModels, new YmapOccludeModelComparer());

        if (mainYmap.LODLights is { LodLights: not null } && mainYmap.LODLights.LodLights.Length != 0)
        {
            mainYmap.LODLights.LodLights = MergeWithRemovals(mainYmap, ymapFiles, y => y.LODLights.LodLights, new YmapLodLightComparer());
        }
        
        if (mainYmap.DistantLODLights is { positions: not null } && mainYmap.DistantLODLights.positions.Length != 0)
        {
            mainYmap.DistantLODLights.positions = MergeWithRemovals(mainYmap, ymapFiles, y => y.DistantLODLights.positions, new YmapDistantLodLightComparer());
        }

        if (mainYmap.CarGenerators != null && mainYmap.CarGenerators.Length != 0)
            mainYmap.CarGenerators = MergeWithRemovals(mainYmap, ymapFiles, y => y.CarGenerators, new YmapCarGenComparer());

        if (mainYmap.GrassInstanceBatches != null && mainYmap.GrassInstanceBatches.Length != 0)
            mainYmap.GrassInstanceBatches = MergeWithRemovals(mainYmap, ymapFiles, y => y.GrassInstanceBatches, new YmapGrassBatchComparer());

        if (mainYmap.TimeCycleModifiers != null && mainYmap.TimeCycleModifiers.Length != 0)
            mainYmap.TimeCycleModifiers = MergeWithRemovals(mainYmap, ymapFiles, y => y.TimeCycleModifiers, new YmapTimeCycleModifierComparer());
        
        mainYmap.CalcFlags();
        mainYmap.CalcExtents();
        Backups.SaveYmap(ServerPath, mainYmap);
    }

    private static T[] MergeWithRemovals<T>(
        YmapFile mainYmap,
        List<YmapFile> ymapFiles,
        Func<YmapFile, IEnumerable<T>> selector,
        IEqualityComparer<T> comparer) where T : notnull
    {
        List<IEnumerable<T>> allPatches = ymapFiles.Select(selector).ToList();
        int patchCount = allPatches.Count;

        Dictionary<T, int> appearances = new(comparer);
        foreach (T item in allPatches.SelectMany(items => new HashSet<T>(items, comparer)))
        {
            if (appearances.TryGetValue(item, out int count))
                appearances[item] = count + 1;
            else
                appearances[item] = 1;
        }

        List<T> final = [];
        Dictionary<T, int>.KeyCollection allItems = appearances.Keys;
        HashSet<T> removedItems = new(selector(mainYmap), comparer);

        final.AddRange(from item in allItems let explicitlyRemoved = removedItems.Contains(item) && appearances[item] < patchCount where !explicitlyRemoved select item);
        return final.ToArray();
    }
    
    private static YmapEntityDef[] MergeYmapEntities(YmapFile mainYmap, List<YmapFile> ymapFiles)
    {
        Dictionary<uint, YmapEntityDef> entitiesToAdd = [];
        Dictionary<uint, YmapEntityDef> entitiesToRemove = [];
    
        Dictionary<uint, YmapEntityDef> mainEntities = mainYmap.AllEntities.ToDictionary(e => e.CEntityDef.guid, e => e);
        Dictionary<uint, YmapEntityDef> patchEntities = ymapFiles.SelectMany(patchFile => patchFile.AllEntities ?? []).GroupBy(e => e.CEntityDef.guid).ToDictionary(g => g.Key, g => g.First());
        
        foreach ((uint guid, YmapEntityDef entity) in patchEntities)
        {
            if (!mainEntities.ContainsKey(guid))
            {
                entitiesToAdd[guid] = entity;
            }
        }
    
        foreach ((uint guid, YmapEntityDef entity) in mainEntities)
        {
            if (!patchEntities.ContainsKey(guid))
            {
                entitiesToRemove[guid] = entity;
            }
        }
        
        List<YmapEntityDef> newEntities = [];
        newEntities.AddRange(from mainEntity in mainYmap.AllEntities let guid = mainEntity.CEntityDef.guid where !entitiesToRemove.ContainsKey(guid) select mainEntity);
    
        foreach ((uint _, YmapEntityDef entity) in entitiesToAdd)
        {
            entity.Index = newEntities.Count;
            newEntities.Add(entity);
        }
        
        List<YmapEntityDef> result = [];

        foreach (YmapEntityDef mainEntity in newEntities)
        {
            foreach (YmapFile patchYmap in ymapFiles)
            {
                Vector3 bestPosition = FindBestPosition(mainEntity.Position, patchYmap.AllEntities.ToList());
                mainEntity.SetPosition(bestPosition);
                UpdateEntityOrientation(mainEntity, patchYmap.AllEntities.ToList());
                UpdateEntityScale(mainEntity, patchYmap.AllEntities.ToList());
                UpdateEntityDistances(mainEntity, patchYmap.AllEntities.ToList());
                UpdateEntityParent(mainEntity, patchYmap.AllEntities.ToList());
                UpdateEntityName(mainEntity, patchYmap.AllEntities.ToList());
                result.Add(mainEntity);
            }
        }
        return result.ToArray();
    }

    private static Vector3 FindBestPosition(Vector3 originalPosition, List<YmapEntityDef> patches)
    {
        Vector3 bestPosition = originalPosition;
        float lowestZ = originalPosition.Z;
        float maxXYChange = 0f;
        
        foreach (YmapEntityDef patch in patches)
        {
            Vector3 patchPos = patch.Position;
            float xyDiff = Math.Abs(patchPos.X - originalPosition.X) + Math.Abs(patchPos.Y - originalPosition.Y);
            
            if (patchPos.Z < lowestZ - 0.1f)
            {
                bestPosition = patchPos;
                lowestZ = patchPos.Z;
                maxXYChange = xyDiff;
            }
            else if (Math.Abs(patchPos.Z - lowestZ) < 0.1f && xyDiff > maxXYChange)
            {
                bestPosition = patchPos;
                maxXYChange = xyDiff;
            }
        }
        
        bestPosition.Z = Math.Max(bestPosition.Z, -200.0f);
        return bestPosition;
    }

    private static void UpdateEntityOrientation(YmapEntityDef target, List<YmapEntityDef> patches)
    {
        Quaternion bestOrientation = target.Orientation;
        float maxChange = 0f;
        
        foreach (YmapEntityDef patch in patches)
        {
            float change = CalculateQuaternionDifference(target.Orientation, patch.Orientation);
            if (!(change > maxChange)) continue;
            maxChange = change;
            bestOrientation = patch.Orientation;
        }
        
        if (maxChange > 0.001f)
        {
            target.SetOrientation(bestOrientation);
        }
    }

    private static void UpdateEntityScale(YmapEntityDef target, List<YmapEntityDef> patches)
    {
        Vector3 bestScale = target.Scale;
        float maxChange = 0f;
        
        foreach (YmapEntityDef patch in patches)
        {
            float change = CalculateVector3Difference(target.Scale, patch.Scale);
            if (!(change > maxChange)) continue;
            maxChange = change;
            bestScale = patch.Scale;
        }
        
        if (maxChange > float.Epsilon)
        {
            target.SetScale(bestScale);
        }
    }

    private static void UpdateEntityDistances(YmapEntityDef target, List<YmapEntityDef> patches)
    {
        foreach (YmapEntityDef patch in patches)
        {
            if (Math.Abs(patch.Distance - target.Distance) > float.Epsilon)
            {
                target.Distance = patch.Distance;
            }
            
            if (Math.Abs(patch.LodDist - target.LodDist) > float.Epsilon)
            {
                target.LodDist = patch.LodDist;
            }
            
            if (Math.Abs(patch.ChildLodDist - target.ChildLodDist) > float.Epsilon)
            {
                target.ChildLodDist = patch.ChildLodDist;
            }
        }
    }

    private static void UpdateEntityParent(YmapEntityDef target, List<YmapEntityDef> patches)
    {
        foreach (YmapEntityDef patch in patches)
        {
            if (patch.Parent == null || (target.Parent != null && target.Parent.Equals(patch.Parent))) continue;
            target.Parent = patch.Parent;
            break;
        }
    }
    
    private static void UpdateEntityName(YmapEntityDef target, List<YmapEntityDef> patches)
    {
        foreach (YmapEntityDef patch in patches)
        {
            if (patch.CEntityDef.guid == target.CEntityDef.guid)
            {
                if (string.IsNullOrEmpty(patch.Name) || target.Name.Equals(patch.Name, StringComparison.OrdinalIgnoreCase)) continue;
                Console.WriteLine($"Changing name of entity {target.CEntityDef.guid} from '{target.Name}' to '{patch.Name}'");
                target.CEntityDef = target.CEntityDef with { archetypeName = patch.CEntityDef.archetypeName };
            }
            break;
        }
    }

    private static float CalculateQuaternionDifference(Quaternion a, Quaternion b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z) + Math.Abs(a.W - b.W);
    }

    private static float CalculateVector3Difference(Vector3 a, Vector3 b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }
}