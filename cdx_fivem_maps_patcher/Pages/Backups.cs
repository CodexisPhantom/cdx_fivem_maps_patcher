using cdx_fivem_maps_patcher.Classes;
using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Pages;

public class Backups(string path) : Page
{
    public void Show()
    {
        while (true)
        {
            PrintMenu();
            string? input = Console.ReadLine();
            Console.Clear();
            switch (input)
            {
                case "1":
                    ShowBackups();
                    break;
                case "2":
                    RemoveBackupMenu();
                    break;
                case "3":
                    RemoveAllBackupMenu();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine(Messages.Get("invalid_entry"));
                    break;
            }
        }
    }

    public static void SaveYmap(string path, YmapFile ymap)
    {
        string resource = Path.Combine(path, "cdx_fivem_maps_patcher");
        if (!Directory.Exists(resource))
            Directory.CreateDirectory(resource);

        string stream = Path.Combine(resource, "stream");
        if (!Directory.Exists(stream)) Directory.CreateDirectory(stream);
        
        string ymapFolder = Path.Combine(stream, "ymaps");
        if (!Directory.Exists(ymapFolder)) Directory.CreateDirectory(ymapFolder);

        string fxmanifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fxmanifest.lua");
        if (File.Exists(fxmanifestPath))
            File.Copy(fxmanifestPath, resource + Path.DirectorySeparatorChar + "fxmanifest.lua", true);

        byte[]? data = ymap.Save();
        string fileName = Path.Combine(ymapFolder, ymap.Name);

        if (File.Exists(fileName)) File.Delete(fileName);

        File.Create(fileName).Close();
        if (data != null) File.WriteAllBytes(fileName, data);
    }
    
    public static void SaveYbn(string path, YbnFile ybn)
    {
        string resource = Path.Combine(path, "cdx_fivem_maps_patcher");
        if (!Directory.Exists(resource))
            Directory.CreateDirectory(resource);

        string stream = Path.Combine(resource, "stream");
        if (!Directory.Exists(stream)) Directory.CreateDirectory(stream);
        
        string ybnFolder = Path.Combine(stream, "ybn");
        if (!Directory.Exists(ybnFolder)) Directory.CreateDirectory(ybnFolder);

        string fxmanifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fxmanifest.lua");
        if (File.Exists(fxmanifestPath))
            File.Copy(fxmanifestPath, resource + Path.DirectorySeparatorChar + "fxmanifest.lua", true);

        byte[]? data = ybn.Save();
        string fileName = Path.Combine(ybnFolder, ybn.Name);

        if (File.Exists(fileName)) File.Delete(fileName);

        File.Create(fileName).Close();
        if (data != null) File.WriteAllBytes(fileName, data);
    }

    private static void PrintMenu()
    {
        Console.WriteLine(Messages.Get("main_menu_title"));
        Console.WriteLine(Messages.Get("backups_menu_list"));
        Console.WriteLine(Messages.Get("backups_menu_remove"));
        Console.WriteLine(Messages.Get("backups_menu_remove_all"));
        Console.WriteLine(Messages.Get("backups_menu_return"));
    }

    private void ShowBackups()
    {
        List<string> backups = GetDistinctBackups();
        if (backups.Count == 0)
            Console.WriteLine(Messages.Get("no_backups_found"));
        else
            for (int i = 0; i < backups.Count; i++)
                Console.WriteLine($"[{i + 1}] SERVER_PATH{backups[i].Replace(path, "")}");
    }

    private void RemoveBackupMenu()
    {
        List<string> backups = GetDistinctBackups();
        if (backups.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_backups_to_remove"));
            return;
        }

        Console.WriteLine(Messages.Get("select_backup_to_remove"));
        for (int i = 0; i < backups.Count; i++)
            Console.WriteLine($"[{i + 1}] SERVER_PATH{backups[i].Replace(path, "")}");

        Console.Write(Messages.Get("backup_number_prompt"));
        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= backups.Count)
            try
            {
                List<string> allbackups = GetAllBackups();
                string backupToDelete = Path.GetFileName(backups[choice - 1]);
                List<string> backupsToDelete =
                    allbackups.Where(backup => Path.GetFileName(backup) == backupToDelete).ToList();

                Console.WriteLine(Messages.Get("backups_to_delete"));
                backupsToDelete.ForEach(Console.WriteLine);
                Console.Write(Messages.Get("confirm_delete"));
                string? confirmation = Console.ReadLine();
                if ((Messages.Lang == "fr" && confirmation?.Trim().ToLower() == "o") ||
                    (Messages.Lang == "en" && confirmation?.Trim().ToLower() == "y"))
                {
                    foreach (string backup in backupsToDelete)
                    {
                        string originalFile = backup.EndsWith(".backup") ? backup[..^7] : backup;
                        if (File.Exists(originalFile)) File.Delete(originalFile);
                        File.Move(backup, originalFile);
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
        else
            Console.WriteLine(Messages.Get("delete_cancelled"));
    }

    private void RemoveAllBackupMenu()
    {
        List<string> backups = GetAllBackups();
        if (backups.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_backups_to_remove"));
            return;
        }

        Console.WriteLine(Messages.Get("confirm_delete_all"));
        string? confirmation = Console.ReadLine();
        if ((Messages.Lang == "fr" && confirmation?.Trim().ToLower() == "o") ||
            (Messages.Lang == "en" && confirmation?.Trim().ToLower() == "y"))
        {
            foreach (string backup in backups)
            {
                string originalFile = backup.EndsWith(".backup") ? backup[..^7] : backup;
                if (File.Exists(originalFile)) File.Delete(originalFile);
                File.Move(backup, originalFile);
            }

            Console.WriteLine(Messages.Get("all_backups_deleted"));
        }
        else
        {
            Console.WriteLine(Messages.Get("delete_cancelled"));
        }
    }

    private List<string> GetDistinctBackups()
    {
        try
        {
            List<string> allbackups = GetAllBackups();
            return allbackups
                .GroupBy(Path.GetFileName)
                .Select(g => g.First())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private List<string> GetAllBackups()
    {
        try
        {
            return Directory.GetFiles(path, "*.backup", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return [];
        }
    }
}