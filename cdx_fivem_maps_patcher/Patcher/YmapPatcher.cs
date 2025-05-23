using cdx_fivem_maps_patcher.Classes;
using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Patcher;

public class YmapPatcher(GameFileCache gameFileCache, string serverPath) : Patcher(gameFileCache, serverPath)
{
    protected override void PrintMenu()
    {
        Console.WriteLine(Messages.Get("main_menu_title"));
        Console.WriteLine(Messages.Get("patch_menu_patch"));
        Console.WriteLine(Messages.Get("patch_menu_return"));
    }

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

            string[] ymapFiles = Directory.GetFiles(directoryPath, "*.ymap", SearchOption.AllDirectories);
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
                    Console.WriteLine($"Error accessing file {filePath}: {ex.Message}");
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
        //Console.WriteLine($"Patching {name}...");
        Dictionary<uint, RpfFileEntry> ymapDict = GameFileCache.YmapDict;

        uint ymapHash =
            (from entry in GameFileCache.YmapDict
                where entry.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                select entry.Key).FirstOrDefault();
        if (ymapHash == 0)
            //Console.WriteLine($"Ymap {name} not found in cache.");
            return;

        RpfFileEntry ymapEntry = ymapDict[ymapHash];
        YmapFile? mainYmap = RpfManager.GetFile<YmapFile>(ymapEntry);

        List<YmapFile> ymapFiles = [];
        foreach (string filePath in files)
            try
            {
                YmapFile ymap = OpenFile(filePath);
                ymapFiles.Add(ymap);
                //File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception)
            {
                //Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ymapFiles.Count == 0)
            //Console.WriteLine($"No valid Ymap files found to patch {name}.");
            return;

        if (mainYmap.AllEntities != null && mainYmap.AllEntities.Length != 0) MergeYmapEntities(mainYmap, ymapFiles);
    }

    private static YmapEntityDef[] MergeYmapEntities(YmapFile mainYmap, List<YmapFile> ymapFiles)
    {
        List<YmapEntityDef> mainEntities = mainYmap.AllEntities.ToList();

        List<YmapEntityDef> entitiesToAdd = [];
        List<YmapEntityDef> entitiesToRemove = [];

        YmapEntityDef[] entites = mainYmap.AllEntities;

        foreach (YmapFile ymap in ymapFiles)
        {
            if (ymap.AllEntities == null || ymap.AllEntities.Length == 0) continue;

            entitiesToAdd.AddRange(ymap.AllEntities.Where(entity => !entites.Contains(entity)));
        }

        return mainEntities.ToArray();
    }
}