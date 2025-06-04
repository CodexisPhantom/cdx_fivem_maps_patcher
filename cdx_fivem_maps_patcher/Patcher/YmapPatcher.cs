using cdx_fivem_maps_patcher.Classes;
using cdx_fivem_maps_patcher.Pages;
using cdx_fivem_maps_patcher.Utils;
using CodeWalker.GameFiles;
using SharpDX;

namespace cdx_fivem_maps_patcher.Patcher;

public class YmapPatcher(GameFileCache gameFileCache, string serverPath) : Patcher(gameFileCache, serverPath)
{
    private List<float> MergePositionX = [];
    private List<float> MergePositionY = [];
    private List<float> MergePositionZ = [];
    
    private List<float> MergeOrientationX = [];
    private List<float> MergeOrientationY = [];
    private List<float> MergeOrientationZ = [];
    private List<float> MergeOrientationW = [];
    
    private List<float> MergeScaleX = [];
    private List<float> MergeScaleY = [];
    private List<float> MergeScaleZ = [];
    
    private List<float> MergeDistance = [];
    private List<float> MergeLodDist = [];

    private Archetype? MergeArchetype = new();
    
    protected override void Patch()
    {
        Dictionary<string, List<string>> duplicates = FindDuplicateYmapFiles(ServerPath);
        if (duplicates.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_duplicates_found"));
            return;
        }

        Console.WriteLine(Messages.Get("duplicates_found"));
        
        Dictionary<string, List<string>> selectedYmaps = PromptUserForYmapSelection(duplicates);
        
        if (selectedYmaps.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_ymaps_selected_for_patching"));
            return;
        }

        foreach (KeyValuePair<string, List<string>> entry in selectedYmaps) 
            PatchYmap(entry.Key, entry.Value);
    }

