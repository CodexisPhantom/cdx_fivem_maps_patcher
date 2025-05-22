using cdx_fivem_maps_patcher.Classes;
using CodeWalker.GameFiles;

const double cacheTime = 60.0;
const long cacheSize = 2L * 1024 * 1024 * 1024; // 2GB Cache
const bool isGen9 = false;
const bool enableMods = false;
const string dlc = "";
const string excludeFolders = "";

string gtaPath = PromptPath("Veuillez entrer le chemin d'installation de GTA V : ");
string serverPath = PromptPath("Veuillez entrer le chemin du server : ");

GTA5Keys.LoadFromPath(gtaPath);
GameFileCache gameFileCache = new(cacheSize, cacheTime, gtaPath, isGen9, dlc, enableMods, excludeFolders);
gameFileCache.EnableDlc = true;
gameFileCache.Init(
    message => Console.WriteLine($"[GameFileCache] {message}"),
    error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
);

Console.Clear();
Backups backups = new(serverPath);
Patcher patcher = new(gameFileCache, serverPath);

while (true)
{
    PrintMainMenu();
    string input = Console.ReadLine();
    Console.Clear();
    switch (input)
    {
        case "1":
            backups.Init();
            break;
        case "2":
                patcher.Init();
            break;
        case "3":
            Console.WriteLine("Au revoir !");
            return;
        default:
            Console.WriteLine("Entrée invalide. Veuillez réessayer.");
            break;
    }
}

void PrintMainMenu()
{
    Console.WriteLine("\n=== CDX | Fivem Maps Patcher Menu ===");
    Console.WriteLine("[1] Backups options");
    Console.WriteLine("[2] Patch maps");
    Console.WriteLine("[3] Quitter");
}

string PromptPath(string message)
{
    string? path = null;
    while (string.IsNullOrEmpty(path))
    {
        Console.Write(message);
        path = Console.ReadLine();
        if (Directory.Exists(path)) continue;
        Console.WriteLine("Chemin invalide. Veuillez réessayer.");
        path = null;
    }
    Console.WriteLine($"Chemin utilisé : {path}");
    return path;
}
