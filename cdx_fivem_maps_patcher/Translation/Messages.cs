namespace cdx_fivem_maps_patcher.Classes;

public static class Messages
{
    private static readonly Dictionary<string, Dictionary<string, string>> _messages = new()
    {
        ["fr"] = new Dictionary<string, string>
        {
            ["main_menu_title"] = "\n=== CDX | Fivem Maps Patcher Menu ===",
            ["main_menu_backups"] = "[1] Options de sauvegarde",
            ["main_menu_patch"] = "[2] Patch maps",
            ["main_menu_quit"] = "[3] Quitter",
            ["invalid_entry"] = "Entrée invalide. Veuillez réessayer.",
            ["goodbye"] = "Au revoir !",
            ["prompt_gta_path"] = "Veuillez entrer le chemin d'installation de GTA V : ",
            ["prompt_server_path"] = "Veuillez entrer le chemin du server : ",
            ["invalid_path"] = "Chemin invalide. Veuillez réessayer.",
            ["path_used"] = "Chemin utilisé : {0}",
            ["backups_menu_list"] = "[1] Lister les sauvegardes",
            ["backups_menu_remove"] = "[2] Supprimer une sauvegarde",
            ["backups_menu_remove_all"] = "[3] Supprimer toutes les sauvegardes",
            ["backups_menu_return"] = "[4] Retourner",
            ["no_backups_found"] = "Aucune sauvegarde trouvée.",
            ["no_backups_to_remove"] = "Aucune sauvegarde à supprimer.",
            ["select_backup_to_remove"] = "Sélectionnez la sauvegarde à supprimer :",
            ["backup_number_prompt"] = "Numéro de la sauvegarde à supprimer (ou 0 pour annuler) : ",
            ["backups_to_delete"] = "Les fichiers suivants seront supprimés :",
            ["confirm_delete"] = "Confirmez-vous la suppression ? (o/n) : ",
            ["confirm_delete_all"] = "Confirmez-vous la suppression de toutes les sauvegardes ? (o/n) : ",
            ["backup_deleted"] = "Sauvegarde supprimée.",
            ["all_backups_deleted"] = "Toutes les sauvegardes supprimées.",
            ["delete_cancelled"] = "Suppression annulée.",
            ["delete_error"] = "Erreur lors de la suppression : {0}",
            ["patch_menu_ymaps"] = "[1] Patch les fichiers ymaps",
            ["patch_menu_ybns"] = "[2] Patch les fichiers ybn",
            ["patch_menu_return"] = "[3] Retourner",
            ["no_duplicates_found"] = "Aucun fichier .ymap dupliqué trouvé.",
            ["duplicates_found"] = "Fichiers .ymap dupliqués trouvés :"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["main_menu_title"] = "\n=== CDX | Fivem Maps Patcher Menu ===",
            ["main_menu_backups"] = "[1] Backups options",
            ["main_menu_patch"] = "[2] Patch maps",
            ["main_menu_quit"] = "[3] Quit",
            ["invalid_entry"] = "Invalid entry. Please try again.",
            ["goodbye"] = "Goodbye!",
            ["prompt_gta_path"] = "Please enter GTA V installation path: ",
            ["prompt_server_path"] = "Please enter server path: ",
            ["invalid_path"] = "Invalid path. Please try again.",
            ["path_used"] = "Path used: {0}",
            ["backups_menu_list"] = "[1] List backups",
            ["backups_menu_remove"] = "[2] Remove a backup",
            ["backups_menu_remove_all"] = "[3] Remove all backups",
            ["backups_menu_return"] = "[4] Return",
            ["no_backups_found"] = "No backups found.",
            ["no_backups_to_remove"] = "No backups to remove.",
            ["select_backup_to_remove"] = "Select backup to remove:",
            ["backup_number_prompt"] = "Backup number to remove (or 0 to cancel): ",
            ["backups_to_delete"] = "The following files will be deleted:",
            ["confirm_delete"] = "Do you confirm deletion? (y/n): ",
            ["confirm_delete_all"] = "Do you confirm deletion of all backups? (y/n): ",
            ["backup_deleted"] = "Backup deleted.",
            ["all_backups_deleted"] = "All backups deleted.",
            ["delete_cancelled"] = "Deletion cancelled.",
            ["delete_error"] = "Error while deleting: {0}",
            ["patch_menu_ymaps"] = "[1] Patch ymap files",
            ["patch_menu_ybns"] = "[2] Patch ybn files",
            ["patch_menu_return"] = "[3] Return",
            ["no_duplicates_found"] = "No duplicate .ymap files found.",
            ["duplicates_found"] = "Duplicate .ymap files found:"
        }
    };

    public static string Lang { get; set; } = "fr";

    public static string Get(string key, params object[] args)
    {
        if (_messages.TryGetValue(Lang, out Dictionary<string, string>? dict) &&
            dict.TryGetValue(key, out string? value)) return args.Length > 0 ? string.Format(value, args) : value;
        return key;
    }
}