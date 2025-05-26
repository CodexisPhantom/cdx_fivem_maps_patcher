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
            Console.WriteLine($"Error searching for duplicates: {ex.Message}");
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void PatchYmap(string name, List<string> files)
    {
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
                File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception)
            {
                //Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ymapFiles.Count == 0) return;

        Console.WriteLine($"Patching {name}...");

        if (mainYmap.AllEntities != null && mainYmap.AllEntities.Length != 0)
        {
            YmapEntityDef[] result = MergeYmapEntities(mainYmap, ymapFiles);
            mainYmap.AllEntities = result;
            mainYmap.RootEntities = result.Where(e => e.Parent == null || e.Parent.Ymap != mainYmap).ToArray();
            mainYmap.BuildCEntityDefs();
        }

        if (mainYmap.BoxOccluders != null && mainYmap.BoxOccluders.Length != 0)
            mainYmap.BoxOccluders =
                MergeWithRemovals(mainYmap, ymapFiles, y => y.BoxOccluders, new YmapBoxOccluderComparer());

        if (mainYmap.OccludeModels != null && mainYmap.OccludeModels.Length != 0)
            mainYmap.OccludeModels =
                MergeWithRemovals(mainYmap, ymapFiles, y => y.OccludeModels, new YmapOccludeModelComparer());

        if (mainYmap.LODLights is { LodLights: not null } && mainYmap.LODLights.LodLights.Length != 0)
            mainYmap.LODLights.LodLights = MergeWithRemovals(mainYmap, ymapFiles, y => y.LODLights.LodLights,
                new YmapLodLightComparer());

        if (mainYmap.DistantLODLights is { positions: not null } && mainYmap.DistantLODLights.positions.Length != 0)
            mainYmap.DistantLODLights.positions = MergeWithRemovals(mainYmap, ymapFiles,
                y => y.DistantLODLights.positions, new YmapDistantLodLightComparer());

        if (mainYmap.CarGenerators != null && mainYmap.CarGenerators.Length != 0)
            mainYmap.CarGenerators =
                MergeWithRemovals(mainYmap, ymapFiles, y => y.CarGenerators, new YmapCarGenComparer());

        if (mainYmap.GrassInstanceBatches != null && mainYmap.GrassInstanceBatches.Length != 0)
            mainYmap.GrassInstanceBatches = MergeWithRemovals(mainYmap, ymapFiles, y => y.GrassInstanceBatches,
                new YmapGrassBatchComparer());

        if (mainYmap.TimeCycleModifiers != null && mainYmap.TimeCycleModifiers.Length != 0)
            mainYmap.TimeCycleModifiers = MergeWithRemovals(mainYmap, ymapFiles, y => y.TimeCycleModifiers,
                new YmapTimeCycleModifierComparer());

        mainYmap.CalcFlags();
        mainYmap.CalcExtents();
        Backups.SaveYmap(ServerPath, mainYmap);
    }

    private static T[] MergeWithRemovals<T>(
        YmapFile mainYmap,
        List<YmapFile> ymapFiles,
        Func<YmapFile, IEnumerable<T>> selector,
        IEqualityComparer<T?> comparer) where T : notnull
    {
        List<IEnumerable<T>> allPatches = ymapFiles.Select(selector).ToList();
        int patchCount = allPatches.Count;

        allPatches = allPatches.Where(patchContent => patchContent != null && patchContent.Any()).ToList();

        Dictionary<T, int> appearances = new(comparer);
        foreach (T? item in allPatches.SelectMany(patchContent => new HashSet<T>(patchContent, comparer)))
            if (appearances.TryGetValue(item, out int count))
                appearances[item] = count + 1;
            else
                appearances[item] = 1;

        List<T> final = [];
        Dictionary<T, int>.KeyCollection allItems = appearances.Keys;

        IEnumerable<T>? mainYmapItemsEnumerable = selector(mainYmap);
        HashSet<T> removedItems = new(mainYmapItemsEnumerable, comparer);
        final.AddRange(from item in allItems
            let explicitlyRemoved = removedItems.Contains(item) && appearances[item] < patchCount
            where !explicitlyRemoved
            select item);
        return final.ToArray();
    }

    private static YmapEntityDef[] MergeYmapEntities(YmapFile mainYmap, List<YmapFile> ymapFiles)
    {
        // Initialize collections for tracking entities
        Dictionary<uint, YmapEntityDef> entitiesToAdd = [];
        Dictionary<uint, YmapEntityDef> entitiesToRemove = [];

        // Create dictionaries for fast GUID-based lookups
        Dictionary<uint, YmapEntityDef>
            mainEntities = mainYmap.AllEntities.ToDictionary(e => e.CEntityDef.guid, e => e);
        Dictionary<uint, YmapEntityDef> patchEntities = ymapFiles
            .SelectMany(patchFile => patchFile.AllEntities ?? [])
            .GroupBy(e => e.CEntityDef.guid)
            .ToDictionary(g => g.Key, g => g.First());

        // Find entities to add (exist in patches but not in main)
        foreach ((uint guid, YmapEntityDef entity) in patchEntities)
            if (!mainEntities.ContainsKey(guid))
                entitiesToAdd[guid] = entity;

        // Find entities to remove - Aggressive removal strategy
        // Remove entity if it's missing from ANY patch file
        foreach ((uint guid, YmapEntityDef entity) in mainEntities)
        {
            bool shouldRemove = false;

            // Check each patch file - if entity is missing from any, mark for removal
            foreach (YmapFile patchFile in ymapFiles)
            {
                if (patchFile.AllEntities == null)
                {
                    // If patch file has no entities, consider this as "entity not present"
                    shouldRemove = true;
                    break;
                }

                bool foundInThisPatch = patchFile.AllEntities.Any(e => e?.CEntityDef.guid == guid);
                if (foundInThisPatch) continue;
                shouldRemove = true;
                break; // Missing from this patch file, so remove it
            }

            if (shouldRemove) entitiesToRemove[guid] = entity;
        }

        // Build the base entity list (main entities minus removed ones)
        List<YmapEntityDef> result = [];
        result.AddRange(from mainEntity in mainYmap.AllEntities
            let guid = mainEntity.CEntityDef.guid
            where !entitiesToRemove.ContainsKey(guid)
            select mainEntity);

        // Add non-removed main entities

        // Add new entities from patches
        foreach ((uint _, YmapEntityDef entity) in entitiesToAdd)
        {
            entity.Index = result.Count;
            result.Add(entity);
        }

        // Now apply patches to all entities in the result list
        HashSet<uint> processedArchetypeChanges = [];

        for (int i = 0; i < result.Count; i++)
        {
            YmapEntityDef entity = result[i];
            uint entityGuid = entity.CEntityDef.guid;

            // Process each patch file for this entity
            foreach (YmapEntityDef? patchEntity in ymapFiles
                         .Select(patchYmap =>
                             patchYmap.AllEntities?.FirstOrDefault(e => e?.CEntityDef.guid == entityGuid))
                         .OfType<YmapEntityDef>())
            {
                // Handle archetype name changes (highest priority - replace entire entity)
                if (patchEntity.CEntityDef.archetypeName != entity.CEntityDef.archetypeName &&
                    !processedArchetypeChanges.Contains(entityGuid))
                {
                    result[i] = patchEntity;
                    processedArchetypeChanges.Add(entityGuid);
                    entity = patchEntity; // Update reference for further processing
                    continue;
                }

                // Apply property patches with consistent logic
                entity = ApplyEntityPatches(entity, patchEntity);
            }

            // Update the entity in the result list
            result[i] = entity;
        }

        return result.ToArray();
    }

    private static YmapEntityDef ApplyEntityPatches(YmapEntityDef mainEntity, YmapEntityDef patchEntity)
    {
        const float tolerance = 0.01f;

        // Apply position patches
        Vector3 mainPosition = mainEntity.Position;
        Vector3 patchPosition = patchEntity.Position;
        bool positionChanged = false;

        // Consistent logic: Update if values are significantly different
        if (Math.Abs(mainPosition.X - patchPosition.X) >= tolerance)
        {
            mainPosition.X = patchPosition.X;
            positionChanged = true;
        }

        if (Math.Abs(mainPosition.Y - patchPosition.Y) >= tolerance)
        {
            mainPosition.Y = patchPosition.Y;
            positionChanged = true;
        }

        // Special Z-coordinate handling: Update if patch is lower (with safety clamp)
        if (patchPosition.Z < mainPosition.Z)
        {
            mainPosition.Z = Math.Max(patchPosition.Z, -200.0f);
            positionChanged = true;
        }

        if (positionChanged) mainEntity.SetPosition(mainPosition);

        // Apply orientation patches
        Quaternion mainOrientation = mainEntity.Orientation;
        Quaternion patchOrientation = patchEntity.Orientation;
        bool orientationChanged = false;

        if (Math.Abs(mainOrientation.X - patchOrientation.X) >= tolerance)
        {
            mainOrientation.X = patchOrientation.X;
            orientationChanged = true;
        }

        if (Math.Abs(mainOrientation.Y - patchOrientation.Y) >= tolerance)
        {
            mainOrientation.Y = patchOrientation.Y;
            orientationChanged = true;
        }

        if (Math.Abs(mainOrientation.Z - patchOrientation.Z) >= tolerance)
        {
            mainOrientation.Z = patchOrientation.Z;
            orientationChanged = true;
        }

        if (Math.Abs(mainOrientation.W - patchOrientation.W) >= tolerance)
        {
            mainOrientation.W = patchOrientation.W;
            orientationChanged = true;
        }

        if (orientationChanged) mainEntity.SetOrientation(mainOrientation);

        // Apply scale patches
        Vector3 mainScale = mainEntity.Scale;
        Vector3 patchScale = patchEntity.Scale;
        bool scaleChanged = false;

        if (Math.Abs(mainScale.X - patchScale.X) >= tolerance)
        {
            mainScale.X = patchScale.X;
            scaleChanged = true;
        }

        if (Math.Abs(mainScale.Y - patchScale.Y) >= tolerance)
        {
            mainScale.Y = patchScale.Y;
            scaleChanged = true;
        }

        if (Math.Abs(mainScale.Z - patchScale.Z) >= tolerance)
        {
            mainScale.Z = patchScale.Z;
            scaleChanged = true;
        }

        if (scaleChanged) mainEntity.SetScale(mainScale);

        // Apply other property patches
        if (mainEntity.Parent != patchEntity.Parent) mainEntity.Parent = patchEntity.Parent;

        if (Math.Abs(mainEntity.Distance - patchEntity.Distance) >= tolerance)
            mainEntity.Distance = patchEntity.Distance;

        if (Math.Abs(mainEntity.LodDist - patchEntity.LodDist) >= tolerance) mainEntity.LodDist = patchEntity.LodDist;

        if (Math.Abs(mainEntity.ChildLodDist - patchEntity.ChildLodDist) >= tolerance)
            mainEntity.ChildLodDist = patchEntity.ChildLodDist;

        return mainEntity;
    }
}