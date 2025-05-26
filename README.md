# CDX FiveM Maps Patcher

**CDX FiveM Maps Patcher** is a command-line tool designed to help you patch `.ymap` map files for FiveM servers. It compares server-side YMAP entities with those from the original Grand Theft Auto V game files and applies necessary changes to attributes such as position, orientation, scale, and more. It also includes backup management tools.

## Features

- **YMAP Patching**  
  Detect and apply patches to `.ymap` files within a specified FiveM server path using base data from GTA V.

- **Backup Management**
    - List available backups.
    - Delete individual backups.
    - Delete all backups.

- **Command-Line Interface**  
  Simple text-based navigation with menu-driven options.

- **Localization Support**  
  Available in both English and French (default language is English).

---

## How It Works

1. **Initialization**
    - The tool prompts you to provide the installation path to GTA V and your FiveM server directory.
    - It builds a `GameFileCache` using `CodeWalker.Core` to access and read GTA V’s game files.

2. **Main Menu**
    - You are presented with options to manage backups, patch maps, or exit.

3. **YMAP Patching**
    - The tool scans your FiveM server’s `.ymap` files.
    - For each entity found, it attempts to locate the corresponding entity from the GTA V game files.
    - If differences are detected (beyond a defined threshold) in attributes like position, rotation, or scale, the server’s entity is updated accordingly.

4. **Backup Tools**
    - Easily browse, delete, or bulk-remove previously created backup files.

---

## Usage

1. Launch the application.
2. Follow the prompts to specify:
    - Your Grand Theft Auto V installation directory.
    - The root directory of your FiveM server.
3. Use the numeric menu options to navigate and execute actions.