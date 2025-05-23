using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Classes;

public class Patcher
{
    private readonly GameFileCache _gameFileCache;
    private readonly RpfManager _rpfManager;
    private readonly string _serverPath;

    public Patcher(GameFileCache gameFileCache, string serverPath)
    {
        _rpfManager = new RpfManager();
        _gameFileCache = gameFileCache;
        _serverPath = serverPath;
    }

    public void Init()
    {
        while (true)
        {
            PrintMenu();
            string? input = Console.ReadLine();
            Console.Clear();
            switch (input)
            {
                case "1":
                    PatchYmaps();
                    break;
                case "2":
                    return;
                default:
                    Console.WriteLine(Messages.Get("invalid_entry"));
                    break;
            }
        }
    }

    private void PrintMenu()
    {
        Console.WriteLine(Messages.Get("main_menu_title"));
        Console.WriteLine(Messages.Get("patch_menu_patch"));
        Console.WriteLine(Messages.Get("patch_menu_return"));
    }

    private void PatchYmaps()
    {
        Dictionary<string, List<string>> duplicates = FindDuplicateYmapFiles(_serverPath);
        if (duplicates.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_duplicates_found"));
            return;
        }

        Console.WriteLine(Messages.Get("duplicates_found"));
        foreach (KeyValuePair<string, List<string>> entry in duplicates) PatchYmap(entry.Key, entry.Value);
    }

    private void PatchYmap(string name, List<string> files)
    {
        //Console.WriteLine($"Patching {name}...");
        Dictionary<uint, RpfFileEntry> ymapDict = _gameFileCache.YmapDict;

        uint ymapHash =
            (from entry in _gameFileCache.YmapDict
                where entry.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                select entry.Key).FirstOrDefault();
        if (ymapHash == 0)
            //Console.WriteLine($"Ymap {name} not found in cache.");
            return;

        RpfFileEntry ymapEntry = ymapDict[ymapHash];
        YmapFile? mainYmap = _rpfManager.GetFile<YmapFile>(ymapEntry);

        List<YmapFile> ymapFiles = [];
        foreach (string filePath in files)
            try
            {
                YmapFile ymap = OpenFile(filePath);
                ymapFiles.Add(ymap);
                //File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ymapFiles.Count == 0)
            //Console.WriteLine($"No valid Ymap files found to patch {name}.");
            return;

        if (mainYmap.AllEntities != null && mainYmap.AllEntities.Length != 0) MergeYmapEntities(mainYmap, ymapFiles);
    }

    private YmapEntityDef[] MergeYmapEntities(YmapFile mainYmap, List<YmapFile> ymapFiles)
    {
        List<YmapEntityDef> mainEntities = mainYmap.AllEntities.ToList();

        List<YmapEntityDef> entitiesToAdd = [];
        List<YmapEntityDef> entitiesToRemove = [];

        YmapEntityDef[] entites = mainYmap.AllEntities;

        foreach (YmapFile ymap in ymapFiles)
        {
            if (ymap.AllEntities == null || ymap.AllEntities.Length == 0) continue;

            foreach (YmapEntityDef? entity in ymap.AllEntities)
                if (!entites.Contains(entity))
                    entitiesToAdd.Add(entity);
        }

        return mainEntities.ToArray();
    }

    public YmapFile OpenFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string name = new FileInfo(path).Name;
        RpfFileEntry fileEntry = CreateFileEntry(name, path, ref data);
        YmapFile? ymap = RpfFile.GetFile<YmapFile>(fileEntry, data);
        ymap.FilePath = path;
        return ymap;
    }

    private RpfFileEntry CreateFileEntry(string name, string path, ref byte[] data)
    {
        RpfFileEntry e;
        uint rsc7 = data.Length > 4 ? BitConverter.ToUInt32(data, 0) : 0;
        if (rsc7 == 0x37435352)
        {
            e = RpfFile.CreateResourceFileEntry(ref data, 0);
            data = ResourceBuilder.Decompress(data);
        }
        else
        {
            RpfBinaryFileEntry be = new()
            {
                FileSize = (uint)data.Length
            };
            be.FileUncompressedSize = be.FileSize;
            e = be;
        }

        e.Name = name;
        e.NameLower = name.ToLowerInvariant();
        e.NameHash = JenkHash.GenHash(e.NameLower);
        e.ShortNameHash = JenkHash.GenHash(Path.GetFileNameWithoutExtension(e.NameLower));
        e.Path = path;
        return e;
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
}