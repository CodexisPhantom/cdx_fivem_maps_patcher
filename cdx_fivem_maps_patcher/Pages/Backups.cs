using cdx_fivem_maps_patcher.Classes;
using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Pages;

public class Backups(string path) : Page
{
    public void Show()
    {
        while (true)
        {
            PrintMainMenu();
            string? input = Console.ReadLine();
            Console.Clear();
            switch (input)
            {
                case "1": ShowTypedBackups(".ymap", "YMAP"); break;
                case "2": ShowTypedBackups(".ybn", "YBN"); break;
                case "3": RemoveTypedBackupMenu(".ymap", "YMAP"); break;
                case "4": RemoveTypedBackupMenu(".ybn", "YBN"); break;
                case "5": RemoveAllTypedBackups(".ymap", "YMAP"); break;
                case "6": RemoveAllTypedBackups(".ybn", "YBN"); break;
                case "7": return;
                default: Console.WriteLine(Messages.Get("invalid_entry")); break;
            }
        }
    }

    private static void PrintMainMenu()
    {
        Console.WriteLine(Messages.Get("backups_menu_title"));
        Console.WriteLine(Messages.Get("backups_menu_show_ymap"));
        Console.WriteLine(Messages.Get("backups_menu_show_ybn"));
        Console.WriteLine(Messages.Get("backups_menu_remove_ymap"));
        Console.WriteLine(Messages.Get("backups_menu_remove_ybn"));
        Console.WriteLine(Messages.Get("backups_menu_remove_all_ymap"));
        Console.WriteLine(Messages.Get("backups_menu_remove_all_ybn"));
        Console.WriteLine(Messages.Get("backups_menu_return"));
    }

    public static void SaveYmap(string path, YmapFile ymap)
    {
        SaveMapFile(path, ymap.Name, ymap.Save(), "ymaps");
    }

    public static void SaveYbn(string path, YbnFile ybn)
    {
        SaveMapFile(path, ybn.Name, ybn.Save(), "ybn");
    }

    public static void CreateYbnBackup(string ybnFilePath)
    {
        try
        {
            if (!File.Exists(ybnFilePath))
            {
                Console.WriteLine(Messages.Get("file_not_found_error", ybnFilePath));
                return;
            }

            string backupPath = ybnFilePath + ".backup";
            
            if (File.Exists(backupPath))
            {
                Console.WriteLine($"  → Backup already exists for {Path.GetFileName(ybnFilePath)}");
                return;
            }

            File.Copy(ybnFilePath, backupPath, true);
            Console.WriteLine($"  ✓ Created backup: {Path.GetFileName(ybnFilePath)}.backup");
        }
        catch (Exception ex)
        {
            Console.WriteLine(Messages.Get("backup_creation_error", Path.GetFileName(ybnFilePath), ex.Message));
        }
    }

    public static void CreateYbnBackups(List<string> ybnFilePaths)
    {
        if (ybnFilePaths == null || ybnFilePaths.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_files_to_backup"));
            return;
        }

        Console.WriteLine(Messages.Get("creating_backups_header", ybnFilePaths.Count));
        
        int successCount = 0;
        int failureCount = 0;

        foreach (string filePath in ybnFilePaths)
        {
            try
            {
                CreateYbnBackup(filePath);
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(Messages.Get("backup_creation_error", Path.GetFileName(filePath), ex.Message));
                failureCount++;
            }
        }

        Console.WriteLine(Messages.Get("backup_summary", successCount, failureCount));
    }

    private static void SaveMapFile(string basePath, string fileName, byte[]? data, string subfolder)
    {
        string resourcePath = Path.Combine(basePath, "cdx_fivem_maps_patcher");
        string streamPath = Path.Combine(resourcePath, "stream");
        string targetFolder = Path.Combine(streamPath, subfolder);
        
        Directory.CreateDirectory(targetFolder);

        string fxmanifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fxmanifest.lua");
        if (File.Exists(fxmanifestPath))
            File.Copy(fxmanifestPath, Path.Combine(resourcePath, "fxmanifest.lua"), true);

        string filePath = Path.Combine(targetFolder, fileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        File.Create(filePath).Close();
        if (data != null) File.WriteAllBytes(filePath, data);
    }

    private void ShowTypedBackups(string extension, string label)
    {
        List<string> backups = GetDistinctBackups(extension);
        if (backups.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_backups_found", label));
            return;
        }

        Console.WriteLine(Messages.Get("backups_list_header", label));
        for (int i = 0; i < backups.Count; i++)
            Console.WriteLine($"[{i + 1}] SERVER_PATH{backups[i].Replace(path, "")}");
    }

    private void RemoveTypedBackupMenu(string extension, string label)
    {
        List<string> backups = GetDistinctBackups(extension);
        if (backups.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_backups_to_remove", label));
            return;
        }

        Console.WriteLine(Messages.Get("select_backup_to_remove", label));
        for (int i = 0; i < backups.Count; i++)
            Console.WriteLine($"[{i + 1}] SERVER_PATH{backups[i].Replace(path, "")}");

        Console.Write(Messages.Get("backup_number_prompt"));
        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= backups.Count)
        {
            try
            {
                string backupName = Path.GetFileName(backups[choice - 1]);
                List<string> backupsToDelete = GetAllBackups(extension)
                    .Where(b => Path.GetFileName(b) == backupName)
                    .ToList();

                Console.WriteLine(Messages.Get("backups_to_delete"));
                backupsToDelete.ForEach(Console.WriteLine);
                Console.Write(Messages.Get("confirm_delete"));
                string? confirmation = Console.ReadLine();

                if (IsConfirmed(confirmation))
                {
                    foreach (string backup in backupsToDelete)
                    {
                        string original = backup[..^7]; // Remove ".backup"
                        if (File.Exists(original)) File.Delete(original);
                        File.Move(backup, original);
                    }
                    Console.WriteLine(Messages.Get("backup_deleted"));
                }
                else
                {
                    Console.WriteLine(Messages.Get("delete_cancelled"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Messages.Get("delete_error", ex.Message));
            }
        }
        else
        {
            Console.WriteLine(Messages.Get("delete_cancelled"));
        }
    }

    private void RemoveAllTypedBackups(string extension, string label)
    {
        List<string> backups = GetAllBackups(extension);
        if (backups.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_backups_to_remove", label));
            return;
        }

        Console.WriteLine(Messages.Get("confirm_delete_all", label));
        string? confirmation = Console.ReadLine();
        if (IsConfirmed(confirmation))
        {
            foreach (string backup in backups)
            {
                string original = backup[..^7];
                if (File.Exists(original)) File.Delete(original);
                File.Move(backup, original);
            }

            Console.WriteLine(Messages.Get("all_backups_deleted", label));
        }
        else
        {
            Console.WriteLine(Messages.Get("delete_cancelled"));
        }
    }

    private static bool IsConfirmed(string? input)
    {
        input = input?.Trim().ToLower();
        return (Messages.Lang == "fr" && input == "o") || (Messages.Lang == "en" && input == "y");
    }

    private List<string> GetAllBackups(string extension)
    {
        try
        {
            return Directory.GetFiles(path, $"*{extension}.backup", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return [];
        }
    }

    private List<string> GetDistinctBackups(string extension)
    {
        try
        {
            return GetAllBackups(extension)
                .GroupBy(Path.GetFileName)
                .Select(g => g.First())
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