    private Dictionary<string, List<string>> PromptUserForYmapSelection(Dictionary<string, List<string>> duplicates)
    {
        Dictionary<string, List<string>> selectedYmaps = new(StringComparer.OrdinalIgnoreCase);
        
        Console.WriteLine(Messages.Get("found_duplicate_ymap_files_header"));
        Console.WriteLine(Messages.Get("select_ymaps_to_patch_prompt"));

        List<KeyValuePair<string, List<string>>> duplicatesList = duplicates.ToList();
        
        for (int i = 0; i < duplicatesList.Count; i++)
        {
            KeyValuePair<string, List<string>> kvp = duplicatesList[i];
            Console.WriteLine(Messages.Get("ymap_selection_item_format", i + 1, kvp.Key, kvp.Value.Count));
            
            foreach (string filePath in kvp.Value)
            {
                Console.WriteLine(Messages.Get("ymap_selection_file_path_prefix") + filePath);
            }
            Console.WriteLine();
        }

        Console.WriteLine(Messages.Get("ymap_selection_options_header"));
        Console.WriteLine(Messages.Get("ymap_selection_option_numbers"));
        Console.WriteLine(Messages.Get("ymap_selection_option_all"));
        Console.WriteLine(Messages.Get("ymap_selection_option_none"));
        Console.Write(Messages.Get("ymap_selection_input_prompt"));

        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) || input.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return selectedYmaps;
        }

        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return duplicates;
        }

        string[] selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> validIndices = [];

        foreach (string selection in selections)
        {
            if (int.TryParse(selection.Trim(), out int index) && 
                index >= 1 && index <= duplicatesList.Count)
            {
                validIndices.Add(index - 1);
            }
            else
            {
                Console.WriteLine(Messages.Get("ymap_selection_invalid_warning", selection));
            }
        }

        foreach (KeyValuePair<string, List<string>> kvp in validIndices.Select(index => duplicatesList[index]))
        {
            selectedYmaps[kvp.Key] = kvp.Value;
        }

        if (selectedYmaps.Count <= 0) return selectedYmaps;
        
        Console.WriteLine(Messages.Get("ymap_selection_selected_count_message", selectedYmaps.Count));
        foreach (string ymapName in selectedYmaps.Keys)
        {
            Console.WriteLine(Messages.Get("ymap_selection_selected_item_prefix") + ymapName);
        }

        return selectedYmaps;
    }

    private Dictionary<string, List<string>> FindDuplicateYmapFiles(string directoryPath)
    {
        Dictionary<string, List<string>> nameToFiles = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(Messages.Get("directory_not_found_error", directoryPath));

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
                    Console.WriteLine(Messages.Get("file_access_error", filePath, ex.Message));
                }

            return nameToFiles
                .Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Messages.Get("duplicate_search_error", ex.Message));
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
        mainYmap.InitYmapEntityArchetypes(GameFileCache);
        mainYmap.EnsureChildYmaps(GameFileCache);

        List<YmapFile> ymapFiles = [];
        foreach (string filePath in files)
            try
            {
                YmapFile ymap = OpenYmapFile(filePath);
                ymap.InitYmapEntityArchetypes(GameFileCache);
                ymap.EnsureChildYmaps(GameFileCache);
                ymapFiles.Add(ymap);
                File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception)
            {
                //Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ymapFiles.Count == 0) return;

        Console.WriteLine(Messages.Get("patching_ymap_message", name));
        
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
            mainYmap.LODLights.LodLights = MergeWithRemovals(mainYmap, ymapFiles, y => y.LODLights.LodLights, new YmapLodLightComparer());

        if (mainYmap.DistantLODLights is { positions: not null } && mainYmap.DistantLODLights.positions.Length != 0)
            mainYmap.DistantLODLights.positions = MergeWithRemovals(mainYmap, ymapFiles, y => y.DistantLODLights.positions, new YmapDistantLodLightComparer());

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

    private T[] MergeWithRemovals<T>(
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

    private YmapEntityDef[] MergeYmapEntities(YmapFile mainYmap, List<YmapFile> ymapFiles)
    {
        Console.WriteLine("Merging Ymap Entities...");

        Dictionary<uint, YmapEntityDef> mainEntities = mainYmap.AllEntities.ToDictionary(e => e._CEntityDef.guid, e => e);
        Dictionary<uint, YmapEntityDef> patchEntities = ymapFiles
            .SelectMany(patchFile => patchFile.AllEntities ?? [])
            .GroupBy(e => e._CEntityDef.guid)
            .ToDictionary(g => g.Key, g => g.First());

        Console.WriteLine($"Main entities: {mainEntities.Count}, Patch entities: {patchEntities.Count}");

        foreach ((uint guid, YmapEntityDef entity) in patchEntities)
        {
            if (mainEntities.ContainsKey(guid)) continue;
            Console.WriteLine($"Adding new entity: {guid}");
            mainYmap.AddEntity(entity);
        }

        foreach ((uint guid, YmapEntityDef entity) in mainEntities)
        {
            bool shouldRemove = false;

            foreach (YmapFile patchFile in ymapFiles)
            {
                if (patchFile.AllEntities == null)
                {
                    shouldRemove = true;
                    Console.WriteLine($"Entity {guid} marked for removal due to null patch entities.");
                    break;
                }

                bool foundInPatch = patchFile.AllEntities.Any(e => e?._CEntityDef.guid == guid);
                if (foundInPatch) continue;

                shouldRemove = true;
                Console.WriteLine($"Entity {guid} not found in patch {patchFile.Name}, marked for removal.");
                break;
            }

            if (!shouldRemove) continue;
            Console.WriteLine($"Removing entity: {guid}");
            mainYmap.RemoveEntity(entity);
        }

        List<YmapEntityDef> result = mainYmap.AllEntities.ToList();
        for (int i = 0; i < result.Count; i++)
        {
            YmapEntityDef entity = result[i];
            
            bool nameChanged = false;
            uint entityGuid = entity._CEntityDef.guid;
            
            MergePositionX.Clear();
            MergePositionY.Clear();
            MergePositionZ.Clear();
        
            MergeOrientationX.Clear();
            MergeOrientationY.Clear();
            MergeOrientationZ.Clear();
            MergeOrientationW.Clear();
        
            MergeScaleX.Clear();
            MergeScaleY.Clear();
            MergeScaleZ.Clear();
        
            MergeDistance.Clear();
            MergeLodDist.Clear();
        
            MergeArchetype = entity.Archetype;
            
            foreach (YmapFile patchYmap in ymapFiles)
            {
                if (patchYmap.AllEntities == null || patchYmap.AllEntities.Length == 0)
                {
                    Console.WriteLine($"Patch {patchYmap.Name} has no entities, skipping.");
                    continue;
                }
                
                foreach (YmapEntityDef patchEntity in patchYmap.AllEntities)
                {
                    if (patchEntity == null || patchEntity._CEntityDef.guid != entityGuid) continue;
                    if (!nameChanged && entity.Name != patchEntity.Name)
                    {
                        Console.WriteLine($"Updating entity name: {entity.Name} -> {patchEntity.Name}");
                        entity._CEntityDef.archetypeName = patchEntity._CEntityDef.archetypeName;
                        nameChanged = true;
                    }
                    ApplyEntityPatches(entity, patchEntity);
                }
            }

            float patchPositionX = MergePositionX.Count > 0 ? entity.Position.X < 0 ? MergePositionX.Min() : MergePositionX.Max() : entity.Position.X;
            float patchPositionY = MergePositionY.Count > 0 ? entity.Position.Y < 0 ? MergePositionY.Min() : MergePositionY.Max() : entity.Position.Y;
            float patchPositionZ = MergePositionZ.Count > 0 ? MergePositionZ.Min() : entity.Position.Z;
            
            Console.WriteLine($"Patch position: {entity.Position.X} -> {patchPositionX}, {entity.Position.Y} -> {patchPositionY}, {entity.Position.Z} -> {patchPositionZ}");
            
            float patchOrientationX = MergeOrientationX.Count > 0 ? MergeOrientationX.Average() : entity.Orientation.X;
            float patchOrientationY = MergeOrientationY.Count > 0 ? MergeOrientationY.Average() : entity.Orientation.Y;
            float patchOrientationZ = MergeOrientationZ.Count > 0 ? MergeOrientationZ.Average() : entity.Orientation.Z;
            float patchOrientationW = MergeOrientationW.Count > 0 ? MergeOrientationW.Average() : entity.Orientation.W;
            
            Console.WriteLine($"Patch orientation: {entity.Orientation.X} -> {patchOrientationX}, {entity.Orientation.Y} -> {patchOrientationY}, {entity.Orientation.Z} -> {patchOrientationZ}, {entity.Orientation.W} -> {patchOrientationW}");
            
            float patchScaleX = MergeScaleX.Count > 0 ? MergeScaleX.Average() : entity.Scale.X;
            float patchScaleY = MergeScaleY.Count > 0 ? MergeScaleY.Average() : entity.Scale.Y;
            float patchScaleZ = MergeScaleZ.Count > 0 ? MergeScaleZ.Average() : entity.Scale.Z;
            
            Console.WriteLine($"Patch scale: {entity.Scale.X} -> {patchScaleX}, {entity.Scale.Y} -> {patchScaleY}, {entity.Scale.Z} -> {patchScaleZ}");
            
            float patchDistance = MergeDistance.Count > 0 ? MergeDistance.Average() : entity.Distance;
            float patchLodDist = MergeLodDist.Count > 0 ? MergeLodDist.Average() : entity.LodDist;
            
            Console.WriteLine($"Patch distance: {entity.Distance} -> {patchDistance}, LodDist: {entity.LodDist} -> {patchLodDist}");
            
            entity.SetPosition(new Vector3(patchPositionX, patchPositionY, patchPositionZ));
            entity.SetOrientation(new Quaternion(patchOrientationX, patchOrientationY, patchOrientationZ, patchOrientationW));
            entity.SetScale(new Vector3(patchScaleX, patchScaleY, patchScaleZ));
            
            entity.LodDist = patchLodDist;
            entity.Distance = patchDistance;
            entity.Archetype = MergeArchetype;
            
            result[i] = entity;
        }

        Console.WriteLine("Merge complete.");
        return result.ToArray();
    }

    private void ApplyEntityPatches(YmapEntityDef mainEntity, YmapEntityDef patchEntity)
    {
        float tolerance = 0.01f;
        
        Vector3 mainPosition = mainEntity.Position;
        Vector3 patchPosition = patchEntity.Position;

        if (Math.Abs(mainPosition.X - patchPosition.X) >= tolerance)
        {
            MergePositionX.Add(patchPosition.X);
        }

        if (Math.Abs(mainPosition.Y - patchPosition.Y) >= tolerance)
        {
            MergePositionY.Add(patchPosition.Y);
        }

        if (patchPosition.Z < mainPosition.Z)
        {
            if (patchPosition.Z < -200.0f)
            {
                MergePositionZ.Add(-200.0f);
            }
            else
            {
                MergePositionZ.Add(patchPosition.Z);
            }
        }

        tolerance = 0.1f;
        Quaternion mainOrientation = mainEntity.Orientation;
        Quaternion patchOrientation = patchEntity.Orientation;

        if (Math.Abs(mainOrientation.X - patchOrientation.X) >= tolerance)
        {
            MergeOrientationX.Add(patchOrientation.X);
        }

        if (Math.Abs(mainOrientation.Y - patchOrientation.Y) >= tolerance)
        {
            MergeOrientationY.Add(patchOrientation.Y);
        }

        if (Math.Abs(mainOrientation.Z - patchOrientation.Z) >= tolerance)
        {
            MergeOrientationZ.Add(patchOrientation.Z);
        }

        if (Math.Abs(mainOrientation.W - patchOrientation.W) >= tolerance)
        {
            MergeOrientationW.Add(patchOrientation.W);
        }

        tolerance = 0.01f;
        Vector3 mainScale = mainEntity.Scale;
        Vector3 patchScale = patchEntity.Scale;

        if (Math.Abs(mainScale.X - patchScale.X) >= tolerance)
        {
            MergeScaleX.Add(patchScale.X);
        }

        if (Math.Abs(mainScale.Y - patchScale.Y) >= tolerance)
        {
            MergeScaleY.Add(patchScale.Y);
        }

        if (Math.Abs(mainScale.Z - patchScale.Z) >= tolerance)
        {
            MergeScaleZ.Add(patchScale.Z);
        }

        if (Math.Abs(mainEntity.Distance - patchEntity.Distance) >= tolerance)
        {
            MergeDistance.Add(patchEntity.Distance);
        }

        if (Math.Abs(mainEntity.LodDist - patchEntity.LodDist) >= tolerance)
        {
            MergeLodDist.Add(patchEntity.LodDist);
        }
    }
}
