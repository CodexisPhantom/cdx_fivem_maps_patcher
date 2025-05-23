using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CodeWalker.Utils;
using CodeWalker.World;
using SharpDX;

namespace CodeWalker.GameFiles
{
    public class GameFileCache
    {
        public static Dictionary<MetaHash, ShaderGen9XmlDataCollection> ShadersGen9ConversionData;
        private readonly Dictionary<uint, Archetype> archetypeDict = new Dictionary<uint, Archetype>();
        private readonly string ExcludeFolders;
        private readonly string GTAFolder;

        private readonly bool GTAGen9;

        ////dynamic cache
        private readonly Cache<GameFileCacheKey, GameFile> mainCache;

        private readonly Dictionary<uint, Archetype>
            projectArchetypes =
                new Dictionary<uint, Archetype>(); //used to override archetypes in world view with project ones


        private readonly Dictionary<GameFileCacheKey, GameFile> projectFiles =
            new Dictionary<GameFileCacheKey, GameFile>(); //for cache files loaded in project window: ydr,ydd,ytd,yft

        private readonly ConcurrentQueue<GameFile> requestQueue = new ConcurrentQueue<GameFile>();
        private readonly object requestSyncRoot = new object();
        private readonly Dictionary<uint, RpfFileEntry> textureLookup = new Dictionary<uint, RpfFileEntry>();
        private readonly object textureSyncRoot = new object(); //for the texture lookup.

        private readonly object updateSyncRoot = new object();

        private volatile bool archetypesLoaded;
        public Dictionary<MetaHash, RelData> AudioCategsDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioConfigDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioCurvesDict = new Dictionary<MetaHash, RelData>();


        public List<RelFile> AudioDatRelFiles = new List<RelFile>();
        public Dictionary<MetaHash, RelData> AudioGameDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioMixersDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSoundsDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSpeechDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSynthsDict = new Dictionary<MetaHash, RelData>();
        public bool BuildExtendedJenkIndex = true;

        public bool DoFullStringIndex = false;
        private Action<string> ErrorLog;
        private Dictionary<MetaHash, MetaHash> hdtexturelookup;
        public volatile bool IsInited;
        public bool LoadArchetypes = true;
        public bool LoadAudio = true;
        public bool LoadPeds = true;
        public bool LoadVehicles = true;
        public int MaxItemsPerLoop = 1; //to keep things flowing...
        private bool PreloadedMode;
        public RpfManager RpfMan;
        private Dictionary<MetaHash, MetaHash> textureParents;

        public Dictionary<uint, TimecycleMod> TimeCycleModsDict = new Dictionary<uint, TimecycleMod>();
        private Action<string> UpdateStatus;


        public GameFileCache(long size, double cacheTime, string folder, bool gen9, string dlc, bool mods,
            string excludeFolders)
        {
            mainCache = new Cache<GameFileCacheKey, GameFile>(size, cacheTime); //2GB is good as default
            SelectedDlc = dlc;
            EnableDlc = !string.IsNullOrEmpty(SelectedDlc);
            EnableMods = mods;
            GTAGen9 = gen9;
            GTAFolder = folder;
            ExcludeFolders = excludeFolders;
        }


        //static indexes
        public Dictionary<uint, RpfFileEntry> YdrDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YddDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YtdDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YmapDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YftDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YbnDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YcdDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YedDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YnvDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> Gxt2Dict { get; private set; }


        public Dictionary<uint, RpfFileEntry> AllYmapsDict { get; private set; }


        //static cached data loaded at init
        public Dictionary<uint, YtypFile> YtypDict { get; set; }

        public List<CacheDatFile> AllCacheFiles { get; set; }
        public Dictionary<uint, MapDataStoreNode> YmapHierarchyDict { get; set; }

        public List<YmfFile> AllManifests { get; set; }


        public bool EnableDlc { get; set; } //true;//
        public bool EnableMods { get; set; }

        public List<string> DlcPaths { get; set; } = new List<string>();
        public List<RpfFile> DlcActiveRpfs { get; set; } = new List<RpfFile>();
        public List<DlcSetupFile> DlcSetupFiles { get; set; } = new List<DlcSetupFile>();
        public List<DlcExtraFolderMountFile> DlcExtraFolderMounts { get; set; } = new List<DlcExtraFolderMountFile>();
        public Dictionary<string, string> DlcPatchedPaths { get; set; } = new Dictionary<string, string>();
        public List<string> DlcCacheFileList { get; set; } = new List<string>();
        public List<string> DlcNameList { get; set; } = new List<string>();
        public string SelectedDlc { get; set; } = string.Empty;

        public Dictionary<string, RpfFile> ActiveMapRpfFiles { get; set; } = new Dictionary<string, RpfFile>();

        public Dictionary<MetaHash, VehicleInitData> VehiclesInitDict { get; set; }
        public Dictionary<MetaHash, CPedModelInfo__InitData> PedsInitDict { get; set; }
        public Dictionary<MetaHash, PedFile> PedVariationsDict { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedDrawableDicts { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedTextureDicts { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedClothDicts { get; set; }


        public List<RpfFile> BaseRpfs { get; private set; }
        public List<RpfFile> AllRpfs { get; private set; }
        public List<RpfFile> DlcRpfs { get; private set; }


        public int QueueLength => requestQueue.Count;

        public int ItemCount => mainCache.Count;

        public long MemoryUsage => mainCache.CurrentMemoryUsage;


        public void Clear()
        {
            IsInited = false;

            mainCache.Clear();

            textureLookup.Clear();

            GameFile queueclear;
            while (requestQueue.TryDequeue(out queueclear))
            {
            } //empty the old queue out...
        }

        public void Init(Action<string> updateStatus, Action<string> errorLog)
        {
            UpdateStatus = updateStatus;
            ErrorLog = errorLog;

            Clear();


            if (RpfMan == null)
            {
                //EnableDlc = !string.IsNullOrEmpty(SelectedDlc);


                RpfMan = new RpfManager();
                RpfMan.ExcludePaths = GetExcludePaths();
                RpfMan.EnableMods = EnableMods;
                RpfMan.BuildExtendedJenkIndex = BuildExtendedJenkIndex;
                RpfMan.Init(GTAFolder, GTAGen9, UpdateStatus, ErrorLog); //, true);


                InitGlobal();

                InitDlc();


                //RE test area!
                //TestAudioRels();
                //TestAudioYmts();
                //TestAudioAwcs();
                //TestMetas();
                //TestPsos();
                //TestRbfs();
                //TestCuts();
                //TestYlds();
                //TestYeds();
                //TestYcds();
                //TestYtds();
                //TestYbns();
                //TestYdrs();
                //TestYdds();
                //TestYfts();
                //TestYpts();
                //TestYnvs();
                //TestYvrs();
                //TestYwrs();
                //TestYmaps();
                //TestYpdbs();
                //TestYfds();
                //TestMrfs();
                //TestFxcs();
                //TestPlacements();
                //TestDrawables();
                //TestCacheFiles();
                //TestHeightmaps();
                //TestWatermaps();
                //GetShadersXml();
                //GetShadersLegacyConversionXml();
                //GetShadersGen9ConversionXml();
                //GetArchetypeTimesList();
                //string typestr = PsoTypes.GetTypesString();
            }
            else
            {
                GC.Collect(); //try free up some of the previously used memory..
            }

            UpdateStatus("Scan complete");


            IsInited = true;
        }

        public void Init(Action<string> updateStatus, Action<string> errorLog, List<RpfFile> allRpfs)
        {
            UpdateStatus = updateStatus;
            ErrorLog = errorLog;

            Clear();

            PreloadedMode = true;
            EnableDlc = true; //just so everything (mainly archetypes) will load..
            EnableMods = false;
            RpfMan = new RpfManager(); //try not to use this in this mode...
            RpfMan.Init(allRpfs, GTAGen9);

            AllRpfs = allRpfs;
            BaseRpfs = allRpfs;
            DlcRpfs = new List<RpfFile>();

            UpdateStatus("Building global dictionaries...");
            InitGlobalDicts();

            UpdateStatus("Loading manifests...");
            InitManifestDicts();

            UpdateStatus("Loading global texture list...");
            InitGtxds();

            UpdateStatus("Loading archetypes...");
            InitArchetypeDicts();

            UpdateStatus("Loading strings...");
            InitStringDicts();

            UpdateStatus("Loading audio...");
            InitAudio();

            IsInited = true;
        }

        private void InitGlobal()
        {
            BaseRpfs = GetModdedRpfList(RpfMan.BaseRpfs);
            AllRpfs = GetModdedRpfList(RpfMan.AllRpfs);
            DlcRpfs = GetModdedRpfList(RpfMan.DlcRpfs);

            UpdateStatus("Building global dictionaries...");
            InitGlobalDicts();
        }

        private void InitDlc()
        {
            UpdateStatus("Building DLC List...");
            InitDlcList();

            UpdateStatus("Building active RPF dictionary...");
            InitActiveMapRpfFiles();

            UpdateStatus("Building map dictionaries...");
            InitMapDicts();

            UpdateStatus("Loading manifests...");
            InitManifestDicts();

            UpdateStatus("Loading global texture list...");
            InitGtxds();

            UpdateStatus("Loading cache...");
            InitMapCaches();

            UpdateStatus("Loading archetypes...");
            InitArchetypeDicts();

            UpdateStatus("Loading strings...");
            InitStringDicts();

            UpdateStatus("Loading vehicles...");
            InitVehicles();

            UpdateStatus("Loading peds...");
            InitPeds();

            UpdateStatus("Loading audio...");
            InitAudio();
        }

        private void InitDlcList()
        {
            //if (!EnableDlc) return;

            string dlclistpath = "update\\update.rpf\\common\\data\\dlclist.xml";
            //if (!EnableDlc)
            //{
            //    dlclistpath = "common.rpf\\data\\dlclist.xml";
            //}
            XmlDocument dlclistxml = RpfMan.GetFileXml(dlclistpath);

            DlcPaths.Clear();
            if (dlclistxml == null || dlclistxml.DocumentElement == null)
                ErrorLog("InitDlcList: Couldn't load " + dlclistpath + ".");
            else
                foreach (XmlNode pathsnode in dlclistxml.DocumentElement)
                foreach (XmlNode itemnode in pathsnode.ChildNodes)
                {
                    if (itemnode.NodeType != XmlNodeType.Element) continue;
                    DlcPaths.Add(itemnode.InnerText.ToLowerInvariant().Replace('\\', '/').Replace("platform:", "x64"));
                }


            //get dlc path names in the appropriate format for reference by the dlclist paths
            Dictionary<string, RpfFile> dlcDict = new Dictionary<string, RpfFile>();
            Dictionary<string, RpfFile> dlcDict2 = new Dictionary<string, RpfFile>();
            foreach (RpfFile dlcrpf in DlcRpfs)
            {
                if (dlcrpf == null) continue;
                if (dlcrpf.NameLower == "dlc.rpf")
                {
                    string path = GetDlcRpfVirtualPath(dlcrpf.Path);
                    string name = GetDlcNameFromPath(dlcrpf.Path);
                    dlcDict[path] = dlcrpf;
                    dlcDict2[name] = dlcrpf;
                }
            }


            //find all the paths for patched files in update.rpf and build the dict
            DlcPatchedPaths.Clear();
            string updrpfpath = "update\\update.rpf";
            RpfFile updrpffile = RpfMan.FindRpfFile(updrpfpath);

            if (updrpffile != null)
            {
                XmlDocument updsetupdoc = RpfMan.GetFileXml(updrpfpath + "\\setup2.xml");
                DlcSetupFile updsetupfile = new DlcSetupFile();
                updsetupfile.Load(updsetupdoc);

                XmlDocument updcontentdoc = RpfMan.GetFileXml(updrpfpath + "\\" + updsetupfile.datFile);
                DlcContentFile updcontentfile = new DlcContentFile();
                updcontentfile.Load(updcontentdoc);

                updsetupfile.DlcFile = updrpffile;
                updsetupfile.ContentFile = updcontentfile;
                updcontentfile.DlcFile = updrpffile;

                updsetupfile.deviceName = "update";
                updcontentfile.LoadDicts(updsetupfile, RpfMan, this);

                if (updcontentfile.ExtraTitleUpdates != null)
                    foreach (DlcExtraTitleUpdateMount tumount in updcontentfile.ExtraTitleUpdates.Mounts)
                    {
                        string lpath = tumount.path.ToLowerInvariant();
                        string relpath = lpath.Replace('/', '\\').Replace("update:\\", "");
                        string dlcname = GetDlcNameFromPath(relpath);
                        RpfFile dlcfile;
                        dlcDict2.TryGetValue(dlcname, out dlcfile);
                        if (dlcfile == null) continue;
                        string dlcpath = dlcfile.Path + "\\";
                        List<RpfFileEntry> files = updrpffile.GetFiles(relpath, true);
                        foreach (RpfFileEntry file in files)
                        {
                            if (file == null) continue;
                            string fpath = file.Path;
                            string frelpath = fpath.Replace(updrpfpath, "update:").Replace('\\', '/')
                                .Replace(lpath, dlcpath).Replace('/', '\\');
                            if (frelpath.StartsWith("mods\\")) frelpath = frelpath.Substring(5);
                            DlcPatchedPaths[frelpath] = fpath;
                        }
                    }
            }
            else
            {
                ErrorLog("InitDlcList: update.rpf not found!");
            }


            DlcSetupFiles.Clear();
            DlcExtraFolderMounts.Clear();

            foreach (string path in DlcPaths)
            {
                RpfFile dlcfile;
                if (dlcDict.TryGetValue(path, out dlcfile))
                    try
                    {
                        string setuppath = GetDlcPatchedPath(dlcfile.Path + "\\setup2.xml");
                        XmlDocument setupdoc = RpfMan.GetFileXml(setuppath);
                        DlcSetupFile setupfile = new DlcSetupFile();
                        setupfile.Load(setupdoc);

                        string contentpath = GetDlcPatchedPath(dlcfile.Path + "\\" + setupfile.datFile);
                        XmlDocument contentdoc = RpfMan.GetFileXml(contentpath);
                        DlcContentFile contentfile = new DlcContentFile();
                        contentfile.Load(contentdoc);

                        setupfile.DlcFile = dlcfile;
                        setupfile.ContentFile = contentfile;
                        contentfile.DlcFile = dlcfile;

                        contentfile.LoadDicts(setupfile, RpfMan, this);
                        foreach (DlcExtraFolderMountFile extramount in contentfile.ExtraMounts.Values) DlcExtraFolderMounts.Add(extramount);

                        DlcSetupFiles.Add(setupfile);
                    }
                    catch (Exception ex)
                    {
                        ErrorLog("InitDlcList: Error processing DLC " + path + "\n" + ex);
                    }
            }

            //load the DLC in the correct order.... 
            DlcSetupFiles = DlcSetupFiles.OrderBy(o => o.order).ToList();


            DlcNameList.Clear();
            foreach (DlcSetupFile sfile in DlcSetupFiles)
            {
                if (sfile == null || sfile.DlcFile == null) continue;
                DlcNameList.Add(GetDlcNameFromPath(sfile.DlcFile.Path));
            }

            if (DlcNameList.Count > 0)
                if (string.IsNullOrEmpty(SelectedDlc))
                    SelectedDlc = DlcNameList[DlcNameList.Count - 1];
        }

        private void InitImagesMetas()
        {
            //currently not used..

            ////parse images.meta
            //string imagesmetapath = "common.rpf\\data\\levels\\gta5\\images.meta";
            //if (EnableDlc)
            //{
            //    imagesmetapath = "update\\update.rpf\\common\\data\\levels\\gta5\\images.meta";
            //}
            //var imagesmetaxml = RpfMan.GetFileXml(imagesmetapath);
            //var imagesnodes = imagesmetaxml.DocumentElement.ChildNodes;
            //List<DlcContentDataFile> imagedatafilelist = new List<DlcContentDataFile>();
            //Dictionary<string, DlcContentDataFile> imagedatafiles = new Dictionary<string, DlcContentDataFile>();
            //foreach (XmlNode node in imagesnodes)
            //{
            //    DlcContentDataFile datafile = new DlcContentDataFile(node);
            //    string fname = datafile.filename.ToLower();
            //    fname = fname.Replace('\\', '/');
            //    imagedatafiles[fname] = datafile;
            //    imagedatafilelist.Add(datafile);
            //}


            //filter ActiveMapFiles based on images.meta?

            //DlcContentDataFile imagesdata;
            //if (imagedatafiles.TryGetValue(path, out imagesdata))
            //{
            //    ActiveMapRpfFiles[path] = baserpf;
            //}
        }

        private void InitActiveMapRpfFiles()
        {
            ActiveMapRpfFiles.Clear();

            foreach (RpfFile baserpf in BaseRpfs) //start with all the base rpf's (eg x64a.rpf)
            {
                string path = baserpf.Path.Replace('\\', '/');
                if (path == "common.rpf")
                {
                    ActiveMapRpfFiles["common"] = baserpf;
                }
                else
                {
                    int bsind = path.IndexOf('/');
                    if (bsind > 0 && bsind < path.Length)
                    {
                        path = "x64" + path.Substring(bsind);

                        //if (ActiveMapRpfFiles.ContainsKey(path))
                        //{ } //x64d.rpf\levels\gta5\generic\cutsobjects.rpf // x64g.rpf\levels\gta5\generic\cutsobjects.rpf - identical?

                        ActiveMapRpfFiles[path] = baserpf;
                    }
                    else
                    {
                        //do we need to include root rpf files? generally don't seem to contain map data?
                        ActiveMapRpfFiles[path] = baserpf;
                    }
                }
            }

            if (!EnableDlc) return; //don't continue for base title only

            foreach (RpfFile rpf in DlcRpfs)
                if (rpf.NameLower == "update.rpf") //include this so that files not in child rpf's can be used..
                {
                    string path = rpf.Path.Replace('\\', '/');
                    ActiveMapRpfFiles[path] = rpf;
                    break;
                }


            DlcActiveRpfs.Clear();
            DlcCacheFileList.Clear();

            //int maxdlcorder = 10000000;

            Dictionary<string, List<string>> overlays = new Dictionary<string, List<string>>();

            foreach (DlcSetupFile setupfile in DlcSetupFiles)
                if (setupfile.DlcFile != null)
                {
                    //if (setupfile.order > maxdlcorder)
                    //    break;
                    DlcContentFile contentfile = setupfile.ContentFile;
                    RpfFile dlcfile = setupfile.DlcFile;

                    DlcActiveRpfs.Add(dlcfile);

                    for (int i = 1; i <= setupfile.subPackCount; i++)
                    {
                        string subpackPath = dlcfile.Path.Replace("\\dlc.rpf", "\\dlc" + i + ".rpf");
                        RpfFile subpack = RpfMan.FindRpfFile(subpackPath);
                        if (subpack != null)
                        {
                            DlcActiveRpfs.Add(subpack);

                            if (setupfile.DlcSubpacks == null) setupfile.DlcSubpacks = new List<RpfFile>();
                            setupfile.DlcSubpacks.Add(subpack);
                        }
                    }


                    string dlcname = GetDlcNameFromPath(dlcfile.Path);
                    if (dlcname == "patchday27ng" &&
                        SelectedDlc !=
                        dlcname)
                        continue; //hack to fix map getting completely broken by this DLC.. but why? need to investigate further!


                    foreach (KeyValuePair<string, DlcContentDataFile> rpfkvp in contentfile.RpfDataFiles)
                    {
                        string umpath = GetDlcUnmountedPath(rpfkvp.Value.filename);
                        string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);

                        //if (rpfkvp.Value.overlay)
                        AddDlcOverlayRpf(rpfkvp.Key, umpath, setupfile, overlays);

                        AddDlcActiveMapRpfFile(rpfkvp.Key, phpath, setupfile);
                    }


                    DlcExtraFolderMountFile extramount;
                    DlcContentDataFile rpfdatafile;


                    foreach (DlcContentChangeSet changeset in contentfile.contentChangeSets)
                    {
                        if (changeset.useCacheLoader)
                        {
                            uint cachehash = JenkHash.GenHash(changeset.changeSetName.ToLowerInvariant());
                            string cachefilename = dlcname + "_" + cachehash + "_cache_y.dat";
                            string cachefilepath = dlcfile.Path + "\\x64\\data\\cacheloaderdata_dlc\\" + cachefilename;
                            string cachefilepathpatched = GetDlcPatchedPath(cachefilepath);
                            DlcCacheFileList.Add(cachefilepathpatched);
                            //if ((changeset.mapChangeSetData != null) && (changeset.mapChangeSetData.Count > 0))
                            //{ }
                            //else
                            //{ }
                        }

                        //if ((changeset.mapChangeSetData != null) && (changeset.mapChangeSetData.Count > 0))
                        //{ }
                        //if (changeset.executionConditions != null)
                        //{ }
                        //if (changeset.filesToInvalidate != null)
                        //{ }//not used
                        //if (changeset.filesToDisable != null)
                        //{ }//not used
                        if (changeset.filesToEnable != null)
                            foreach (string file in changeset.filesToEnable)
                            {
                                string dfn = GetDlcPlatformPath(file).ToLowerInvariant();
                                if (contentfile.ExtraMounts.TryGetValue(dfn, out extramount))
                                {
                                    //foreach (var rpfkvp in contentfile.RpfDataFiles)
                                    //{
                                    //    string umpath = GetDlcUnmountedPath(rpfkvp.Value.filename);
                                    //    string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);
                                    //    //if (rpfkvp.Value.overlay)
                                    //    AddDlcOverlayRpf(rpfkvp.Key, umpath, setupfile, overlays);
                                    //    AddDlcActiveMapRpfFile(rpfkvp.Key, phpath);
                                    //}
                                }
                                else if (contentfile.RpfDataFiles.TryGetValue(dfn, out rpfdatafile))
                                {
                                    string phpath = GetDlcRpfPhysicalPath(rpfdatafile.filename, setupfile);

                                    //if (rpfdatafile.overlay)
                                    AddDlcOverlayRpf(dfn, rpfdatafile.filename, setupfile, overlays);

                                    AddDlcActiveMapRpfFile(dfn, phpath, setupfile);
                                }
                                else
                                {
                                    if (dfn.EndsWith(".rpf"))
                                    {
                                    }
                                }
                            }

                        if (changeset.executionConditions != null)
                        {
                        }

                        if (changeset.mapChangeSetData != null)
                            foreach (DlcContentChangeSet mapcs in changeset.mapChangeSetData)
                            {
                                //if (mapcs.mapChangeSetData != null)
                                //{ }//not used
                                if (mapcs.filesToInvalidate != null)
                                    foreach (string file in mapcs.filesToInvalidate)
                                    {
                                        string upath = GetDlcMountedPath(file);
                                        string fpath = GetDlcPlatformPath(upath);
                                        if (fpath.EndsWith(".rpf"))
                                            RemoveDlcActiveMapRpfFile(fpath,
                                                overlays); //how to deal with individual files? milo_.interior
                                    }

                                if (mapcs.filesToDisable != null)
                                {
                                }

                                if (mapcs.filesToEnable != null)
                                    foreach (string file in mapcs.filesToEnable)
                                    {
                                        string fpath = GetDlcPlatformPath(file);
                                        string umpath = GetDlcUnmountedPath(fpath);
                                        string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);

                                        if (fpath != umpath)
                                        {
                                        }

                                        AddDlcOverlayRpf(fpath, umpath, setupfile, overlays);

                                        AddDlcActiveMapRpfFile(fpath, phpath, setupfile);
                                    }
                            }
                    }


                    if (dlcname == SelectedDlc) break; //everything's loaded up to the selected DLC.
                }
        }

        private void AddDlcActiveMapRpfFile(string vpath, string phpath, DlcSetupFile setupfile)
        {
            vpath = vpath.ToLowerInvariant();
            phpath = phpath.ToLowerInvariant();
            if (phpath.EndsWith(".rpf"))
            {
                RpfFile rpffile = RpfMan.FindRpfFile(phpath);
                if (rpffile != null) ActiveMapRpfFiles[vpath] = rpffile;
            }
            //how to handle individual files? eg interiorProxies.meta
        }

        private void AddDlcOverlayRpf(string path, string umpath, DlcSetupFile setupfile,
            Dictionary<string, List<string>> overlays)
        {
            string opath = GetDlcOverlayPath(umpath, setupfile);
            if (opath == path) return;
            List<string> overlayList;
            if (!overlays.TryGetValue(opath, out overlayList))
            {
                overlayList = new List<string>();
                overlays[opath] = overlayList;
            }

            overlayList.Add(path);
        }

        private void RemoveDlcActiveMapRpfFile(string vpath, Dictionary<string, List<string>> overlays)
        {
            List<string> overlayList;
            if (overlays.TryGetValue(vpath, out overlayList))
            {
                foreach (string overlayPath in overlayList)
                    if (ActiveMapRpfFiles.ContainsKey(overlayPath))
                        ActiveMapRpfFiles.Remove(overlayPath);

                overlays.Remove(vpath);
            }

            if (ActiveMapRpfFiles.ContainsKey(vpath)) ActiveMapRpfFiles.Remove(vpath);
            //nothing to remove?
        }

        private string GetDlcRpfPhysicalPath(string path, DlcSetupFile setupfile)
        {
            string devname = setupfile.deviceName.ToLowerInvariant();
            string fpath = GetDlcPlatformPath(path).ToLowerInvariant();
            string kpath = fpath; //.Replace(devname + ":\\", "");
            string dlcpath = setupfile.DlcFile.Path;
            fpath = fpath.Replace(devname + ":", dlcpath);
            fpath = fpath.Replace("x64:", dlcpath + "\\x64").Replace('/', '\\');
            if (setupfile.DlcSubpacks != null)
                if (RpfMan.FindRpfFile(fpath) == null)
                    foreach (RpfFile subpack in setupfile.DlcSubpacks)
                    {
                        dlcpath = subpack.Path;
                        string tpath = kpath.Replace(devname + ":", dlcpath);
                        tpath = tpath.Replace("x64:", dlcpath + "\\x64").Replace('/', '\\');
                        if (RpfMan.FindRpfFile(tpath) != null) return GetDlcPatchedPath(tpath);
                    }

            return GetDlcPatchedPath(fpath);
        }

        private string GetDlcOverlayPath(string path, DlcSetupFile setupfile)
        {
            string devname = setupfile.deviceName.ToLowerInvariant();
            string fpath = path.Replace("%PLATFORM%", "x64").Replace('\\', '/').ToLowerInvariant();
            string opath = fpath.Replace(devname + ":/", "");
            return opath;
        }

        private string GetDlcRpfVirtualPath(string path)
        {
            path = path.Replace('\\', '/');
            if (path.StartsWith("mods/")) path = path.Substring(5);
            if (path.Length > 7) path = path.Substring(0, path.Length - 7); //trim off "dlc.rpf"
            if (path.StartsWith("x64"))
            {
                int bsind = path.IndexOf('/'); //replace x64*.rpf
                if (bsind > 0 && bsind < path.Length) path = "x64" + path.Substring(bsind);
                //no hits here
            }
            else if (path.StartsWith("update/x64/dlcpacks"))
            {
                path = path.Replace("update/x64/dlcpacks", "dlcpacks:");
            } //no hits here

            return path;
        }

        private string GetDlcNameFromPath(string path)
        {
            string[] parts = path.ToLowerInvariant().Split('\\');
            if (parts.Length > 1) return parts[parts.Length - 2].ToLowerInvariant();
            return path;
        }

        public static string GetDlcPlatformPath(string path)
        {
            return path.Replace("%PLATFORM%", "x64").Replace('\\', '/').Replace("platform:", "x64").ToLowerInvariant();
        }

        private string GetDlcMountedPath(string path)
        {
            foreach (DlcExtraFolderMountFile efm in DlcExtraFolderMounts)
            foreach (DlcExtraFolderMount fm in efm.FolderMounts)
                if (fm.platform == null || fm.platform == "x64")
                    if (path.StartsWith(fm.path))
                        path = path.Replace(fm.path, fm.mountAs);

            return path;
        }

        private string GetDlcUnmountedPath(string path)
        {
            foreach (DlcExtraFolderMountFile efm in DlcExtraFolderMounts)
            foreach (DlcExtraFolderMount fm in efm.FolderMounts)
                if (fm.platform == null || fm.platform == "x64")
                    if (path.StartsWith(fm.mountAs))
                        path = path.Replace(fm.mountAs, fm.path);

            return path;
        }

        public string GetDlcPatchedPath(string path)
        {
            string p;
            if (DlcPatchedPaths.TryGetValue(path, out p)) return p;
            return path;
        }

        private List<RpfFile> GetModdedRpfList(List<RpfFile> list)
        {
            //if (!EnableMods) return new List<RpfFile>(list);
            List<RpfFile> rlist = new List<RpfFile>();
            RpfFile f;
            if (!EnableMods)
            {
                foreach (RpfFile file in list)
                    if (!file.Path.StartsWith("mods"))
                        rlist.Add(file);
            }
            else
            {
                foreach (RpfFile file in list)
                    if (RpfMan.ModRpfDict.TryGetValue(file.Path, out f))
                    {
                        rlist.Add(f);
                    }
                    else
                    {
                        if (file.Path.StartsWith("mods"))
                        {
                            string basepath = file.Path.Substring(5);
                            if (!RpfMan.RpfDict.ContainsKey(basepath)) //this file isn't overriding anything
                                rlist.Add(file);
                        }
                        else
                        {
                            rlist.Add(file);
                        }
                    }
            }

            return rlist;
        }


        private void InitGlobalDicts()
        {
            YdrDict = new Dictionary<uint, RpfFileEntry>();
            YddDict = new Dictionary<uint, RpfFileEntry>();
            YtdDict = new Dictionary<uint, RpfFileEntry>();
            YftDict = new Dictionary<uint, RpfFileEntry>();
            YcdDict = new Dictionary<uint, RpfFileEntry>();
            YedDict = new Dictionary<uint, RpfFileEntry>();
            foreach (RpfFile rpffile in AllRpfs)
            {
                if (rpffile.AllEntries == null) continue;
                foreach (RpfEntry entry in rpffile.AllEntries)
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".ydr"))
                            YdrDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".ydd"))
                            YddDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".ytd"))
                            YtdDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".yft"))
                            YftDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".ycd"))
                            YcdDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".yed")) YedDict[entry.ShortNameHash] = fentry;
                    }
            }
        }

        private void InitMapDicts()
        {
            YmapDict = new Dictionary<uint, RpfFileEntry>();
            YbnDict = new Dictionary<uint, RpfFileEntry>();
            YnvDict = new Dictionary<uint, RpfFileEntry>();
            foreach (RpfFile rpffile in ActiveMapRpfFiles.Values) //RpfMan.BaseRpfs)
            {
                if (rpffile.AllEntries == null) continue;
                foreach (RpfEntry entry in rpffile.AllEntries)
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".ymap"))
                            //YmapDict[entry.NameHash] = fentry;
                            YmapDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".ybn"))
                            //YbnDict[entry.NameHash] = fentry;
                            YbnDict[entry.ShortNameHash] = fentry;
                        else if (entry.NameLower.EndsWith(".ynv")) YnvDict[entry.ShortNameHash] = fentry;
                    }
            }

            AllYmapsDict = new Dictionary<uint, RpfFileEntry>();
            foreach (RpfFile rpffile in AllRpfs)
            {
                if (rpffile.AllEntries == null) continue;
                foreach (RpfEntry entry in rpffile.AllEntries)
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".ymap")) AllYmapsDict[entry.ShortNameHash] = fentry;
                    }
            }
        }

        private void InitManifestDicts()
        {
            AllManifests = new List<YmfFile>();
            hdtexturelookup = new Dictionary<MetaHash, MetaHash>();
            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;
            foreach (RpfFile file in rpfs)
            {
                if (file.AllEntries == null) continue;
                //manifest and meta parsing..
                foreach (RpfEntry entry in file.AllEntries)
                    //sp_manifest.ymt
                    //if (entry.NameLower.EndsWith("zonebind.ymt")/* || entry.Name.EndsWith("vinewood.ymt")*/)
                    //{
                    //    YmtFile ymt = GetFile<YmtFile>(entry);
                    //}
                    if (entry.Name.EndsWith(".ymf")) // || entry.Name.EndsWith(".ymt"))
                        try
                        {
                            UpdateStatus(entry.Path);
                            YmfFile ymffile = RpfMan.GetFile<YmfFile>(entry);
                            if (ymffile != null)
                            {
                                AllManifests.Add(ymffile);

                                if (ymffile.Pso != null)
                                {
                                }
                                else if (ymffile.Rbf != null)
                                {
                                }
                                else if (ymffile.Meta != null)
                                {
                                }


                                if (ymffile.HDTxdAssetBindings != null)
                                    for (int i = 0; i < ymffile.HDTxdAssetBindings.Length; i++)
                                    {
                                        CHDTxdAssetBinding b = ymffile.HDTxdAssetBindings[i];
                                        uint targetasset =
                                            JenkHash.GenHash(b.targetAsset.ToString().ToLowerInvariant());
                                        uint hdtxd = JenkHash.GenHash(b.HDTxd.ToString().ToLowerInvariant());
                                        hdtexturelookup[targetasset] = hdtxd;
                                    }
                            }
                        }
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex;
                            ErrorLog(errstr);
                        }
            }
        }

        private void InitGtxds()
        {
            Dictionary<MetaHash, MetaHash> parentTxds = new Dictionary<MetaHash, MetaHash>();

            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;

            Action<Dictionary<string, string>> addTxdRelationships = from =>
            {
                foreach (KeyValuePair<string, string> kvp in from)
                {
                    uint chash = JenkHash.GenHash(kvp.Key.ToLowerInvariant());
                    uint phash = JenkHash.GenHash(kvp.Value.ToLowerInvariant());
                    if (!parentTxds.ContainsKey(chash)) parentTxds.Add(chash, phash);
                }
            };

            Action<IEnumerable<RpfFile>> addRpfTxdRelationships = from =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                        try
                        {
                            if (entry.NameLower == "gtxd.ymt" || entry.NameLower == "gtxd.meta" ||
                                entry.NameLower == "mph4_gtxd.ymt")
                            {
                                GtxdFile ymt = RpfMan.GetFile<GtxdFile>(entry);
                                if (ymt.TxdRelationships != null) addTxdRelationships(ymt.TxdRelationships);
                            }
                            else if (entry.NameLower == "vehicles.meta")
                            {
                                VehiclesFile vf = RpfMan.GetFile<VehiclesFile>(entry); //could also get loaded in InitVehicles...
                                if (vf.TxdRelationships != null) addTxdRelationships(vf.TxdRelationships);
                            }
                        }
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex;
                            ErrorLog(errstr);
                        }
                }
            };


            addRpfTxdRelationships(rpfs);


            if (EnableDlc) addRpfTxdRelationships(DlcActiveRpfs);


            textureParents = parentTxds;


            //ensure resident global texture dicts:
            YtdFile ytd1 = new YtdFile(GetYtdEntry(JenkHash.GenHash("mapdetail")));
            LoadFile(ytd1);
            AddTextureLookups(ytd1);

            YtdFile ytd2 = new YtdFile(GetYtdEntry(JenkHash.GenHash("vehshare")));
            LoadFile(ytd2);
            AddTextureLookups(ytd2);
        }

        private void InitMapCaches()
        {
            AllCacheFiles = new List<CacheDatFile>();
            YmapHierarchyDict = new Dictionary<uint, MapDataStoreNode>();


            CacheDatFile loadCacheFile(string path, bool finalAttempt)
            {
                try
                {
                    CacheDatFile cache = RpfMan.GetFile<CacheDatFile>(path);
                    if (cache != null)
                    {
                        AllCacheFiles.Add(cache);
                        foreach (MapDataStoreNode node in cache.AllMapNodes)
                            if (YmapDict.ContainsKey(node.Name))
                                YmapHierarchyDict[node.Name] = node;
                        //ymap not found...
                    }
                    else if (finalAttempt)
                    {
                        ErrorLog(path +
                                 ": main cachefile not loaded! Possibly an unsupported GTAV installation version.");
                    } //update\x64\dlcpacks\mpspecialraces\dlc.rpf\x64\data\cacheloaderdata_dlc\mpspecialraces_3336915258_cache_y.dat (hash of: mpspecialraces_interior_additions)

                    return cache;
                }
                catch (Exception ex)
                {
                    ErrorLog(path + ": " + ex);
                }

                return null;
            }


            CacheDatFile maincache = null;
            if (EnableDlc)
            {
                maincache = loadCacheFile("update\\update.rpf\\common\\data\\gta5_cache_y.dat", false);
                if (maincache == null)
                    maincache = loadCacheFile("update\\update2.rpf\\common\\data\\gta5_cache_y.dat", true);
            }
            else
            {
                maincache = loadCacheFile("common.rpf\\data\\gta5_cache_y.dat", true);
            }


            if (EnableDlc)
                foreach (string dlccachefile in DlcCacheFileList)
                    loadCacheFile(dlccachefile, false);
        }

        private void InitArchetypeDicts()
        {
            YtypDict = new Dictionary<uint, YtypFile>();

            archetypesLoaded = false;
            archetypeDict.Clear();

            if (!LoadArchetypes) return;


            List<RpfFile> rpfs = EnableDlc ? AllRpfs : BaseRpfs;

            foreach (RpfFile file in rpfs) //RpfMan.BaseRpfs)RpfMan.AllRpfs)//ActiveMapRpfFiles.Values) // 
            {
                if (file.AllEntries == null) continue;
                if (!EnableDlc && file.Path.StartsWith("update")) continue;

                //parse ytyps
                foreach (RpfEntry entry in file.AllEntries)
                    try
                    {
                        if (entry.NameLower.EndsWith(".ytyp")) AddYtypToDictionary(entry);
                    }
                    catch (Exception ex)
                    {
                        ErrorLog(entry.Path + ": " + ex.Message);
                    }
            }

            archetypesLoaded = true;
        }

        private void AddYtypToDictionary(RpfEntry entry)
        {
            UpdateStatus(entry.Path);
            YtypFile ytypfile = RpfMan.GetFile<YtypFile>(entry);
            if (ytypfile == null)
                throw new Exception(
                    "Couldn't load ytyp file."); //couldn't load the file for some reason... shouldn't happen..
            if (ytypfile.Meta == null) throw new Exception("ytyp file was not in meta format.");
            if (YtypDict.ContainsKey(ytypfile.NameHash))
                //throw new Exception("ytyp " + JenkIndex.GetString(ytypfile.NameHash) + " already loaded.");
                //errorAction(entry.Path + ": ytyp " + JenkIndex.GetString(ytypfile.NameHash) + " already loaded.");
                YtypDict[ytypfile.NameHash] =
                    ytypfile; //override ytyp and continue anyway, could be unique archetypes in here still...
            else
                YtypDict.Add(ytypfile.NameHash, ytypfile);


            if (ytypfile.AllArchetypes == null || ytypfile.AllArchetypes.Length == 0)
                ErrorLog(entry.Path + ": no archetypes found");
            else
                foreach (Archetype arch in ytypfile.AllArchetypes)
                {
                    uint hash = arch.Hash;
                    if (hash == 0) continue;
                    if (archetypeDict.ContainsKey(hash))
                    {
                        Archetype oldval = archetypeDict[hash]; //replace old archetype?
                    }

                    archetypeDict[hash] = arch;
                }


            ////if (ytypfile.AudioEmitters != null)
            ////{
            ////    foreach (CExtensionDefAudioEmitter emitter in ytypfile.AudioEmitters)
            ////    {
            ////        //audioind++;
            ////        //uint hash = emitter.name;
            ////        //if (hash == 0) hash = archetype.name;
            ////        //if (hash == 0)
            ////        //    continue;
            ////        //if (AudioArchetypes.ContainsKey(hash))
            ////        //{
            ////        //    var oldval = AudioArchetypes[hash];
            ////        //    //errorAction(entry.Path + ": " + emitter.ToString() + ": (CTimeArchetypeDef) Already in archetype dict. Was in: " + oldval.ToString());
            ////        //    //overwrite with new definition? how to tell?
            ////        //    AudioArchetypes[hash] = new Tuple<YtypFile, int>(ytypfile, audioind);
            ////        //}
            ////        //else
            ////        //{
            ////        //    AudioArchetypes.Add(hash, new Tuple<YtypFile, int>(ytypfile, audioind));
            ////        //}
            ////    }
            ////}
        }

        public void InitStringDicts()
        {
            string langstr = "american_rel"; //todo: make this variable?
            string langstr2 = "americandlc.rpf";
            string langstr3 = "american.rpf";

            Gxt2Dict = new Dictionary<uint, RpfFileEntry>();
            List<Gxt2File> gxt2files = new List<Gxt2File>();
            foreach (RpfFile rpf in AllRpfs)
            foreach (RpfEntry entry in rpf.AllEntries)
                if (entry is RpfFileEntry fentry)
                {
                    string p = entry.Path;
                    if (entry.NameLower.EndsWith(".gxt2") &&
                        (p.Contains(langstr) || p.Contains(langstr2) || p.Contains(langstr3)))
                    {
                        Gxt2Dict[entry.ShortNameHash] = fentry;

                        if (DoFullStringIndex)
                        {
                            Gxt2File gxt2 = RpfMan.GetFile<Gxt2File>(entry);
                            if (gxt2 != null)
                            {
                                for (int i = 0; i < gxt2.TextEntries.Length; i++)
                                {
                                    Gxt2Entry e = gxt2.TextEntries[i];
                                    GlobalText.Ensure(e.Text, e.Hash);
                                }

                                gxt2files.Add(gxt2);
                            }
                        }
                    }
                }

            if (!DoFullStringIndex)
            {
                string globalgxt2path = "x64b.rpf\\data\\lang\\" + langstr + ".rpf\\global.gxt2";
                Gxt2File globalgxt2 = RpfMan.GetFile<Gxt2File>(globalgxt2path);
                if (globalgxt2 != null)
                    for (int i = 0; i < globalgxt2.TextEntries.Length; i++)
                    {
                        Gxt2Entry e = globalgxt2.TextEntries[i];
                        GlobalText.Ensure(e.Text, e.Hash);
                    }

                return;
            }


            GlobalText.FullIndexBuilt = true;


            foreach (RpfFile rpf in AllRpfs)
            foreach (RpfEntry entry in rpf.AllEntries)
                if (entry.NameLower.EndsWith("statssetup.xml"))
                {
                    XmlDocument xml = RpfMan.GetFileXml(entry.Path);
                    if (xml == null) continue;

                    XmlNodeList statnodes = xml.SelectNodes("StatsSetup/stats/stat");

                    foreach (XmlNode statnode in statnodes)
                    {
                        if (statnode == null) continue;
                        string statname = Xml.GetStringAttribute(statnode, "Name");
                        if (string.IsNullOrEmpty(statname)) continue;

                        string statnamel = statname.ToLowerInvariant();
                        StatsNames.Ensure(statname);
                        StatsNames.Ensure(statnamel);

                        StatsNames.Ensure("sp_" + statnamel);
                        StatsNames.Ensure("mp0_" + statnamel);
                        StatsNames.Ensure("mp1_" + statnamel);
                    }
                }

            StatsNames.FullIndexBuilt = true;
        }

        public void InitVehicles()
        {
            if (!LoadVehicles) return;


            //Neos7
            //Involved files(at least for rendering purpose )
            //Vehicles.meta
            //Carcols.meta
            //Carvariations.meta
            //Vehiclelayouts.meta
            //The other metas shouldn't be important for rendering
            //Then the global carcols.ymt is required too
            //As it contains the general shared tuning options
            //Carcols for modkits and lights kits definitions
            //Carvariations links such modkits and lights kits to each vehicle plus defines colours combinations of spawned vehicles
            //Vehiclelayouts mostly to handle ped interactions with the vehicle


            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;


            Dictionary<MetaHash, VehicleInitData> allVehicles = new Dictionary<MetaHash, VehicleInitData>();
            List<CarColsFile> allCarCols = new List<CarColsFile>();
            List<CarModColsFile> allCarModCols = new List<CarModColsFile>();
            List<CarVariationsFile> allCarVariations = new List<CarVariationsFile>();
            Dictionary<MetaHash, CVehicleModelInfoVariation_418053801> allCarVariationsDict =
                new Dictionary<MetaHash, CVehicleModelInfoVariation_418053801>();
            List<VehicleLayoutsFile> allVehicleLayouts = new List<VehicleLayoutsFile>();

            Action<IEnumerable<RpfFile>> addVehicleFiles = from =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
#if !DEBUG
                        try
#endif
                        {
                            if (entry.NameLower == "vehicles.meta")
                            {
                                VehiclesFile vf = RpfMan.GetFile<VehiclesFile>(entry);
                                if (vf.InitDatas != null)
                                    foreach (VehicleInitData initData in vf.InitDatas)
                                    {
                                        string name = initData.modelName.ToLowerInvariant();
                                        uint hash = JenkHash.GenHash(name);
                                        if (allVehicles.ContainsKey(hash))
                                        {
                                        }

                                        allVehicles[hash] = initData;
                                    }
                            }

                            if (entry.NameLower == "carcols.ymt" || entry.NameLower == "carcols.meta")
                            {
                                CarColsFile cf = RpfMan.GetFile<CarColsFile>(entry);
                                if (cf.VehicleModelInfo != null)
                                {
                                }

                                allCarCols.Add(cf);
                            }

                            if (entry.NameLower == "carmodcols.ymt")
                            {
                                CarModColsFile cf = RpfMan.GetFile<CarModColsFile>(entry);
                                if (cf.VehicleModColours != null)
                                {
                                }

                                allCarModCols.Add(cf);
                            }

                            if (entry.NameLower == "carvariations.ymt" || entry.NameLower == "carvariations.meta")
                            {
                                CarVariationsFile cf = RpfMan.GetFile<CarVariationsFile>(entry);
                                if (cf.VehicleModelInfo?.variationData != null)
                                    foreach (CVehicleModelInfoVariation_418053801 variation in cf.VehicleModelInfo.variationData)
                                    {
                                        string name = variation.modelName.ToLowerInvariant();
                                        uint hash = JenkHash.GenHash(name);
                                        allCarVariationsDict[hash] = variation;
                                    }

                                allCarVariations.Add(cf);
                            }

                            if (entry.NameLower.StartsWith("vehiclelayouts") && entry.NameLower.EndsWith(".meta"))
                            {
                                VehicleLayoutsFile lf = RpfMan.GetFile<VehicleLayoutsFile>(entry);
                                if (lf.Xml != null)
                                {
                                }

                                allVehicleLayouts.Add(lf);
                            }
                        }
#if !DEBUG
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
#endif
                    }
                }
            };


            addVehicleFiles(rpfs);

            if (EnableDlc) addVehicleFiles(DlcActiveRpfs);


            VehiclesInitDict = allVehicles;
        }

        public void InitPeds()
        {
            if (!LoadPeds) return;

            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;
            List<RpfFile> dlcrpfs = new List<RpfFile>();
            if (EnableDlc)
                foreach (RpfFile rpf in DlcActiveRpfs)
                {
                    dlcrpfs.Add(rpf);
                    if (rpf.Children == null) continue;
                    foreach (RpfFile crpf in rpf.Children)
                    {
                        dlcrpfs.Add(crpf);
                        if (crpf.Children?.Count > 0)
                        {
                        }
                    }
                }


            Dictionary<MetaHash, CPedModelInfo__InitData> allPeds = new Dictionary<MetaHash, CPedModelInfo__InitData>();
            List<PedsFile> allPedsFiles = new List<PedsFile>();
            Dictionary<MetaHash, PedFile> allPedYmts = new Dictionary<MetaHash, PedFile>();
            Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> allPedDrwDicts =
                new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();
            Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> allPedTexDicts =
                new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();
            Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> allPedClothDicts =
                new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();


            Dictionary<MetaHash, RpfFileEntry> ensureDict(Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> coll,
                MetaHash hash)
            {
                Dictionary<MetaHash, RpfFileEntry> dict;
                if (!coll.TryGetValue(hash, out dict))
                {
                    dict = new Dictionary<MetaHash, RpfFileEntry>();
                    coll[hash] = dict;
                }

                return dict;
            }

            Action<string, MetaHash, RpfDirectoryEntry> addPedDicts = (namel, hash, dir) =>
            {
                Dictionary<MetaHash, RpfFileEntry> dict = null;
                List<RpfFileEntry> files = dir?.Files;
                if (files != null)
                    foreach (RpfFileEntry file in files)
                        if (file.NameLower == namel + ".yld")
                        {
                            dict = ensureDict(allPedClothDicts, hash);
                            dict[file.ShortNameHash] = file;
                        }

                if (dir?.Directories != null)
                {
                    foreach (RpfDirectoryEntry cdir in dir.Directories)
                        if (cdir.NameLower == namel)
                        {
                            dir = cdir;
                            break;
                        }

                    files = dir?.Files;
                    if (files != null)
                        foreach (RpfFileEntry file in files)
                        {
                            if (file?.NameLower == null) continue;
                            if (file.NameLower.EndsWith(".ydd"))
                            {
                                dict = ensureDict(allPedDrwDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                            else if (file.NameLower.EndsWith(".ytd"))
                            {
                                dict = ensureDict(allPedTexDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                            else if (file.NameLower.EndsWith(".yld"))
                            {
                                dict = ensureDict(allPedClothDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                        }
                }
            };

            Action<IEnumerable<RpfFile>> addPedsFiles = from =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
#if !DEBUG
                        try
#endif
                        {
                            if (entry.NameLower == "peds.ymt" || entry.NameLower == "peds.meta")
                            {
                                PedsFile pf = RpfMan.GetFile<PedsFile>(entry);
                                if (pf.InitDataList?.InitDatas != null)
                                    foreach (CPedModelInfo__InitData initData in pf.InitDataList.InitDatas)
                                    {
                                        string name = initData.Name.ToLowerInvariant();
                                        uint hash = JenkHash.GenHash(name);
                                        if (allPeds.ContainsKey(hash))
                                        {
                                        }

                                        allPeds[hash] = initData;
                                    }

                                allPedsFiles.Add(pf);
                            }
                        }
#if !DEBUG
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
#endif
                    }
                }
            };

            Action<IEnumerable<RpfFile>> addPedFiles = from =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
#if !DEBUG
                        try
#endif
                        {
                            if (entry.NameLower.EndsWith(".ymt"))
                            {
                                string testname = entry.GetShortNameLower();
                                uint testhash = JenkHash.GenHash(testname);
                                if (allPeds.ContainsKey(testhash))
                                {
                                    PedFile pf = RpfMan.GetFile<PedFile>(entry);
                                    if (pf != null)
                                    {
                                        allPedYmts[testhash] = pf;
                                        addPedDicts(testname, testhash, entry.Parent);
                                    }
                                }
                            }
                        }
#if !DEBUG
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
#endif
                    }
                }
            };


            addPedsFiles(rpfs);
            addPedsFiles(dlcrpfs);

            addPedFiles(rpfs);
            addPedFiles(dlcrpfs);


            PedsInitDict = allPeds;
            PedVariationsDict = allPedYmts;
            PedDrawableDicts = allPedDrwDicts;
            PedTextureDicts = allPedTexDicts;
            PedClothDicts = allPedClothDicts;


            foreach (KeyValuePair<MetaHash, CPedModelInfo__InitData> kvp in PedsInitDict)
                if (!PedVariationsDict.ContainsKey(kvp.Key))
                {
                } //checking we found them all!
        }

        public void InitAudio()
        {
            if (!LoadAudio) return;

            Dictionary<uint, RpfFileEntry> datrelentries = new Dictionary<uint, RpfFileEntry>();

            void addRpfDatRelEntries(RpfFile rpffile)
            {
                if (rpffile.AllEntries == null) return;
                foreach (RpfEntry entry in rpffile.AllEntries)
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".rel")) datrelentries[entry.NameHash] = fentry;
                    }
            }

            RpfFile audrpf = RpfMan.FindRpfFile("x64\\audio\\audio_rel.rpf");
            if (audrpf != null) addRpfDatRelEntries(audrpf);

            if (EnableDlc)
            {
                RpfFile updrpf = RpfMan.FindRpfFile("update\\update.rpf");
                if (updrpf != null) addRpfDatRelEntries(updrpf);
                foreach (RpfFile dlcrpf in DlcActiveRpfs) //load from current dlc rpfs
                    addRpfDatRelEntries(dlcrpf);
                if (DlcActiveRpfs.Count == 0) //when activated from RPF explorer... DLCs aren't initialised fully
                    foreach (RpfFile rpf in
                             AllRpfs) //this is a bit of a hack - DLC orders won't be correct so likely will select wrong versions of things
                        if (rpf.NameLower.StartsWith("dlc"))
                            addRpfDatRelEntries(rpf);
            }


            List<RelFile> audioDatRelFiles = new List<RelFile>();
            Dictionary<MetaHash, RelData> audioConfigDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioSpeechDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioSynthsDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioMixersDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioCurvesDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioCategsDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioSoundsDict = new Dictionary<MetaHash, RelData>();
            Dictionary<MetaHash, RelData> audioGameDict = new Dictionary<MetaHash, RelData>();


            foreach (RpfFileEntry datrelentry in datrelentries.Values)
            {
                RelFile relfile = RpfMan.GetFile<RelFile>(datrelentry);
                if (relfile == null) continue;

                audioDatRelFiles.Add(relfile);

                Dictionary<MetaHash, RelData> d = audioGameDict;
                RelDatFileType t = relfile.RelType;
                switch (t)
                {
                    case RelDatFileType.Dat4:
                        d = relfile.IsAudioConfig ? audioConfigDict : audioSpeechDict;
                        break;
                    case RelDatFileType.Dat10ModularSynth:
                        d = audioSynthsDict;
                        break;
                    case RelDatFileType.Dat15DynamicMixer:
                        d = audioMixersDict;
                        break;
                    case RelDatFileType.Dat16Curves:
                        d = audioCurvesDict;
                        break;
                    case RelDatFileType.Dat22Categories:
                        d = audioCategsDict;
                        break;
                    case RelDatFileType.Dat54DataEntries:
                        d = audioSoundsDict;
                        break;
                    case RelDatFileType.Dat149:
                    case RelDatFileType.Dat150:
                    case RelDatFileType.Dat151:
                    default:
                        d = audioGameDict;
                        break;
                }

                foreach (RelData reldata in relfile.RelDatas)
                {
                    if (reldata.NameHash == 0) continue;
                    //if (d.TryGetValue(reldata.NameHash, out var exdata) && (exdata.TypeID != reldata.TypeID))
                    //{ }//sanity check
                    d[reldata.NameHash] = reldata;
                }
            }


            AudioDatRelFiles = audioDatRelFiles;
            AudioConfigDict = audioConfigDict;
            AudioSpeechDict = audioSpeechDict;
            AudioSynthsDict = audioSynthsDict;
            AudioMixersDict = audioMixersDict;
            AudioCurvesDict = audioCurvesDict;
            AudioCategsDict = audioCategsDict;
            AudioSoundsDict = audioSoundsDict;
            AudioGameDict = audioGameDict;
        }


        public bool SetDlcLevel(string dlc, bool enable)
        {
            bool dlcchange = dlc != SelectedDlc;
            bool enablechange = enable != EnableDlc;
            bool change = (dlcchange && enable) || enablechange;

            if (change)
                lock (updateSyncRoot)
                {
                    //lock (textureSyncRoot)
                    {
                        SelectedDlc = dlc;
                        EnableDlc = enable;

                        //mainCache.Clear();
                        ClearCachedMaps();

                        InitDlc();
                    }
                }

            return change;
        }

        public bool SetModsEnabled(bool enable)
        {
            bool change = enable != EnableMods;

            if (change)
                lock (updateSyncRoot)
                {
                    //lock (textureSyncRoot)
                    {
                        EnableMods = enable;
                        RpfMan.EnableMods = enable;

                        mainCache.Clear();

                        InitGlobal();
                        InitDlc();
                    }
                }

            return change;
        }


        private void ClearCachedMaps()
        {
            if (AllYmapsDict != null)
                foreach (RpfFileEntry ymap in AllYmapsDict.Values)
                {
                    GameFileCacheKey k = new GameFileCacheKey(ymap.ShortNameHash, GameFileType.Ymap);
                    mainCache.Remove(k);
                }
        }


        public void AddProjectFile(GameFile f)
        {
            if (f == null) return;
            if (f.RpfFileEntry == null) return;
            if (f.RpfFileEntry.ShortNameHash == 0)
                f.RpfFileEntry.ShortNameHash = JenkHash.GenHash(f.RpfFileEntry.GetShortNameLower());
            GameFileCacheKey key = new GameFileCacheKey(f.RpfFileEntry.ShortNameHash, f.Type);
            lock (requestSyncRoot)
            {
                projectFiles[key] = f;
            }
        }

        public void RemoveProjectFile(GameFile f)
        {
            if (f == null) return;
            if (f.RpfFileEntry == null) return;
            if (f.RpfFileEntry.ShortNameHash == 0) return;
            GameFileCacheKey key = new GameFileCacheKey(f.RpfFileEntry.ShortNameHash, f.Type);
            lock (requestSyncRoot)
            {
                projectFiles.Remove(key);
            }
        }

        public void ClearProjectFiles()
        {
            lock (requestSyncRoot)
            {
                projectFiles.Clear();
            }
        }

        public void AddProjectArchetype(Archetype a)
        {
            if ((a?.Hash ?? 0) == 0) return;
            lock (requestSyncRoot)
            {
                projectArchetypes[a.Hash] = a;
            }
        }

        public void RemoveProjectArchetype(Archetype a)
        {
            if ((a?.Hash ?? 0) == 0) return;
            Archetype tarch = null;
            lock (requestSyncRoot)
            {
                projectArchetypes.TryGetValue(a.Hash, out tarch);
                if (tarch == a) projectArchetypes.Remove(a.Hash);
            }
        }

        public void ClearProjectArchetypes()
        {
            lock (requestSyncRoot)
            {
                projectArchetypes.Clear();
            }
        }

        public void TryLoadEnqueue(GameFile gf)
        {
            if (!gf.Loaded && requestQueue.Count < 10) // && (!gf.LoadQueued)
            {
                requestQueue.Enqueue(gf);
                gf.LoadQueued = true;
            }
        }


        public Archetype GetArchetype(uint hash)
        {
            if (!archetypesLoaded) return null;
            Archetype arch = null;
            projectArchetypes.TryGetValue(hash, out arch);
            if (arch != null) return arch;
            archetypeDict.TryGetValue(hash, out arch);
            return arch;
        }

        public MapDataStoreNode GetMapNode(uint hash)
        {
            if (!IsInited) return null;
            MapDataStoreNode node = null;
            YmapHierarchyDict.TryGetValue(hash, out node);
            return node;
        }

        public YdrFile GetYdr(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ydr);
                if (projectFiles.TryGetValue(key, out GameFile pgf)) return pgf as YdrFile;
                YdrFile ydr = mainCache.TryGet(key) as YdrFile;
                if (ydr == null)
                {
                    RpfFileEntry e = GetYdrEntry(hash);
                    if (e != null)
                    {
                        ydr = new YdrFile(e);
                        if (mainCache.TryAdd(key, ydr))
                            TryLoadEnqueue(ydr);
                        else
                            ydr.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load drawable: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Drawable not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ydr.Loaded)
                {
                    TryLoadEnqueue(ydr);
                }

                return ydr;
            }
        }

        public YddFile GetYdd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ydd);
                if (projectFiles.TryGetValue(key, out GameFile pgf)) return pgf as YddFile;
                YddFile ydd = mainCache.TryGet(key) as YddFile;
                if (ydd == null)
                {
                    RpfFileEntry e = GetYddEntry(hash);
                    if (e != null)
                    {
                        ydd = new YddFile(e);
                        if (mainCache.TryAdd(key, ydd))
                            TryLoadEnqueue(ydd);
                        else
                            ydd.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load drawable dictionary: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Drawable dictionary not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ydd.Loaded)
                {
                    TryLoadEnqueue(ydd);
                }

                return ydd;
            }
        }

        public YtdFile GetYtd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ytd);
                if (projectFiles.TryGetValue(key, out GameFile pgf)) return pgf as YtdFile;
                YtdFile ytd = mainCache.TryGet(key) as YtdFile;
                if (ytd == null)
                {
                    RpfFileEntry e = GetYtdEntry(hash);
                    if (e != null)
                    {
                        ytd = new YtdFile(e);
                        if (mainCache.TryAdd(key, ytd))
                            TryLoadEnqueue(ytd);
                        else
                            ytd.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load texture dictionary: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Texture dictionary not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ytd.Loaded)
                {
                    TryLoadEnqueue(ytd);
                }

                return ytd;
            }
        }

        public YmapFile GetYmap(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ymap);
                YmapFile ymap = mainCache.TryGet(key) as YmapFile;
                if (ymap == null)
                {
                    RpfFileEntry e = GetYmapEntry(hash);
                    if (e != null)
                    {
                        ymap = new YmapFile(e);
                        if (mainCache.TryAdd(key, ymap))
                            TryLoadEnqueue(ymap);
                        else
                            ymap.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load ymap: " + JenkIndex.GetString(hash));
                    }
                    //ErrorLog("Ymap not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ymap.Loaded)
                {
                    TryLoadEnqueue(ymap);
                }

                return ymap;
            }
        }

        public YftFile GetYft(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Yft);
                YftFile yft = mainCache.TryGet(key) as YftFile;
                if (projectFiles.TryGetValue(key, out GameFile pgf)) return pgf as YftFile;
                if (yft == null)
                {
                    RpfFileEntry e = GetYftEntry(hash);
                    if (e != null)
                    {
                        yft = new YftFile(e);
                        if (mainCache.TryAdd(key, yft))
                            TryLoadEnqueue(yft);
                        else
                            yft.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load yft: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Yft not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!yft.Loaded)
                {
                    TryLoadEnqueue(yft);
                }

                return yft;
            }
        }

        public YbnFile GetYbn(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ybn);
                YbnFile ybn = mainCache.TryGet(key) as YbnFile;
                if (ybn == null)
                {
                    RpfFileEntry e = GetYbnEntry(hash);
                    if (e != null)
                    {
                        ybn = new YbnFile(e);
                        if (mainCache.TryAdd(key, ybn))
                            TryLoadEnqueue(ybn);
                        else
                            ybn.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load ybn: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Ybn not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ybn.Loaded)
                {
                    TryLoadEnqueue(ybn);
                }

                return ybn;
            }
        }

        public YcdFile GetYcd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ycd);
                YcdFile ycd = mainCache.TryGet(key) as YcdFile;
                if (ycd == null)
                {
                    RpfFileEntry e = GetYcdEntry(hash);
                    if (e != null)
                    {
                        ycd = new YcdFile(e);
                        if (mainCache.TryAdd(key, ycd))
                            TryLoadEnqueue(ycd);
                        else
                            ycd.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load ycd: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Ycd not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ycd.Loaded)
                {
                    TryLoadEnqueue(ycd);
                }

                return ycd;
            }
        }

        public YedFile GetYed(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Yed);
                YedFile yed = mainCache.TryGet(key) as YedFile;
                if (yed == null)
                {
                    RpfFileEntry e = GetYedEntry(hash);
                    if (e != null)
                    {
                        yed = new YedFile(e);
                        if (mainCache.TryAdd(key, yed))
                            TryLoadEnqueue(yed);
                        else
                            yed.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load yed: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Yed not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!yed.Loaded)
                {
                    TryLoadEnqueue(yed);
                }

                return yed;
            }
        }

        public YnvFile GetYnv(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                GameFileCacheKey key = new GameFileCacheKey(hash, GameFileType.Ynv);
                YnvFile ynv = mainCache.TryGet(key) as YnvFile;
                if (ynv == null)
                {
                    RpfFileEntry e = GetYnvEntry(hash);
                    if (e != null)
                    {
                        ynv = new YnvFile(e);
                        if (mainCache.TryAdd(key, ynv))
                            TryLoadEnqueue(ynv);
                        else
                            ynv.LoadQueued = false;
                        //ErrorLog("Out of cache space - couldn't load ycd: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                    //ErrorLog("Ycd not found: " + JenkIndex.GetString(hash)); //too spammy...
                }
                else if (!ynv.Loaded)
                {
                    TryLoadEnqueue(ynv);
                }

                return ynv;
            }
        }


        public RpfFileEntry GetYdrEntry(uint hash)
        {
            RpfFileEntry entry;
            YdrDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYddEntry(uint hash)
        {
            RpfFileEntry entry;
            YddDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYtdEntry(uint hash)
        {
            RpfFileEntry entry;
            YtdDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYmapEntry(uint hash)
        {
            RpfFileEntry entry;
            if (!YmapDict.TryGetValue(hash, out entry)) AllYmapsDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYftEntry(uint hash)
        {
            RpfFileEntry entry;
            YftDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYbnEntry(uint hash)
        {
            RpfFileEntry entry;
            YbnDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYcdEntry(uint hash)
        {
            RpfFileEntry entry;
            YcdDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYedEntry(uint hash)
        {
            RpfFileEntry entry;
            YedDict.TryGetValue(hash, out entry);
            return entry;
        }

        public RpfFileEntry GetYnvEntry(uint hash)
        {
            RpfFileEntry entry;
            YnvDict.TryGetValue(hash, out entry);
            return entry;
        }


        public bool LoadFile<T>(T file) where T : GameFile, PackedFile
        {
            if (file == null) return false;
            RpfFileEntry entry = file.RpfFileEntry;
            if (entry != null) return RpfMan.LoadFile(file, entry);
            return false;
        }


        public T GetFileUncached<T>(RpfFileEntry e) where T : GameFile, new()
        {
            T f = new T();
            f.RpfFileEntry = e;
            TryLoadEnqueue(f);
            return f;
        }


        public void BeginFrame()
        {
            lock (requestSyncRoot)
            {
                mainCache.BeginFrame();
            }
        }


        public bool ContentThreadProc()
        {
            Monitor.Enter(updateSyncRoot);

            GameFile req;
            //bool loadedsomething = false;

            int itemcount = 0;

            while (requestQueue.TryDequeue(out req) && itemcount < MaxItemsPerLoop)
            {
                //process content requests.
                if (req.Loaded)
                    continue; //it's already loaded... (somehow)

                if ((req.LastUseTime - DateTime.Now).TotalSeconds > 0.5)
                    continue; //hasn't been requested lately..! ignore, will try again later if necessary

                itemcount++;
                //if (!loadedsomething)
                //{
                //UpdateStatus("Loading " + req.RpfFileEntry.Name + "...");
                //}

#if !DEBUG
                try
                {
#endif

                switch (req.Type)
                {
                    case GameFileType.Ydr:
                        req.Loaded = LoadFile(req as YdrFile);
                        break;
                    case GameFileType.Ydd:
                        req.Loaded = LoadFile(req as YddFile);
                        break;
                    case GameFileType.Ytd:
                        req.Loaded = LoadFile(req as YtdFile);
                        //if (req.Loaded) AddTextureLookups(req as YtdFile);
                        break;
                    case GameFileType.Ymap:
                        YmapFile y = req as YmapFile;
                        req.Loaded = LoadFile(y);
                        if (req.Loaded) y.InitYmapEntityArchetypes(this);
                        break;
                    case GameFileType.Yft:
                        req.Loaded = LoadFile(req as YftFile);
                        break;
                    case GameFileType.Ybn:
                        req.Loaded = LoadFile(req as YbnFile);
                        break;
                    case GameFileType.Ycd:
                        req.Loaded = LoadFile(req as YcdFile);
                        break;
                    case GameFileType.Yed:
                        req.Loaded = LoadFile(req as YedFile);
                        break;
                    case GameFileType.Ynv:
                        req.Loaded = LoadFile(req as YnvFile);
                        break;
                    case GameFileType.Yld:
                        req.Loaded = LoadFile(req as YldFile);
                        break;
                }

                UpdateStatus((req.Loaded ? "Loaded " : "Error loading ") + req);

                if (!req.Loaded) ErrorLog("Error loading " + req);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    ErrorLog($"Failed to load file {req.Name}: {ex.Message}");
                    //TODO: try to stop subsequent attempts to load this!
                }
#endif

                //loadedsomething = true;
            }

            //whether or not we need another content thread loop
            bool itemsStillPending = itemcount >= MaxItemsPerLoop;


            Monitor.Exit(updateSyncRoot);


            return itemsStillPending;
        }


        private void AddTextureLookups(YtdFile ytd)
        {
            if (ytd?.TextureDict?.TextureNameHashes?.data_items == null) return;

            lock (textureSyncRoot)
            {
                foreach (uint hash in ytd.TextureDict.TextureNameHashes.data_items)
                    textureLookup[hash] = ytd.RpfFileEntry;
            }
        }

        public YtdFile TryGetTextureDictForTexture(uint hash)
        {
            lock (textureSyncRoot)
            {
                RpfFileEntry e;
                if (textureLookup.TryGetValue(hash, out e)) return GetYtd(e.ShortNameHash);
            }

            return null;
        }

        public YtdFile TryGetParentYtd(uint hash)
        {
            MetaHash phash;
            if (textureParents.TryGetValue(hash, out phash)) return GetYtd(phash);
            return null;
        }

        public uint TryGetParentYtdHash(uint hash)
        {
            MetaHash phash = 0;
            textureParents.TryGetValue(hash, out phash);
            return phash;
        }

        public uint TryGetHDTextureHash(uint txdhash)
        {
            MetaHash hdhash = 0;
            if (hdtexturelookup?.TryGetValue(txdhash, out hdhash) ?? false) return hdhash;
            return txdhash;
        }

        public Texture TryFindTextureInParent(uint texhash, uint txdhash)
        {
            Texture tex = null;

            YtdFile ytd = TryGetParentYtd(txdhash);
            while (ytd != null && tex == null)
            {
                if (ytd.Loaded && ytd.TextureDict != null) tex = ytd.TextureDict.Lookup(texhash);
                if (tex == null) ytd = TryGetParentYtd(ytd.Key.Hash);
            }

            return tex;
        }


        public DrawableBase TryGetDrawable(Archetype arche)
        {
            if (arche == null) return null;
            uint drawhash = arche.Hash;
            DrawableBase drawable = null;
            if (arche.DrawableDict != 0) // && (arche.DrawableDict != arche.Hash))
            {
                //try get drawable from ydd...
                YddFile ydd = GetYdd(arche.DrawableDict);
                if (ydd != null)
                {
                    if (ydd.Loaded && ydd.Dict != null)
                    {
                        Drawable d;
                        ydd.Dict.TryGetValue(drawhash, out d); //can't out to base class?
                        drawable = d;
                        if (drawable == null) return null; //drawable wasn't in dict!!
                    }
                    else
                    {
                        return null; //ydd not loaded yet, or has no dict
                    }
                }
                //return null; //couldn't find drawable dict... quit now?
            }

            if (drawable == null)
            {
                //try get drawable from ydr.
                YdrFile ydr = GetYdr(drawhash);
                if (ydr != null)
                {
                    if (ydr.Loaded) drawable = ydr.Drawable;
                }
                else
                {
                    YftFile yft = GetYft(drawhash);
                    if (yft != null)
                        if (yft.Loaded)
                            if (yft.Fragment != null)
                                drawable = yft.Fragment.Drawable;
                }
            }

            return drawable;
        }

        public async Task<(DrawableBase drawable, bool waitingForLoad)> TryGetDrawableAsync(Archetype arche)
        {
            bool waitingForLoad = false;
            if (arche == null) return (null, false);

            uint drawhash = arche.Hash;
            DrawableBase drawable = null;

            // Run Ydd, Ydr, and Yft parts in parallel
            Task yddTask = Task.Run(() => TryGetDrawableFromYdd(arche, drawhash, ref drawable));
            Task ydrTask = Task.Run(() => TryGetDrawableFromYdr(drawhash, ref drawable));
            Task yftTask = Task.Run(() => TryGetDrawableFromYft(drawhash, ref drawable));

            // Wait for any of them to complete
            await Task.WhenAny(yddTask, ydrTask, yftTask);

            // After any task completes, check if drawable is loaded or if we are still waiting
            if (drawable != null) return (drawable, waitingForLoad);

            // If all tasks are still running, check their load status
            if (yddTask.Status != TaskStatus.RanToCompletion || ydrTask.Status != TaskStatus.RanToCompletion ||
                yftTask.Status != TaskStatus.RanToCompletion)
            {
                waitingForLoad = true;
                return (null, true); // Not ready yet, return early
            }

            return (drawable, waitingForLoad);
        }

        // Helper functions for Ydd, Ydr, and Yft

        private void TryGetDrawableFromYdd(Archetype arche, uint drawhash, ref DrawableBase drawable)
        {
            if (arche.DrawableDict != 0)
            {
                YddFile ydd = GetYdd(arche.DrawableDict);
                if (ydd != null && ydd.Loaded && ydd.Dict != null && ydd.Dict.TryGetValue(drawhash, out Drawable d))
                    drawable = d;
            }
        }

        private void TryGetDrawableFromYdr(uint drawhash, ref DrawableBase drawable)
        {
            if (drawable == null)
            {
                YdrFile ydr = GetYdr(drawhash);
                if (ydr != null && ydr.Loaded) drawable = ydr.Drawable;
            }
        }

        private void TryGetDrawableFromYft(uint drawhash, ref DrawableBase drawable)
        {
            if (drawable == null)
            {
                YftFile yft = GetYft(drawhash);
                if (yft != null && yft.Loaded) drawable = yft.Fragment?.Drawable;
            }
        }


        private string[] GetExcludePaths()
        {
            string[] exclpaths = ExcludeFolders.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (exclpaths.Length > 0)
                for (int i = 0; i < exclpaths.Length; i++)
                    exclpaths[i] = exclpaths[i].ToLowerInvariant();
            else
                exclpaths = null;

            return exclpaths;
        }


        public void TestAudioRels()
        {
            UpdateStatus("Testing Audio REL files");


            bool savetest = true;
            bool xmltest = true;
            bool asmtest = true;

            foreach (RpfFile rpf in RpfMan.AllRpfs)
            foreach (RpfEntry entry in rpf.AllEntries)
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                RpfBinaryFileEntry rbfe = rfe as RpfBinaryFileEntry;
                if (rfe == null || rbfe == null) continue;

                if (rfe.NameLower.EndsWith(".rel"))
                {
                    UpdateStatus(entry.Path);

                    RelFile rel = new RelFile(rfe);
                    RpfMan.LoadFile(rel, rfe);


                    byte[] data;

                    if (savetest)
                    {
                        data = rel.Save();
                        if (data != null)
                        {
                            if (data.Length != rbfe.FileUncompressedSize)
                            {
                            }
                            else if (data.Length != rel.RawFileData.Length)
                            {
                            }
                            else
                            {
                                for (int i = 0; i < data.Length; i++) //raw file test
                                    if (data[i] != rel.RawFileData[i])
                                        break;
                            }


                            RelFile rel2 = new RelFile();
                            rel2.Load(data, rfe); //roundtrip test

                            if (rel2.IndexCount != rel.IndexCount)
                            {
                            }

                            if (rel2.RelDatas == null)
                            {
                            }
                        }
                    }

                    if (xmltest)
                    {
                        string relxml = RelXml.GetXml(rel); //XML test...
                        RelFile rel3 = XmlRel.GetRel(relxml);
                        if (rel3 != null)
                        {
                            if (rel3.RelDatasSorted?.Length != rel.RelDatasSorted?.Length)
                            {
                            } //check nothing went missing...


                            data = rel3.Save(); //full roundtrip!
                            if (data != null)
                            {
                                RelFile rel4 = new RelFile();
                                rel4.Load(data, rfe); //insanity check

                                if (data.Length != rbfe.FileUncompressedSize)
                                {
                                }
                                else if (data.Length != rel.RawFileData.Length)
                                {
                                }
                                else
                                {
                                    for (int i = 0; i < data.Length; i++) //raw file test
                                        if (data[i] != rel.RawFileData[i])
                                            break;
                                }

                                string relxml2 = RelXml.GetXml(rel4); //full insanity
                                if (relxml2.Length != relxml.Length)
                                {
                                }

                                if (relxml2 != relxml)
                                {
                                }
                            }
                        }
                    }

                    if (asmtest)
                        if (rel.RelType == RelDatFileType.Dat10ModularSynth)
                            foreach (RelData d in rel.RelDatasSorted)
                                if (d is Dat10Synth synth)
                                    synth.TestDisassembly();
                }
            }


            Dictionary<RelFile.HashesMapKey, List<RelFile.HashesMapValue>> hashmap = RelFile.HashesMap;
            if (hashmap.Count > 0)
            {
            }


            StringBuilder sb2 = new StringBuilder();
            foreach (KeyValuePair<RelFile.HashesMapKey, List<RelFile.HashesMapValue>> kvp in hashmap)
            {
                string itemtype = kvp.Key.ItemType.ToString();
                if (kvp.Key.FileType == RelDatFileType.Dat151)
                    itemtype = ((Dat151RelType)kvp.Key.ItemType).ToString();
                else if (kvp.Key.FileType == RelDatFileType.Dat54DataEntries)
                    itemtype = ((Dat54SoundType)kvp.Key.ItemType).ToString();
                else
                    itemtype = kvp.Key.FileType + ".Unk" + kvp.Key.ItemType;
                if (kvp.Key.IsContainer) itemtype += " (container)";

                //if (kvp.Key.FileType == RelDatFileType.Dat151)
                {
                    sb2.Append(itemtype);
                    sb2.Append("     ");
                    foreach (RelFile.HashesMapValue val in kvp.Value)
                    {
                        sb2.Append(val);
                        sb2.Append("   ");
                    }

                    sb2.AppendLine();
                }
            }

            string hashmapstr = sb2.ToString();
            if (!string.IsNullOrEmpty(hashmapstr))
            {
            }
        }

        public void TestAudioYmts()
        {
            StringBuilder sb = new StringBuilder();

            Dictionary<uint, int> allids = new Dictionary<uint, int>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    string n = entry.NameLower;
                    if (n.EndsWith(".ymt"))
                    {
                        UpdateStatus(entry.Path);

                        //YmtFile ymtfile = RpfMan.GetFile<YmtFile>(entry);
                        //if ((ymtfile != null))
                        //{
                        //}
                        string sn = entry.GetShortName();
                        uint un;
                        if (uint.TryParse(sn, out un))
                        {
                            if (allids.ContainsKey(un))
                            {
                                allids[un] = allids[un] + 1;
                            }
                            else
                            {
                                allids[un] = 1;
                                //ushort s1 = (ushort)(un & 0x1FFFu);
                                //ushort s2 = (ushort)((un >> 13) & 0x1FFFu);
                                uint s1 = un % 80000;
                                uint s2 = un / 80000;
                                float f1 = s1 / 5000.0f;
                                float f2 = s2 / 5000.0f;
                                sb.AppendFormat("{0}, {1}, 0, {2}\r\n", f1, f2, sn);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }

            List<uint> skeys = allids.Keys.ToList();
            skeys.Sort();

            List<string> hkeys = new List<string>();
            foreach (uint skey in skeys)
            {
                FlagsUint fu = new FlagsUint(skey);
                //hkeys.Add(skey.ToString("X"));
                hkeys.Add(fu.Bin);
            }

            string nstr = string.Join("\r\n", hkeys.ToArray());
            string pstr = sb.ToString();
            if (pstr.Length > 0)
            {
            }
        }

        public void TestAudioAwcs()
        {
            StringBuilder sb = new StringBuilder();

            Dictionary<uint, int> allids = new Dictionary<uint, int>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
            {
                //try
                //{
                string n = entry.NameLower;
                if (n.EndsWith(".awc"))
                {
                    UpdateStatus(entry.Path);
                    AwcFile awcfile = RpfMan.GetFile<AwcFile>(entry);
                    if (awcfile != null)
                    {
                    }
                }
                //}
                //catch (Exception ex)
                //{
                //    UpdateStatus("Error! " + ex.ToString());
                //}
            }
        }

        public void TestMetas()
        {
            //find all RSC meta files and generate the MetaTypes init code

            MetaTypes.Clear();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                string n = entry.NameLower;


                //if (n.EndsWith(".ymap"))
                //{
                //    UpdateStatus(entry.Path);
                //    YmapFile ymapfile = RpfMan.GetFile<YmapFile>(entry);
                //    if ((ymapfile != null) && (ymapfile.Meta != null))
                //    {
                //        MetaTypes.EnsureMetaTypes(ymapfile.Meta);
                //    }
                //}
                //else if (n.EndsWith(".ytyp"))
                //{
                //    UpdateStatus(entry.Path);
                //    YtypFile ytypfile = RpfMan.GetFile<YtypFile>(entry);
                //    if ((ytypfile != null) && (ytypfile.Meta != null))
                //    {
                //        MetaTypes.EnsureMetaTypes(ytypfile.Meta);
                //    }
                //}
                //else if (n.EndsWith(".ymt"))
                //{
                //    UpdateStatus(entry.Path);
                //    YmtFile ymtfile = RpfMan.GetFile<YmtFile>(entry);
                //    if ((ymtfile != null) && (ymtfile.Meta != null))
                //    {
                //        MetaTypes.EnsureMetaTypes(ymtfile.Meta);
                //    }
                //}
                if (n.EndsWith(".ymap") || n.EndsWith(".ytyp") || n.EndsWith(".ymt"))
                {
                    RpfResourceFileEntry rfe = entry as RpfResourceFileEntry;
                    if (rfe == null) continue;

                    UpdateStatus(entry.Path);

                    byte[] data = rfe.File.ExtractFile(rfe);
                    ResourceDataReader rd = new ResourceDataReader(rfe, data);
                    Meta meta = rd.ReadBlock<Meta>();
                    string xml = MetaXml.GetXml(meta);
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.LoadXml(xml);
                    Meta meta2 = XmlMeta.GetMeta(xdoc);
                    string xml2 = MetaXml.GetXml(meta2);

                    if (xml.Length != xml2.Length)
                    {
                    }

                    if (xml != xml2 && !n.EndsWith("srl.ymt") && !n.StartsWith("des_"))
                    {
                    }
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            string str = MetaTypes.GetTypesInitString();
        }

        public void TestPsos()
        {
            //find all PSO meta files and generate the PsoTypes init code
            PsoTypes.Clear();

            List<Exception> exceptions = new List<Exception>();
            List<string> allpsos = new List<string>();
            List<string> diffpsos = new List<string>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                string n = entry.NameLower;
                if (!(n.EndsWith(".pso") ||
                      n.EndsWith(".ymt") ||
                      n.EndsWith(".ymf") ||
                      n.EndsWith(".ymap") ||
                      n.EndsWith(".ytyp") ||
                      n.EndsWith(".cut")))
                    continue; //PSO files seem to only have these extensions

                RpfFileEntry fentry = entry as RpfFileEntry;
                byte[] data = entry.File.ExtractFile(fentry);
                if (data != null)
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (PsoFile.IsPSO(ms))
                        {
                            UpdateStatus(entry.Path);

                            PsoFile pso = new PsoFile();
                            pso.Load(ms);

                            allpsos.Add(fentry.Path);

                            PsoTypes.EnsurePsoTypes(pso);

                            string xml = PsoXml.GetXml(pso);
                            if (!string.IsNullOrEmpty(xml))
                            {
                            }

                            XmlDocument xdoc = new XmlDocument();
                            xdoc.LoadXml(xml);
                            PsoFile pso2 = XmlPso.GetPso(xdoc);
                            byte[] pso2b = pso2.Save();

                            PsoFile pso3 = new PsoFile();
                            pso3.Load(pso2b);
                            string xml3 = PsoXml.GetXml(pso3);

                            if (xml.Length != xml3.Length)
                            {
                            }

                            if (xml != xml3) diffpsos.Add(fentry.Path);
                            //if (entry.NameLower == "clip_sets.ymt")
                            //{ }
                            //if (entry.NameLower == "vfxinteriorinfo.ymt")
                            //{ }
                            //if (entry.NameLower == "vfxvehicleinfo.ymt")
                            //{ }
                            //if (entry.NameLower == "vfxpedinfo.ymt")
                            //{ }
                            //if (entry.NameLower == "vfxregioninfo.ymt")
                            //{ }
                            //if (entry.NameLower == "vfxweaponinfo.ymt")
                            //{ }
                            //if (entry.NameLower == "physicstasks.ymt")
                            //{ }
                        }
                    }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            string allpsopaths = string.Join("\r\n", allpsos);
            string diffpsopaths = string.Join("\r\n", diffpsos);

            string str = PsoTypes.GetTypesInitString();
            if (!string.IsNullOrEmpty(str))
            {
            }
        }

        public void TestRbfs()
        {
            List<Exception> exceptions = new List<Exception>();
            List<string> allrbfs = new List<string>();
            List<string> diffrbfs = new List<string>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
            {
                string n = entry.NameLower;
                if (!(n.EndsWith(".ymt") ||
                      n.EndsWith(".ymf") ||
                      n.EndsWith(".ymap") ||
                      n.EndsWith(".ytyp") ||
                      n.EndsWith(".cut")))
                    continue; //PSO files seem to only have these extensions

                RpfFileEntry fentry = entry as RpfFileEntry;
                byte[] data = entry.File.ExtractFile(fentry);
                if (data != null)
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (RbfFile.IsRBF(ms))
                        {
                            UpdateStatus(entry.Path);

                            RbfFile rbf = new RbfFile();
                            rbf.Load(ms);

                            allrbfs.Add(fentry.Path);

                            string xml = RbfXml.GetXml(rbf);
                            if (!string.IsNullOrEmpty(xml))
                            {
                            }

                            XmlDocument xdoc = new XmlDocument();
                            xdoc.LoadXml(xml);
                            RbfFile rbf2 = XmlRbf.GetRbf(xdoc);
                            byte[] rbf2b = rbf2.Save();

                            RbfFile rbf3 = new RbfFile();
                            rbf3.Load(rbf2b);
                            string xml3 = RbfXml.GetXml(rbf3);

                            if (xml.Length != xml3.Length)
                            {
                            }

                            if (xml != xml3) diffrbfs.Add(fentry.Path);

                            if (data.Length != rbf2b.Length)
                            {
                                //File.WriteAllBytes("C:\\GitHub\\CodeWalkerResearch\\RBF\\" + fentry.Name + ".dat0", data);
                                //File.WriteAllBytes("C:\\GitHub\\CodeWalkerResearch\\RBF\\" + fentry.Name + ".dat1", rbf2b);
                            }
                            else
                            {
                                for (int i = 0; i < data.Length; i++)
                                    if (data[i] != rbf2b[i])
                                    {
                                        diffrbfs.Add(fentry.Path);
                                        break;
                                    }
                            }
                        }
                    }
            }

            string allrbfpaths = string.Join("\r\n", allrbfs);
            string diffrbfpaths = string.Join("\r\n", diffrbfs);
        }

        public void TestCuts()
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                if (rfe.NameLower.EndsWith(".cut"))
                {
                    UpdateStatus(entry.Path);

                    CutFile cut = new CutFile(rfe);
                    RpfMan.LoadFile(cut, rfe);
                    //PsoTypes.EnsurePsoTypes(cut.Pso);
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            string str = PsoTypes.GetTypesInitString();
            if (!string.IsNullOrEmpty(str))
            {
            }
        }

        public void TestYlds()
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                if (rfe.NameLower.EndsWith(".yld"))
                {
                    UpdateStatus(entry.Path);

                    YldFile yld = new YldFile(rfe);
                    RpfMan.LoadFile(yld, rfe);
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            if (exceptions.Count > 0)
            {
            }
        }

        public void TestYeds()
        {
            bool xmltest = true;
            List<Exception> exceptions = new List<Exception>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                if (rfe.NameLower.EndsWith(".yed"))
                {
                    UpdateStatus(entry.Path);

                    YedFile yed = new YedFile(rfe);
                    RpfMan.LoadFile(yed, rfe);

                    if (xmltest)
                    {
                        string xml = YedXml.GetXml(yed);
                        YedFile yed2 = XmlYed.GetYed(xml);
                        byte[] data2 = yed2.Save();
                        YedFile yed3 = new YedFile();
                        RpfFile.LoadResourceFile(yed3, data2, 25); //full roundtrip
                        string xml2 = YedXml.GetXml(yed3);
                        if (xml != xml2)
                        {
                        } //no hitting
                    }
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            if (exceptions.Count > 0)
            {
            }
        }

        public void TestYcds()
        {
            bool savetest = false;
            List<YcdFile> errorfiles = new List<YcdFile>();
            List<RpfEntry> errorentries = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                //{
                if (entry.NameLower.EndsWith(".ycd"))
                {
                    UpdateStatus(entry.Path);
                    YcdFile ycd1 = RpfMan.GetFile<YcdFile>(entry);
                    if (ycd1 == null)
                    {
                        errorentries.Add(entry);
                    }
                    else if (ycd1?.LoadException != null)
                    {
                        errorfiles.Add(ycd1); //these ones have file corruption issues and won't load as resource...
                    }
                    else if (savetest)
                    {
                        if (ycd1.ClipDictionary == null) continue;

                        //var data1 = ycd1.Save();
                        string xml = YcdXml.GetXml(ycd1);
                        YcdFile ycdX = XmlYcd.GetYcd(xml);
                        byte[] data = ycdX.Save();
                        YcdFile ycd2 = new YcdFile();
                        RpfFile.LoadResourceFile(ycd2, data, 46); //full roundtrip

                        {
                            if (ycd2 == null) continue;
                            if (ycd2.ClipDictionary == null) continue;

                            ClipMapEntry[] c1 = ycd1.ClipDictionary.Clips?.data_items;
                            ClipMapEntry[] c2 = ycd2.ClipDictionary.Clips?.data_items;
                            if (c1 == null || c2 == null) continue;
                            if (c1.Length != c2.Length) continue;

                            AnimationMapEntry[] a1 = ycd1.ClipDictionary.Animations?.Animations?.data_items;
                            AnimationMapEntry[] a2 = ycd2.ClipDictionary.Animations?.Animations?.data_items;
                            if (a1 == null || a2 == null) continue;
                            if (a1.Length != a2.Length) continue;

                            Dictionary<MetaHash, AnimationMapEntry> m1 = ycd1.AnimMap;
                            Dictionary<MetaHash, AnimationMapEntry> m2 = ycd2.AnimMap;
                            if (m1 == null || m2 == null) continue;
                            if (m1.Count != m2.Count) continue;
                            foreach (KeyValuePair<MetaHash, AnimationMapEntry> kvp1 in m1)
                            {
                                AnimationMapEntry an1 = kvp1.Value;
                                AnimationMapEntry an2 = an1;
                                if (!m2.TryGetValue(kvp1.Key, out an2)) continue;

                                Sequence[] sa1 = an1?.Animation?.Sequences?.data_items;
                                Sequence[] sa2 = an2?.Animation?.Sequences?.data_items;
                                if (sa1 == null || sa2 == null) continue;
                                if (sa1.Length != sa2.Length) continue;
                                for (int s = 0; s < sa1.Length; s++)
                                {
                                    Sequence s1 = sa1[s];
                                    Sequence s2 = sa2[s];
                                    if (s1?.Sequences == null || s2?.Sequences == null) continue;

                                    if (s1.NumFrames != s2.NumFrames)
                                    {
                                    }

                                    if (s1.ChunkSize != s2.ChunkSize)
                                    {
                                    }

                                    if (s1.FrameOffset != s2.FrameOffset)
                                    {
                                    }

                                    if (s1.DataLength != s2.DataLength)
                                    {
                                    }

                                    //for (int b = 0; b < s1.DataLength; b++)
                                    //{
                                    //    var b1 = s1.Data[b];
                                    //    var b2 = s2.Data[b];
                                    //    if (b1 != b2)
                                    //    { }
                                    //}
                                    for (int ss = 0; ss < s1.Sequences.Length; ss++)
                                    {
                                        AnimSequence ss1 = s1.Sequences[ss];
                                        AnimSequence ss2 = s2.Sequences[ss];
                                        if (ss1?.Channels == null || ss2?.Channels == null) continue;
                                        if (ss1.Channels.Length != ss2.Channels.Length) continue;


                                        for (int c = 0; c < ss1.Channels.Length; c++)
                                        {
                                            AnimChannel sc1 = ss1.Channels[c];
                                            AnimChannel sc2 = ss2.Channels[c];
                                            if (sc1 == null || sc2 == null) continue;
                                            if (sc1.Type == AnimChannelType.LinearFloat) continue;
                                            if (sc1.Type != sc2.Type) continue;
                                            if (sc1.Index != sc2.Index) continue;
                                            if (sc1.Type == AnimChannelType.StaticQuaternion)
                                            {
                                                AnimChannelStaticQuaternion acsq1 = sc1 as AnimChannelStaticQuaternion;
                                                AnimChannelStaticQuaternion acsq2 = sc2 as AnimChannelStaticQuaternion;
                                                Quaternion vdiff = acsq1.Value - acsq2.Value;
                                                float len = vdiff.Length();
                                                float v1len = Math.Max(acsq1.Value.Length(), 1);
                                                if (len > 1e-2f * v1len)
                                                {
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.StaticVector3)
                                            {
                                                AnimChannelStaticVector3 acsv1 = sc1 as AnimChannelStaticVector3;
                                                AnimChannelStaticVector3 acsv2 = sc2 as AnimChannelStaticVector3;
                                                Vector3 vdiff = acsv1.Value - acsv2.Value;
                                                float len = vdiff.Length();
                                                float v1len = Math.Max(acsv1.Value.Length(), 1);
                                                if (len > 1e-2f * v1len)
                                                {
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.StaticFloat)
                                            {
                                                AnimChannelStaticFloat acsf1 = sc1 as AnimChannelStaticFloat;
                                                AnimChannelStaticFloat acsf2 = sc2 as AnimChannelStaticFloat;
                                                float vdiff = Math.Abs(acsf1.Value - acsf2.Value);
                                                float v1len = Math.Max(Math.Abs(acsf1.Value), 1);
                                                if (vdiff > 1e-2f * v1len)
                                                {
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.RawFloat)
                                            {
                                                AnimChannelRawFloat acrf1 = sc1 as AnimChannelRawFloat;
                                                AnimChannelRawFloat acrf2 = sc2 as AnimChannelRawFloat;
                                                for (int v = 0; v < acrf1.Values.Length; v++)
                                                {
                                                    float v1 = acrf1.Values[v];
                                                    float v2 = acrf2.Values[v];
                                                    float vdiff = Math.Abs(v1 - v2);
                                                    float v1len = Math.Max(Math.Abs(v1), 1);
                                                    if (vdiff > 1e-2f * v1len) break;
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.QuantizeFloat)
                                            {
                                                AnimChannelQuantizeFloat acqf1 = sc1 as AnimChannelQuantizeFloat;
                                                AnimChannelQuantizeFloat acqf2 = sc2 as AnimChannelQuantizeFloat;
                                                if (acqf1.ValueBits != acqf2.ValueBits) continue;
                                                if (Math.Abs(acqf1.Offset - acqf2.Offset) >
                                                    0.001f * Math.Abs(acqf1.Offset)) continue;
                                                if (Math.Abs(acqf1.Quantum - acqf2.Quantum) > 0.00001f) continue;
                                                for (int v = 0; v < acqf1.Values.Length; v++)
                                                {
                                                    float v1 = acqf1.Values[v];
                                                    float v2 = acqf2.Values[v];
                                                    float vdiff = Math.Abs(v1 - v2);
                                                    float v1len = Math.Max(Math.Abs(v1), 1);
                                                    if (vdiff > 1e-2f * v1len) break;
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.IndirectQuantizeFloat)
                                            {
                                                AnimChannelIndirectQuantizeFloat aciqf1 = sc1 as AnimChannelIndirectQuantizeFloat;
                                                AnimChannelIndirectQuantizeFloat aciqf2 = sc2 as AnimChannelIndirectQuantizeFloat;
                                                if (aciqf1.FrameBits != aciqf2.FrameBits) continue;
                                                if (aciqf1.ValueBits != aciqf2.ValueBits) continue;
                                                if (Math.Abs(aciqf1.Offset - aciqf2.Offset) >
                                                    0.001f * Math.Abs(aciqf1.Offset)) continue;
                                                if (Math.Abs(aciqf1.Quantum - aciqf2.Quantum) > 0.00001f) continue;
                                                for (int f = 0; f < aciqf1.Frames.Length; f++)
                                                    if (aciqf1.Frames[f] != aciqf2.Frames[f])
                                                        break;

                                                for (int v = 0; v < aciqf1.Values.Length; v++)
                                                {
                                                    float v1 = aciqf1.Values[v];
                                                    float v2 = aciqf2.Values[v];
                                                    float vdiff = Math.Abs(v1 - v2);
                                                    float v1len = Math.Max(Math.Abs(v1), 1);
                                                    if (vdiff > 1e-2f * v1len) break;
                                                }
                                            }
                                            else if (sc1.Type == AnimChannelType.CachedQuaternion1 ||
                                                     sc1.Type == AnimChannelType.CachedQuaternion2)
                                            {
                                                AnimChannelCachedQuaternion acrf1 = sc1 as AnimChannelCachedQuaternion;
                                                AnimChannelCachedQuaternion acrf2 = sc2 as AnimChannelCachedQuaternion;
                                                if (acrf1.QuatIndex != acrf2.QuatIndex)
                                                {
                                                }
                                            }
                                        }
                                        //for (int f = 0; f < s1.NumFrames; f++)
                                        //{
                                        //    var v1 = ss1.EvaluateVector(f);
                                        //    var v2 = ss2.EvaluateVector(f);
                                        //    var vdiff = v1 - v2;
                                        //    var len = vdiff.Length();
                                        //    var v1len = Math.Max(v1.Length(), 1);
                                        //    if (len > 1e-2f*v1len)
                                        //    { }
                                        //}
                                    }
                                }
                            }
                        }
                    }
                }

            //if (entry.NameLower.EndsWith(".awc")) //awcs can also contain clip dicts..
            //{
            //    UpdateStatus(entry.Path);
            //    AwcFile awcfile = RpfMan.GetFile<AwcFile>(entry);
            //    if ((awcfile != null))
            //    { }
            //}
            //}
            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYtds()
        {
            bool ddstest = false;
            bool savetest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ytd"))
                {
                    UpdateStatus(entry.Path);
                    YtdFile ytdfile = null;
                    try
                    {
                        ytdfile = RpfMan.GetFile<YtdFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (ddstest && ytdfile != null && ytdfile.TextureDict != null)
                        foreach (Texture tex in ytdfile.TextureDict.Textures.data_items)
                        {
                            byte[] dds = DDSIO.GetDDSFile(tex);
                            Texture tex2 = DDSIO.GetTexture(dds);
                            if (!tex.Name.StartsWith("script_rt"))
                            {
                                if (tex.Data?.FullData?.Length != tex2.Data?.FullData?.Length)
                                {
                                }

                                if (tex.Stride != tex2.Stride)
                                {
                                }
                            }

                            if (tex.Format != tex2.Format || tex.Width != tex2.Width || tex.Height != tex2.Height ||
                                tex.Depth != tex2.Depth || tex.Levels != tex2.Levels)
                            {
                            }
                        }

                    if (savetest && ytdfile != null && ytdfile.TextureDict != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = ytdfile.Save();

                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);

                        if (ytdfile.TextureDict.Textures?.Count == 0)
                        {
                        }


                        YtdFile ytd2 = new YtdFile();
                        //ytd2.Load(bytes, fentry);
                        RpfFile.LoadResourceFile(ytd2, bytes, 13);

                        if (ytd2.TextureDict == null) continue;
                        if (ytd2.TextureDict.Textures?.Count != ytdfile.TextureDict.Textures?.Count) continue;

                        for (int i = 0; i < ytdfile.TextureDict.Textures.Count; i++)
                        {
                            Texture tx1 = ytdfile.TextureDict.Textures[i];
                            Texture tx2 = ytd2.TextureDict.Textures[i];
                            TextureData td1 = tx1.Data;
                            TextureData td2 = tx2.Data;
                            if (td1.FullData.Length != td2.FullData.Length) continue;

                            for (int j = 0; j < td1.FullData.Length; j++)
                                if (td1.FullData[j] != td2.FullData[j])
                                    break;
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYbns()
        {
            bool xmltest = false;
            bool savetest = false;
            bool reloadtest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ybn"))
                {
                    UpdateStatus(entry.Path);
                    YbnFile ybn = null;
                    try
                    {
                        ybn = RpfMan.GetFile<YbnFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (xmltest && ybn != null && ybn.Bounds != null)
                    {
                        string xml = YbnXml.GetXml(ybn);
                        YbnFile ybn2 = XmlYbn.GetYbn(xml);
                        string xml2 = YbnXml.GetXml(ybn2);
                        if (xml.Length != xml2.Length)
                        {
                        }
                    }

                    if (savetest && ybn != null && ybn.Bounds != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = ybn.Save();

                        if (!reloadtest) continue;

                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);


                        YbnFile ybn2 = new YbnFile();
                        RpfFile.LoadResourceFile(ybn2, bytes, 43);

                        if (ybn2.Bounds == null) continue;
                        if (ybn2.Bounds.Type != ybn.Bounds.Type) continue;

                        //quick check of roundtrip
                        switch (ybn2.Bounds.Type)
                        {
                            case BoundsType.Sphere:
                            {
                                BoundSphere a = ybn.Bounds as BoundSphere;
                                BoundSphere b = ybn2.Bounds as BoundSphere;
                                if (b == null)
                                {
                                }

                                break;
                            }
                            case BoundsType.Capsule:
                            {
                                BoundCapsule a = ybn.Bounds as BoundCapsule;
                                BoundCapsule b = ybn2.Bounds as BoundCapsule;
                                if (b == null)
                                {
                                }

                                break;
                            }
                            case BoundsType.Box:
                            {
                                BoundBox a = ybn.Bounds as BoundBox;
                                BoundBox b = ybn2.Bounds as BoundBox;
                                if (b == null)
                                {
                                }

                                break;
                            }
                            case BoundsType.Geometry:
                            {
                                BoundGeometry a = ybn.Bounds as BoundGeometry;
                                BoundGeometry b = ybn2.Bounds as BoundGeometry;
                                if (b == null) continue;
                                if (a.Polygons?.Length != b.Polygons?.Length) continue;
                                for (int i = 0; i < a.Polygons.Length; i++)
                                {
                                    BoundPolygon pa = a.Polygons[i];
                                    BoundPolygon pb = b.Polygons[i];
                                    if (pa.Type != pb.Type)
                                    {
                                    }
                                }

                                break;
                            }
                            case BoundsType.GeometryBVH:
                            {
                                BoundBVH a = ybn.Bounds as BoundBVH;
                                BoundBVH b = ybn2.Bounds as BoundBVH;
                                if (b == null) continue;
                                if (a.BVH?.Nodes?.data_items?.Length != b.BVH?.Nodes?.data_items?.Length)
                                {
                                }

                                if (a.Polygons?.Length != b.Polygons?.Length) continue;
                                for (int i = 0; i < a.Polygons.Length; i++)
                                {
                                    BoundPolygon pa = a.Polygons[i];
                                    BoundPolygon pb = b.Polygons[i];
                                    if (pa.Type != pb.Type)
                                    {
                                    }
                                }

                                break;
                            }
                            case BoundsType.Composite:
                            {
                                BoundComposite a = ybn.Bounds as BoundComposite;
                                BoundComposite b = ybn2.Bounds as BoundComposite;
                                if (b == null) continue;
                                if (a.Children?.data_items?.Length != b.Children?.data_items?.Length)
                                {
                                }

                                break;
                            }
                            case BoundsType.Disc:
                            {
                                BoundDisc a = ybn.Bounds as BoundDisc;
                                BoundDisc b = ybn2.Bounds as BoundDisc;
                                if (b == null)
                                {
                                }

                                break;
                            }
                            case BoundsType.Cylinder:
                            {
                                BoundCylinder a = ybn.Bounds as BoundCylinder;
                                BoundCylinder b = ybn2.Bounds as BoundCylinder;
                                if (b == null)
                                {
                                }

                                break;
                            }
                            case BoundsType.Cloth:
                            {
                                BoundCloth a = ybn.Bounds as BoundCloth;
                                BoundCloth b = ybn2.Bounds as BoundCloth;
                                if (b == null)
                                {
                                }

                                break;
                            }
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYdrs()
        {
            bool savetest = true;
            bool boundsonly = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ydr"))
                {
                    UpdateStatus(entry.Path);
                    YdrFile ydr = null;
                    try
                    {
                        ydr = RpfMan.GetFile<YdrFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (savetest && ydr != null && ydr.Drawable != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        if (boundsonly && ydr.Drawable.Bound == null) continue;

                        byte[] bytes = ydr.Save();

                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);

                        YdrFile ydr2 = new YdrFile();
                        RpfFile.LoadResourceFile(ydr2, bytes, (uint)ydr.GetVersion(RpfManager.IsGen9));

                        if (ydr2.Drawable == null) continue;
                        if (ydr2.Drawable.AllModels?.Length != ydr.Drawable.AllModels?.Length)
                        {
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count != 13)
            {
            }
        }

        public void TestYdds()
        {
            bool savetest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ydd"))
                {
                    UpdateStatus(entry.Path);
                    YddFile ydd = null;
                    try
                    {
                        ydd = RpfMan.GetFile<YddFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (savetest && ydd != null && ydd.DrawableDict != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = ydd.Save();

                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);


                        YddFile ydd2 = new YddFile();
                        RpfFile.LoadResourceFile(ydd2, bytes, 165);

                        if (ydd2.DrawableDict == null) continue;
                        if (ydd2.DrawableDict.Drawables?.Count != ydd.DrawableDict.Drawables?.Count) continue;
                    }

                    if (ydd?.DrawableDict?.Hashes != null)
                    {
                        uint h = 0;
                        foreach (uint th in ydd.DrawableDict.Hashes)
                        {
                            if (th <= h)
                            {
                            } //should never happen

                            h = th;
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYfts()
        {
            bool xmltest = false;
            bool savetest = false;
            bool glasstest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            StringBuilder sb = new StringBuilder();
            Dictionary<uint, int> flagdict = new Dictionary<uint, int>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".yft"))
                {
                    UpdateStatus(entry.Path);
                    YftFile yft = null;
                    try
                    {
                        yft = RpfMan.GetFile<YftFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (xmltest && yft != null && yft.Fragment != null)
                    {
                        string xml = YftXml.GetXml(yft);
                        YftFile yft2 = XmlYft.GetYft(xml); //can't do full roundtrip here due to embedded textures
                        string xml2 = YftXml.GetXml(yft2);
                        if (xml != xml2)
                        {
                        }
                    }

                    if (savetest && yft != null && yft.Fragment != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = yft.Save();


                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);

                        YftFile yft2 = new YftFile();
                        RpfFile.LoadResourceFile(yft2, bytes, 162);

                        if (yft2.Fragment == null) continue;
                        if (yft2.Fragment.Drawable?.AllModels?.Length != yft.Fragment.Drawable?.AllModels?.Length)
                            continue;
                    }

                    if (glasstest && yft?.Fragment?.GlassWindows?.data_items != null)
                    {
                        int lastf = -1;
                        for (int i = 0; i < yft.Fragment.GlassWindows.data_items.Length; i++)
                        {
                            FragGlassWindow w = yft.Fragment.GlassWindows.data_items[i];
                            if (w.Flags == lastf) continue;
                            lastf = w.Flags;
                            flagdict.TryGetValue(w.Flags, out int n);
                            if (n < 10)
                            {
                                flagdict[w.Flags] = n + 1;
                                sb.AppendLine(entry.Path + " Window " + i + ": Flags " + w.Flags + ", Low:" +
                                              w.FlagsLo + ", High:" + w.FlagsHi);
                            }
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            string teststr = sb.ToString();

            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYpts()
        {
            bool savetest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ypt"))
                {
                    UpdateStatus(entry.Path);
                    YptFile ypt = null;
                    try
                    {
                        ypt = RpfMan.GetFile<YptFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (savetest && ypt != null && ypt.PtfxList != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = ypt.Save();


                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);

                        YptFile ypt2 = new YptFile();
                        RpfFile.LoadResourceFile(ypt2, bytes, 68);

                        if (ypt2.PtfxList == null) continue;
                        if (ypt2.PtfxList.Name?.Value != ypt.PtfxList.Name?.Value)
                        {
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYnvs()
        {
            bool xmltest = true;
            bool savetest = false;
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
                if (entry.NameLower.EndsWith(".ynv"))
                {
                    UpdateStatus(entry.Path);
                    YnvFile ynv = null;
                    try
                    {
                        ynv = RpfMan.GetFile<YnvFile>(entry);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Error! " + ex);
                        errorfiles.Add(entry);
                    }

                    if (xmltest && ynv != null && ynv.Nav != null)
                    {
                        string xml = YnvXml.GetXml(ynv);
                        if (xml != null)
                        {
                        }

                        YnvFile ynv2 = XmlYnv.GetYnv(xml);
                        if (ynv2 != null)
                        {
                        }

                        byte[] ynv2b = ynv2.Save();
                        if (ynv2b != null)
                        {
                        }

                        YnvFile ynv3 = new YnvFile();
                        RpfFile.LoadResourceFile(ynv3, ynv2b, 2);
                        string xml3 = YnvXml.GetXml(ynv3);
                        if (xml.Length != xml3.Length)
                        {
                        }

                        string[] xmllines = xml.Split('\n');
                        string[] xml3lines = xml3.Split('\n');
                        if (xmllines.Length != xml3lines.Length)
                        {
                        }
                    }

                    if (savetest && ynv != null && ynv.Nav != null)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (fentry == null) continue; //shouldn't happen

                        byte[] bytes = ynv.Save();

                        string origlen = TextUtil.GetBytesReadable(fentry.FileSize);
                        string bytelen = TextUtil.GetBytesReadable(bytes.Length);

                        YnvFile ynv2 = new YnvFile();
                        RpfFile.LoadResourceFile(ynv2, bytes, 2);

                        if (ynv2.Nav == null)
                        {
                        }
                    }
                }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //}
            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestYvrs()
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                if (rfe.NameLower.EndsWith(".yvr"))
                {
                    if (rfe.NameLower == "agencyprep001.yvr") continue; //this file seems corrupted

                    UpdateStatus(entry.Path);

                    YvrFile yvr = new YvrFile(rfe);
                    RpfMan.LoadFile(yvr, rfe);

                    string xml = YvrXml.GetXml(yvr);
                    YvrFile yvr2 = XmlYvr.GetYvr(xml);
                    byte[] data2 = yvr2.Save();
                    YvrFile yvr3 = new YvrFile();
                    RpfFile.LoadResourceFile(yvr3, data2, 1); //full roundtrip
                    string xml2 = YvrXml.GetXml(yvr3);
                    if (xml != xml2)
                    {
                    }
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            if (exceptions.Count > 0)
            {
            }
        }

        public void TestYwrs()
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                //try
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                if (rfe.NameLower.EndsWith(".ywr"))
                {
                    UpdateStatus(entry.Path);

                    YwrFile ywr = new YwrFile(rfe);
                    RpfMan.LoadFile(ywr, rfe);

                    string xml = YwrXml.GetXml(ywr);
                    YwrFile ywr2 = XmlYwr.GetYwr(xml);
                    byte[] data2 = ywr2.Save();
                    YwrFile ywr3 = new YwrFile();
                    RpfFile.LoadResourceFile(ywr3, data2, 1); //full roundtrip
                    string xml2 = YwrXml.GetXml(ywr3);
                    if (xml != xml2)
                    {
                    }
                }
            }

            //catch (Exception ex)
            //{
            //    UpdateStatus("Error! " + ex.ToString());
            //    exceptions.Add(ex);
            //}
            if (exceptions.Count > 0)
            {
            }
        }

        public void TestYmaps()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (entry.NameLower.EndsWith(".ymap"))
                    {
                        UpdateStatus(entry.Path);
                        YmapFile ymapfile = RpfMan.GetFile<YmapFile>(entry);
                        if (ymapfile != null) // && (ymapfile.Meta != null))
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }
        }

        public void TestYpdbs()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                try
                {
                    if (rfe.NameLower.EndsWith(".ypdb"))
                    {
                        UpdateStatus(entry.Path);
                        YpdbFile ypdb = RpfMan.GetFile<YpdbFile>(entry);
                        if (ypdb != null)
                        {
                            byte[] odata = entry.File.ExtractFile(entry as RpfFileEntry);

                            //var ndata = ypdb.Save();
                            string xml = YpdbXml.GetXml(ypdb);
                            YpdbFile ypdb2 = XmlYpdb.GetYpdb(xml);
                            byte[] ndata = ypdb2.Save();

                            if (ndata.Length == odata.Length)
                                for (int i = 0; i < ndata.Length; i++)
                                    if (ndata[i] != odata[i])
                                        break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }
            }
        }

        public void TestYfds()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe == null) continue;

                try
                {
                    if (rfe.NameLower.EndsWith(".yfd"))
                    {
                        UpdateStatus(entry.Path);
                        YfdFile yfd = RpfMan.GetFile<YfdFile>(entry);
                        if (yfd != null)
                        {
                            if (yfd.FrameFilterDictionary != null)
                                // check that all signatures can be re-calculated
                                foreach (FrameFilterBase f in yfd.FrameFilterDictionary.Filters.data_items)
                                    if (f.Signature != f.CalculateSignature())
                                    {
                                    }

                            string xml = YfdXml.GetXml(yfd);
                            YfdFile yfd2 = XmlYfd.GetYfd(xml);
                            byte[] data2 = yfd2.Save();
                            YfdFile yfd3 = new YfdFile();
                            RpfFile.LoadResourceFile(yfd3, data2, 4); //full roundtrip
                            string xml2 = YfdXml.GetXml(yfd3);
                            if (xml != xml2)
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }
            }
        }

        public void TestMrfs()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (entry.NameLower.EndsWith(".mrf"))
                    {
                        UpdateStatus(entry.Path);
                        MrfFile mrffile = RpfMan.GetFile<MrfFile>(entry);
                        if (mrffile != null)
                        {
                            byte[] odata = entry.File.ExtractFile(entry as RpfFileEntry);
                            byte[] ndata = mrffile.Save();
                            if (ndata.Length == odata.Length)
                                for (int i = 0; i < ndata.Length; i++)
                                    if (ndata[i] != odata[i])
                                        break;

                            string xml = MrfXml.GetXml(mrffile);
                            MrfFile mrf2 = XmlMrf.GetMrf(xml);
                            byte[] ndata2 = mrf2.Save();
                            if (ndata2.Length == odata.Length)
                                for (int i = 0; i < ndata2.Length; i++)
                                    if (ndata2[i] != odata[i] && !mrfDiffCanBeIgnored(i, mrffile))
                                        break;

                            bool mrfDiffCanBeIgnored(int fileOffset, MrfFile originalMrf)
                            {
                                foreach (MrfNode n in originalMrf.AllNodes)
                                    if (n is MrfNodeStateBase state)
                                        // If TransitionCount is 0, the TransitionsOffset value can be ignored.
                                        // TransitionsOffset in original MRFs isn't always set to 0 in this case,
                                        // XML-imported MRFs always set it to 0
                                        if (state.TransitionCount == 0 && fileOffset == state.FileOffset + 0x1C)
                                            return true;

                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }

            // create and save a custom MRF
            {
                // Usage example:
                //  RequestAnimDict("move_m@alien")
                //  TaskMoveNetworkByName(PlayerPedId(), "mymrf", 0.0, true, 0, 0)
                //  SetTaskMoveNetworkSignalFloat(PlayerPedId(), "sprintrate", 2.0)
                MrfFile mymrf = new MrfFile();
                MrfNodeClip clip1 = new MrfNodeClip
                {
                    NodeIndex = 0,
                    Name = JenkHash.GenHash("clip1"),
                    ClipType = MrfValueType.Literal,
                    ClipContainerType = MrfClipContainerType.ClipDictionary,
                    ClipContainerName = JenkHash.GenHash("move_m@alien"),
                    ClipName = JenkHash.GenHash("alien_run"),
                    LoopedType = MrfValueType.Literal,
                    Looped = true
                };
                MrfNodeClip clip2 = new MrfNodeClip
                {
                    NodeIndex = 0,
                    Name = JenkHash.GenHash("clip2"),
                    ClipType = MrfValueType.Literal,
                    ClipContainerType = MrfClipContainerType.ClipDictionary,
                    ClipContainerName = JenkHash.GenHash("move_m@alien"),
                    ClipName = JenkHash.GenHash("alien_sprint"),
                    LoopedType = MrfValueType.Literal,
                    Looped = true,
                    RateType = MrfValueType.Parameter,
                    RateParameterName = JenkHash.GenHash("sprintrate")
                };
                MrfNodeState clipstate1 = new MrfNodeState
                {
                    NodeIndex = 0,
                    Name = JenkHash.GenHash("clipstate1"),
                    InitialNode = clip1,
                    Transitions = new[]
                    {
                        new MrfStateTransition
                        {
                            Duration = 2.5f,
                            HasDurationParameter = false,
                            //TargetState = clipstate2,
                            Conditions = new[]
                            {
                                new MrfConditionTimeGreaterThan { Value = 4.0f }
                            }
                        }
                    }
                };
                MrfNodeState clipstate2 = new MrfNodeState
                {
                    NodeIndex = 1,
                    Name = JenkHash.GenHash("clipstate2"),
                    InitialNode = clip2,
                    Transitions = new[]
                    {
                        new MrfStateTransition
                        {
                            Duration = 2.5f,
                            HasDurationParameter = false,
                            //TargetState = clipstate1,
                            Conditions = new[]
                            {
                                new MrfConditionTimeGreaterThan { Value = 4.0f }
                            }
                        }
                    }
                };
                clipstate1.Transitions[0].TargetState = clipstate2;
                clipstate2.Transitions[0].TargetState = clipstate1;
                MrfNodeStateMachine rootsm = new MrfNodeStateMachine
                {
                    NodeIndex = 0,
                    Name = JenkHash.GenHash("statemachine"),
                    States = new[]
                    {
                        new MrfStateRef { StateName = clipstate1.Name, State = clipstate1 },
                        new MrfStateRef { StateName = clipstate2.Name, State = clipstate2 }
                    },
                    InitialNode = clipstate1
                };
                mymrf.AllNodes = new MrfNode[]
                {
                    rootsm,
                    clipstate1,
                    clip1,
                    clipstate2,
                    clip2
                };
                mymrf.RootState = rootsm;

                byte[] mymrfData = mymrf.Save();
                //File.WriteAllBytes("mymrf.mrf", mymrfData);
                //File.WriteAllText("mymrf.dot", mymrf.DumpStateGraph());
            }
        }

        public void TestFxcs()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (entry.NameLower.EndsWith(".fxc"))
                    {
                        UpdateStatus(entry.Path);
                        FxcFile fxcfile = RpfMan.GetFile<FxcFile>(entry);
                        if (fxcfile != null)
                        {
                            byte[] odata = entry.File.ExtractFile(entry as RpfFileEntry);
                            byte[] ndata = fxcfile.Save();
                            if (ndata.Length == odata.Length)
                                for (int i = 0; i < ndata.Length; i++)
                                    if (ndata[i] != odata[i])
                                        break;

                            string xml1 = FxcXml.GetXml(fxcfile); //won't output bytecodes with no output folder
                            FxcFile fxc1 = XmlFxc.GetFxc(xml1);
                            string xml2 = FxcXml.GetXml(fxc1);
                            if (xml1 != xml2)
                            {
                            }


                            for (int i = 0; i < fxcfile.Shaders.Length; i++)
                            {
                                if (fxc1.Shaders[i].Name != fxcfile.Shaders[i].Name)
                                {
                                }

                                fxc1.Shaders[i].ByteCode = fxcfile.Shaders[i].ByteCode;
                            }

                            byte[] xdata = fxc1.Save();
                            if (xdata.Length == odata.Length)
                                for (int i = 0; i < xdata.Length; i++)
                                    if (xdata[i] != odata[i])
                                        break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }
        }

        public void TestPlacements()
        {
            //int totplacements = 0;
            //int tottimedplacements = 0;
            //int totaudioplacements = 0;
            //StringBuilder sbtest = new StringBuilder();
            //StringBuilder sbterr = new StringBuilder();
            //sbtest.AppendLine("X, Y, Z, name, assetName, drawableDictionary, textureDictionary, ymap");
            //foreach (RpfFile file in RpfMan.AllRpfs)
            //{
            //    foreach (RpfEntry entry in file.AllEntries)
            //    {
            //        try
            //        {
            //            if (entry.NameLower.EndsWith(".ymap"))
            //            {
            //                UpdateStatus(entry.Path);
            //                YmapFile ymapfile = RpfMan.GetFile<YmapFile>(entry);
            //                if ((ymapfile != null))// && (ymapfile.Meta != null))
            //                {
            //                    //if (ymapfile.CMapData.parent == 0) //root ymap output
            //                    //{
            //                    //    sbtest.AppendLine(JenkIndex.GetString(ymapfile.CMapData.name) + ": " + entry.Path);
            //                    //}
            //                    if (ymapfile.CEntityDefs != null)
            //                    {
            //                        for (int n = 0; n < ymapfile.CEntityDefs.Length; n++)
            //                        {
            //                            //find ytyp...
            //                            var entdef = ymapfile.CEntityDefs[n];
            //                            var pos = entdef.position;
            //                            bool istimed = false;
            //                            Tuple<YtypFile, int> archetyp;
            //                            if (!BaseArchetypes.TryGetValue(entdef.archetypeName, out archetyp))
            //                            {
            //                                sbterr.AppendLine("Couldn't find ytyp for " + entdef.ToString());
            //                            }
            //                            else
            //                            {
            //                                int ymapbasecount = (archetyp.Item1.CBaseArchetypeDefs != null) ? archetyp.Item1.CBaseArchetypeDefs.Length : 0;
            //                                int baseoffset = archetyp.Item2 - ymapbasecount;
            //                                if (baseoffset >= 0)
            //                                {
            //                                    if ((archetyp.Item1.CTimeArchetypeDefs == null) || (baseoffset > archetyp.Item1.CTimeArchetypeDefs.Length))
            //                                    {
            //                                        sbterr.AppendLine("Couldn't lookup CTimeArchetypeDef... " + archetyp.ToString());
            //                                        continue;
            //                                    }

            //                                    istimed = true;

            //                                    //it's a CTimeArchetypeDef...
            //                                    CTimeArchetypeDef ctad = archetyp.Item1.CTimeArchetypeDefs[baseoffset];

            //                                    //if (ctad.ToString().Contains("spider"))
            //                                    //{
            //                                    //}
            //                                    //sbtest.AppendFormat("{0}, {1}, {2}, {3}, {4}", pos.X, pos.Y, pos.Z, ctad.ToString(), entry.Name);
            //                                    //sbtest.AppendLine();

            //                                    tottimedplacements++;
            //                                }
            //                                totplacements++;
            //                            }

            //                            Tuple<YtypFile, int> audiotyp;
            //                            if (AudioArchetypes.TryGetValue(entdef.archetypeName, out audiotyp))
            //                            {
            //                                if (istimed)
            //                                {
            //                                }
            //                                if (!BaseArchetypes.TryGetValue(entdef.archetypeName, out archetyp))
            //                                {
            //                                    sbterr.AppendLine("Couldn't find ytyp for " + entdef.ToString());
            //                                }
            //                                if (audiotyp.Item1 != archetyp.Item1)
            //                                {
            //                                }

            //                                CBaseArchetypeDef cbad = archetyp.Item1.CBaseArchetypeDefs[archetyp.Item2];
            //                                CExtensionDefAudioEmitter emitr = audiotyp.Item1.AudioEmitters[audiotyp.Item2];

            //                                if (emitr.name != cbad.name)
            //                                {
            //                                }

            //                                string hashtest = JenkIndex.GetString(emitr.effectHash);

            //                                sbtest.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}", pos.X, pos.Y, pos.Z, cbad.ToString(), entry.Name, hashtest);
            //                                sbtest.AppendLine();

            //                                totaudioplacements++;
            //                            }

            //                        }
            //                    }

            //                    //if (ymapfile.TimeCycleModifiers != null)
            //                    //{
            //                    //    for (int n = 0; n < ymapfile.TimeCycleModifiers.Length; n++)
            //                    //    {
            //                    //        var tcmod = ymapfile.TimeCycleModifiers[n];
            //                    //        Tuple<YtypFile, int> archetyp;
            //                    //        if (BaseArchetypes.TryGetValue(tcmod.name, out archetyp))
            //                    //        {
            //                    //        }
            //                    //        else
            //                    //        {
            //                    //        }
            //                    //    }
            //                    //}
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            sbterr.AppendLine(entry.Path + ": " + ex.ToString());
            //        }
            //    }
            //}

            //UpdateStatus("Ymap scan finished.");

            //sbtest.AppendLine();
            //sbtest.AppendLine(totplacements.ToString() + " total CEntityDef placements parsed");
            //sbtest.AppendLine(tottimedplacements.ToString() + " total CTimeArchetypeDef placements");
            //sbtest.AppendLine(totaudioplacements.ToString() + " total CExtensionDefAudioEmitter placements");

            //string teststr = sbtest.ToString();
            //string testerr = sbterr.ToString();

            //return;
        }

        public void TestDrawables()
        {
            DateTime starttime = DateTime.Now;

            bool doydr = true;
            bool doydd = true;
            bool doyft = true;

            List<string> errs = new List<string>();
            Dictionary<ulong, VertexDeclaration> vdecls = new Dictionary<ulong, VertexDeclaration>();
            Dictionary<ulong, int> vdecluse = new Dictionary<ulong, int>();
            int drawablecount = 0;
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (doydr && entry.NameLower.EndsWith(".ydr"))
                    {
                        UpdateStatus(entry.Path);
                        YdrFile ydr = RpfMan.GetFile<YdrFile>(entry);

                        if (ydr == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read file");
                            continue;
                        }

                        if (ydr.Drawable == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read drawable data");
                            continue;
                        }

                        drawablecount++;
                        foreach (KeyValuePair<ulong, VertexDeclaration> kvp in ydr.Drawable.VertexDecls)
                            if (!vdecls.ContainsKey(kvp.Key))
                            {
                                vdecls.Add(kvp.Key, kvp.Value);
                                vdecluse.Add(kvp.Key, 1);
                            }
                            else
                            {
                                vdecluse[kvp.Key]++;
                            }
                    }
                    else if (doydd & entry.NameLower.EndsWith(".ydd"))
                    {
                        UpdateStatus(entry.Path);
                        YddFile ydd = RpfMan.GetFile<YddFile>(entry);

                        if (ydd == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read file");
                            continue;
                        }

                        if (ydd.Dict == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read drawable dictionary data");
                            continue;
                        }

                        foreach (Drawable drawable in ydd.Dict.Values)
                        {
                            drawablecount++;
                            foreach (KeyValuePair<ulong, VertexDeclaration> kvp in drawable.VertexDecls)
                                if (!vdecls.ContainsKey(kvp.Key))
                                {
                                    vdecls.Add(kvp.Key, kvp.Value);
                                    vdecluse.Add(kvp.Key, 1);
                                }
                                else
                                {
                                    vdecluse[kvp.Key]++;
                                }
                        }
                    }
                    else if (doyft && entry.NameLower.EndsWith(".yft"))
                    {
                        UpdateStatus(entry.Path);
                        YftFile yft = RpfMan.GetFile<YftFile>(entry);

                        if (yft == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read file");
                            continue;
                        }

                        if (yft.Fragment == null)
                        {
                            errs.Add(entry.Path + ": Couldn't read fragment data");
                            continue;
                        }

                        if (yft.Fragment.Drawable != null)
                        {
                            drawablecount++;
                            foreach (KeyValuePair<ulong, VertexDeclaration> kvp in yft.Fragment.Drawable.VertexDecls)
                                if (!vdecls.ContainsKey(kvp.Key))
                                {
                                    vdecls.Add(kvp.Key, kvp.Value);
                                    vdecluse.Add(kvp.Key, 1);
                                }
                                else
                                {
                                    vdecluse[kvp.Key]++;
                                }
                        }

                        if (yft.Fragment.Cloths != null && yft.Fragment.Cloths.data_items != null)
                            foreach (EnvironmentCloth cloth in yft.Fragment.Cloths.data_items)
                            {
                                drawablecount++;
                                foreach (KeyValuePair<ulong, VertexDeclaration> kvp in cloth.Drawable.VertexDecls)
                                    if (!vdecls.ContainsKey(kvp.Key))
                                    {
                                        vdecls.Add(kvp.Key, kvp.Value);
                                        vdecluse.Add(kvp.Key, 1);
                                    }
                                    else
                                    {
                                        vdecluse[kvp.Key]++;
                                    }
                            }

                        if (yft.Fragment.DrawableArray != null && yft.Fragment.DrawableArray.data_items != null)
                            foreach (FragDrawable drawable in yft.Fragment.DrawableArray.data_items)
                            {
                                drawablecount++;
                                foreach (KeyValuePair<ulong, VertexDeclaration> kvp in drawable.VertexDecls)
                                    if (!vdecls.ContainsKey(kvp.Key))
                                    {
                                        vdecls.Add(kvp.Key, kvp.Value);
                                        vdecluse.Add(kvp.Key, 1);
                                    }
                                    else
                                    {
                                        vdecluse[kvp.Key]++;
                                    }
                            }
                    }
                }
                catch (Exception ex)
                {
                    errs.Add(entry.Path + ": " + ex);
                }


            string errstr = string.Join("\r\n", errs);


            //build vertex types code string
            errs.Clear();
            StringBuilder sbverts = new StringBuilder();
            foreach (KeyValuePair<ulong, VertexDeclaration> kvp in vdecls)
            {
                VertexDeclaration vd = kvp.Value;
                int usage = vdecluse[kvp.Key];
                sbverts.AppendFormat(
                    "public struct VertexType{0} //id: {1}, stride: {2}, flags: {3}, types: {4}, refs: {5}", vd.Flags,
                    kvp.Key, vd.Stride, vd.Flags, vd.Types, usage);
                sbverts.AppendLine();
                sbverts.AppendLine("{");
                uint compid = 1;
                for (int i = 0; i < 16; i++)
                    if (((vd.Flags >> i) & 1) == 1)
                    {
                        string typestr = "Unknown";
                        uint type = (uint)(((ulong)vd.Types >> (4 * i)) & 0xF);
                        switch (type)
                        {
                            case 0: typestr = "ushort"; break; // Data[i] = new ushort[1 * count]; break;
                            case 1: typestr = "ushort2"; break; // Data[i] = new ushort[2 * count]; break;
                            case 2: typestr = "ushort3"; break; // Data[i] = new ushort[3 * count]; break;
                            case 3: typestr = "ushort4"; break; // Data[i] = new ushort[4 * count]; break;
                            case 4: typestr = "float"; break; // Data[i] = new float[1 * count]; break;
                            case 5: typestr = "Vector2"; break; // Data[i] = new float[2 * count]; break;
                            case 6: typestr = "Vector3"; break; // Data[i] = new float[3 * count]; break;
                            case 7: typestr = "Vector4"; break; // Data[i] = new float[4 * count]; break;
                            case 8: typestr = "uint"; break; // Data[i] = new uint[count]; break;
                            case 9: typestr = "uint"; break; // Data[i] = new uint[count]; break;
                            case 10: typestr = "uint"; break; // Data[i] = new uint[count]; break;
                        }

                        sbverts.AppendLine("   public " + typestr + " Component" + compid + ";");
                        compid++;
                    }

                sbverts.AppendLine("}");
                sbverts.AppendLine();
            }

            string vertstr = sbverts.ToString();
            string verrstr = string.Join("\r\n", errs);

            UpdateStatus((DateTime.Now - starttime) + " elapsed, " + drawablecount + " drawables, " + errs.Count +
                         " errors.");
        }

        public void TestCacheFiles()
        {
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (entry.NameLower.EndsWith("cache_y.dat")) // || entry.NameLower.EndsWith("cache_y_bank.dat"))
                    {
                        UpdateStatus(entry.Path);
                        CacheDatFile cdfile = RpfMan.GetFile<CacheDatFile>(entry);
                        if (cdfile != null)
                        {
                            byte[] odata = entry.File.ExtractFile(entry as RpfFileEntry);

                            //var ndata = cdfile.Save();
                            string xml = CacheDatXml.GetXml(cdfile);
                            CacheDatFile cdf2 = XmlCacheDat.GetCacheDat(xml);
                            byte[] ndata = cdf2.Save();

                            if (ndata.Length == odata.Length)
                                for (int i = 0; i < ndata.Length; i++)
                                    if (ndata[i] != odata[i])
                                        break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error! " + ex);
                }
        }

        public void TestHeightmaps()
        {
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                if (entry.NameLower.EndsWith(".dat") && entry.NameLower.StartsWith("heightmap"))
                {
                    UpdateStatus(entry.Path);
                    HeightmapFile hmf = null;
                    hmf = RpfMan.GetFile<HeightmapFile>(entry);
                    byte[] d1 = hmf.RawFileData;
                    //var d2 = hmf.Save();
                    string xml = HmapXml.GetXml(hmf);
                    HeightmapFile hmf2 = XmlHmap.GetHeightmap(xml);
                    byte[] d2 = hmf2.Save();

                    if (d1.Length == d2.Length)
                        for (int i = 0; i < d1.Length; i++)
                            if (d1[i] != d2[i])
                            {
                            }
                }

            if (errorfiles.Count > 0)
            {
            }
        }

        public void TestWatermaps()
        {
            List<RpfEntry> errorfiles = new List<RpfEntry>();
            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                if (entry.NameLower.EndsWith(".dat") && entry.NameLower.StartsWith("waterheight"))
                {
                    UpdateStatus(entry.Path);
                    WatermapFile wmf = null;
                    wmf = RpfMan.GetFile<WatermapFile>(entry);
                    //var d1 = wmf.RawFileData;
                    //var d2 = wmf.Save();
                    //var xml = WatermapXml.GetXml(wmf);
                    //var wmf2 = XmlWatermap.GetWatermap(xml);
                    //var d2 = wmf2.Save();
                    //if (d1.Length == d2.Length)
                    //{
                    //    for (int i = 0; i < d1.Length; i++)
                    //    {
                    //        if (d1[i] != d2[i])
                    //        { }
                    //    }
                    //}
                    //else
                    //{ }
                }

            if (errorfiles.Count > 0)
            {
            }
        }

        public void GetShadersXml()
        {
            bool doydr = true;
            bool doydd = true;
            bool doyft = true;
            bool doypt = true;

            Dictionary<MetaHash, ShaderXmlDataCollection> data = new Dictionary<MetaHash, ShaderXmlDataCollection>();

            void collectDrawable(DrawableBase d)
            {
                if (d?.AllModels == null) return;
                foreach (DrawableModel model in d.AllModels)
                {
                    if (model?.Geometries == null) continue;
                    foreach (DrawableGeometry geom in model.Geometries)
                    {
                        ShaderFX s = geom?.Shader;
                        if (s == null) continue;
                        ShaderXmlDataCollection dc = null;
                        if (!data.TryGetValue(s.Name, out dc))
                        {
                            dc = new ShaderXmlDataCollection();
                            dc.Name = s.Name;
                            data.Add(s.Name, dc);
                        }

                        dc.AddShaderUse(s, geom);
                    }
                }
            }


            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (doydr && entry.NameLower.EndsWith(".ydr"))
                    {
                        UpdateStatus(entry.Path);
                        YdrFile ydr = RpfMan.GetFile<YdrFile>(entry);

                        if (ydr == null) continue;
                        if (ydr.Drawable == null) continue;
                        collectDrawable(ydr.Drawable);
                    }
                    else if (doydd & entry.NameLower.EndsWith(".ydd"))
                    {
                        UpdateStatus(entry.Path);
                        YddFile ydd = RpfMan.GetFile<YddFile>(entry);

                        if (ydd == null) continue;
                        if (ydd.Dict == null) continue;
                        foreach (Drawable drawable in ydd.Dict.Values) collectDrawable(drawable);
                    }
                    else if (doyft && entry.NameLower.EndsWith(".yft"))
                    {
                        UpdateStatus(entry.Path);
                        YftFile yft = RpfMan.GetFile<YftFile>(entry);

                        if (yft == null) continue;
                        if (yft.Fragment == null) continue;
                        if (yft.Fragment.Drawable != null) collectDrawable(yft.Fragment.Drawable);
                        if (yft.Fragment.Cloths != null && yft.Fragment.Cloths.data_items != null)
                            foreach (EnvironmentCloth cloth in yft.Fragment.Cloths.data_items)
                                collectDrawable(cloth.Drawable);

                        if (yft.Fragment.DrawableArray != null && yft.Fragment.DrawableArray.data_items != null)
                            foreach (FragDrawable drawable in yft.Fragment.DrawableArray.data_items)
                                collectDrawable(drawable);
                    }
                    else if (doypt && entry.NameLower.EndsWith(".ypt"))
                    {
                        UpdateStatus(entry.Path);
                        YptFile ypt = RpfMan.GetFile<YptFile>(entry);

                        if (ypt == null) continue;
                        if (ypt.DrawableDict == null) continue;
                        foreach (DrawableBase drawable in ypt.DrawableDict.Values) collectDrawable(drawable);
                    }
                }
                catch //(Exception ex)
                {
                }


            List<ShaderXmlDataCollection> shaders = data.Values.ToList();
            shaders.Sort((a, b) => { return b.GeomCount.CompareTo(a.GeomCount); });


            StringBuilder sb = new StringBuilder();

            sb.AppendLine(MetaXml.XmlHeader);
            MetaXml.OpenTag(sb, 0, "Shaders");
            foreach (ShaderXmlDataCollection s in shaders)
            {
                MetaXml.OpenTag(sb, 1, "Item");
                MetaXml.StringTag(sb, 2, "Name", MetaXml.HashString(s.Name));
                MetaXml.WriteHashItemArray(sb, s.GetSortedList(s.FileNames).ToArray(), 2, "FileName");
                MetaXml.WriteRawArray(sb, s.GetSortedList(s.RenderBuckets).ToArray(), 2, "RenderBucket", "");
                MetaXml.OpenTag(sb, 2, "Layout");
                List<ShaderXmlVertexLayout> layouts = s.GetSortedList(s.VertexLayouts);
                foreach (ShaderXmlVertexLayout l in layouts)
                {
                    VertexDeclaration vd = new VertexDeclaration();
                    vd.Types = l.Types;
                    vd.Flags = l.Flags;
                    vd.WriteXml(sb, 3, "Item");
                }

                MetaXml.CloseTag(sb, 2, "Layout");
                MetaXml.OpenTag(sb, 2, "Parameters");
                string otstr = "Item name=\"{0}\" type=\"{1}\"";
                List<MetaName> texparams = s.GetSortedList(s.TexParams);
                Dictionary<MetaName, Dictionary<Vector4, int>> valparams = s.ValParams;
                Dictionary<MetaName, List<Vector4[]>> arrparams = s.ArrParams;
                foreach (MetaName tp in texparams)
                    MetaXml.SelfClosingTag(sb, 3, string.Format(otstr, ((ShaderParamNames)tp).ToString(), "Texture"));
                foreach (KeyValuePair<MetaName, Dictionary<Vector4, int>> vp in valparams)
                {
                    List<Vector4> svp = s.GetSortedList(vp.Value);
                    Vector4 defval = svp.FirstOrDefault();
                    MetaXml.SelfClosingTag(sb, 3,
                        string.Format(otstr, ((ShaderParamNames)vp.Key).ToString(), "Vector") + " " +
                        FloatUtil.GetVector4XmlString(defval));
                }

                foreach (KeyValuePair<MetaName, List<Vector4[]>> ap in arrparams)
                {
                    Vector4[] defval = ap.Value.FirstOrDefault();
                    MetaXml.OpenTag(sb, 3, string.Format(otstr, ((ShaderParamNames)ap.Key).ToString(), "Array"));
                    foreach (Vector4 vec in defval)
                        MetaXml.SelfClosingTag(sb, 4, "Value " + FloatUtil.GetVector4XmlString(vec));
                    MetaXml.CloseTag(sb, 3, "Item");
                }

                MetaXml.CloseTag(sb, 2, "Parameters");
                MetaXml.CloseTag(sb, 1, "Item");
            }

            MetaXml.CloseTag(sb, 0, "Shaders");

            string xml = sb.ToString();

            File.WriteAllText("C:\\Shaders.xml", xml);
        }

        public void GetShadersLegacyConversionXml()
        {
            //on legacy game files, iterate fxc files and generate mapping of old>new param names
            //for use by legacy/gen9 conversions
            //gen9 param names seem to use the other name as speficied in the fxc files...


            Dictionary<string, Dictionary<string, string>>
                dict = new Dictionary<string, Dictionary<string, string>>(); //shadername:(legacyparam:gen9param)

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                if (entry.NameLower.EndsWith(".fxc"))
                {
                    UpdateStatus(entry.Path);
                    FxcFile fxcfile = RpfMan.GetFile<FxcFile>(entry);
                    if (fxcfile != null)
                    {
                        string sname = entry.GetShortNameLower();
                        dict.TryGetValue(sname, out Dictionary<string, string> pdict);
                        if (pdict == null)
                        {
                            pdict = new Dictionary<string, string>();
                            dict[sname] = pdict;
                        }

                        FxcVariable[] paras = fxcfile.Variables2;
                        if (paras != null)
                            foreach (FxcVariable para in paras)
                            {
                                if (para == null) continue;
                                pdict[para.Name1] = para.Name2;
                            }
                    }
                }

            List<string> shadernames = dict.Keys.ToList();
            shadernames.Sort();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(MetaXml.XmlHeader);
            MetaXml.OpenTag(sb, 0, "ShadersLegacyConversion");
            foreach (string shadername in shadernames)
            {
                MetaXml.OpenTag(sb, 1, "Item");
                MetaXml.StringTag(sb, 2, "Name", shadername);
                MetaXml.StringTag(sb, 2, "FileName", shadername.ToLowerInvariant() + ".sps");
                MetaXml.OpenTag(sb, 2, "Parameters");
                dict.TryGetValue(shadername, out Dictionary<string, string> pdict);
                if (pdict != null)
                {
                    string otstr = "Item name=\"{0}\" gen9=\"{1}\"";
                    foreach (KeyValuePair<string, string> kvp in pdict)
                        MetaXml.SelfClosingTag(sb, 3, string.Format(otstr, kvp.Key, kvp.Value));
                }

                MetaXml.CloseTag(sb, 2, "Parameters");
                MetaXml.CloseTag(sb, 1, "Item");
            }

            MetaXml.CloseTag(sb, 0, "ShadersLegacyConversion");

            string xml = sb.ToString();

            File.WriteAllText("C:\\ShadersLegacyConversion.xml", xml);
        }

        public void GetShadersGen9ConversionXml()
        {
            //on gen9 game files, iterate drawables and find param offsets and types
            //use the ShadersLegacyConversion.xml to generate ShadersGen9Conversion.xml,
            //filtering to required shaders only and including the offsets and types.
            //this should allow for rebuilding the gen9 params buffers from legacy ones.


            bool doydr = true;
            bool doydd = true;
            bool doyft = true;
            bool doypt = true;

            Dictionary<MetaHash, ShaderGen9XmlDataCollection> data =
                new Dictionary<MetaHash, ShaderGen9XmlDataCollection>();

            void updateDC(ShaderGen9XmlDataCollection dc, ShaderParamInfoG9[] infos, ShaderFX s)
            {
                ShaderParamInfosG9 pi = s.G9_ParamInfos;
                ShaderParametersBlock pb = s.ParametersList;
                byte bc = pi.NumBuffers;
                uint[] bsizs = pb.G9_BufferSizes;

                int[] blens = new int[bc];
                for (int i = 0; i < bc; i++) blens[i] = (int)bsizs[i];
                dc.BufferSizes = blens;
                dc.ParamInfos = infos;
                dc.SamplerValues = pb.G9_Samplers;
            }

            void collectDrawable(DrawableBase d)
            {
                if (d?.AllModels == null) return;
                foreach (DrawableModel model in d.AllModels)
                {
                    if (model?.Geometries == null) continue;
                    foreach (DrawableGeometry geom in model.Geometries)
                    {
                        ShaderFX s = geom?.Shader;
                        if (s == null) continue;
                        data.TryGetValue(s.Name, out ShaderGen9XmlDataCollection dc);
                        if (dc == null)
                        {
                            dc = new ShaderGen9XmlDataCollection();
                            dc.Name = s.Name;
                            updateDC(dc, s.G9_ParamInfos.Params, s);
                            data[s.Name] = dc;
                        }
                        else
                        {
                            ShaderParamInfosG9 pi = s.G9_ParamInfos;
                            ShaderParametersBlock pb = s.ParametersList;
                            byte bc = pi.NumBuffers;
                            uint[] bsizs = pb.G9_BufferSizes;
                            bool changed = false; //sometimes params don't all match... ugh
                            if (dc.BufferSizes.Length != bc) changed = true;
                            for (int i = 0; i < bc; i++)
                                if (dc.BufferSizes[i] != bsizs[i])
                                    /*changed = true;*/
                                    break;

                            if (dc.ParamInfos.Length <
                                pi.Params.Length)
                                changed =
                                    true; //just take whichever has the most params.. maybe not 100% correct since really we want the latest ones
                            if (changed) updateDC(dc, pi.Params, s);
                        }
                    }
                }
            }

            foreach (RpfFile file in AllRpfs)
            foreach (RpfEntry entry in file.AllEntries)
                try
                {
                    if (doydr && entry.NameLower.EndsWith(".ydr"))
                    {
                        if (entry is RpfResourceFileEntry re)
                            if (re.Version != 159)
                                continue;

                        UpdateStatus(entry.Path);
                        YdrFile ydr = RpfMan.GetFile<YdrFile>(entry);

                        if (ydr == null) continue;
                        if (ydr.Drawable == null) continue;
                        collectDrawable(ydr.Drawable);
                    }
                    else if (doydd & entry.NameLower.EndsWith(".ydd"))
                    {
                        UpdateStatus(entry.Path);
                        YddFile ydd = RpfMan.GetFile<YddFile>(entry);

                        if (ydd == null) continue;
                        if (ydd.Dict == null) continue;
                        foreach (Drawable drawable in ydd.Dict.Values) collectDrawable(drawable);
                    }
                    else if (doyft && entry.NameLower.EndsWith(".yft"))
                    {
                        UpdateStatus(entry.Path);
                        YftFile yft = RpfMan.GetFile<YftFile>(entry);

                        if (yft == null) continue;
                        if (yft.Fragment == null) continue;
                        if (yft.Fragment.Drawable != null) collectDrawable(yft.Fragment.Drawable);
                        if (yft.Fragment.Cloths != null && yft.Fragment.Cloths.data_items != null)
                            foreach (EnvironmentCloth cloth in yft.Fragment.Cloths.data_items)
                                collectDrawable(cloth.Drawable);

                        if (yft.Fragment.DrawableArray != null && yft.Fragment.DrawableArray.data_items != null)
                            foreach (FragDrawable drawable in yft.Fragment.DrawableArray.data_items)
                                collectDrawable(drawable);
                    }
                    else if (doypt && entry.NameLower.EndsWith(".ypt"))
                    {
                        UpdateStatus(entry.Path);
                        YptFile ypt = RpfMan.GetFile<YptFile>(entry);

                        if (ypt == null) continue;
                        if (ypt.DrawableDict == null) continue;
                        foreach (DrawableBase drawable in ypt.DrawableDict.Values) collectDrawable(drawable);
                    }
                }
                catch //(Exception ex)
                {
                }


            string legxml = File.ReadAllText("C:\\ShadersLegacyConversion.xml");
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(legxml);
            XmlNodeList shaders = xdoc.SelectNodes("ShadersLegacyConversion/Item");
            Dictionary<MetaHash, XmlNode> shadernodes = new Dictionary<MetaHash, XmlNode>();
            List<string> shadernames = new List<string>();
            foreach (XmlNode shader in shaders)
            {
                string name = Xml.GetChildInnerText(shader, "Name")?.ToLowerInvariant();
                MetaHash hash = new MetaHash(JenkHash.GenHash(name));
                if (data.ContainsKey(hash) == false) continue;
                shadernodes[hash] = shader;
                shadernames.Add(name);
            }

            shadernames.Sort();

            if (shadernames.Count != data.Count)
            {
            } //this shouldn't happen - something was missing?


            StringBuilder sb = new StringBuilder();
            sb.AppendLine(MetaXml.XmlHeader);
            MetaXml.OpenTag(sb, 0, "ShadersGen9Conversion");
            foreach (string name in shadernames)
            {
                MetaHash hash = new MetaHash(JenkHash.GenHash(name));
                data.TryGetValue(hash, out ShaderGen9XmlDataCollection cd);
                shadernodes.TryGetValue(hash, out XmlNode sn);
                if (cd == null) continue; //shouldn't happen
                if (sn == null) continue; //shouldn't happen

                Dictionary<MetaHash, (string, string)>
                    pdict = new Dictionary<MetaHash, (string, string)>(); //gen9hash:(gen9str:legacystr)
                XmlNodeList pnodes = sn.SelectNodes("Parameters/Item");
                foreach (XmlNode p in pnodes)
                {
                    string old = Xml.GetStringAttribute(p, "name");
                    string gen9 = Xml.GetStringAttribute(p, "gen9");
                    MetaHash phash = new MetaHash(JenkHash.GenHash(gen9?.ToLowerInvariant()));
                    pdict[phash] = (gen9, old);
                }

                MetaXml.OpenTag(sb, 1, "Item");
                MetaXml.StringTag(sb, 2, "Name", name);
                MetaXml.StringTag(sb, 2, "FileName", Xml.GetChildInnerText(sn, "FileName"));
                MetaXml.StringTag(sb, 2, "BufferSizes", string.Join(" ", cd.BufferSizes));
                MetaXml.OpenTag(sb, 2, "Parameters");
                foreach (ShaderParamInfoG9 p in cd.ParamInfos)
                {
                    string istr = $"Item type=\"{p.Type}\"";
                    if (pdict.TryGetValue(p.Name, out (string, string) pdv))
                        istr += $" name=\"{pdv.Item1}\" old=\"{pdv.Item2}\"";
                    else
                        istr += $" name=\"{MetaXml.HashString(p.Name)}\"";
                    switch (p.Type)
                    {
                        case ShaderParamTypeG9.Texture: istr += $" index=\"{p.TextureIndex}\""; break;
                        case ShaderParamTypeG9.Unknown: istr += $" index=\"{p.SamplerIndex}\""; break;
                        case ShaderParamTypeG9.Sampler:
                            istr += $" index=\"{p.SamplerIndex}\" sampler=\"{cd.SamplerValues[p.SamplerIndex]}\"";
                            break;
                        case ShaderParamTypeG9.CBuffer:
                            istr +=
                                $" buffer=\"{p.CBufferIndex}\" length=\"{p.ParamLength}\" offset=\"{p.ParamOffset}\"";
                            break;
                    }

                    MetaXml.SelfClosingTag(sb, 3, istr);
                }

                MetaXml.CloseTag(sb, 2, "Parameters");
                MetaXml.CloseTag(sb, 1, "Item");
            }

            MetaXml.CloseTag(sb, 0, "ShadersGen9Conversion");
            string xml = sb.ToString();

            File.WriteAllText("C:\\ShadersGen9Conversion.xml", xml);
        }

        public void GetArchetypeTimesList()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
                "Name,AssetName,12am,1am,2am,3am,4am,5am,6am,7am,8am,9am,10am,11am,12pm,1pm,2pm,3pm,4pm,5pm,6pm,7pm,8pm,9pm,10pm,11pm,+12am,+1am,+2am,+3am,+4am,+5am,+6am,+7am");
            foreach (YtypFile ytyp in YtypDict.Values)
            foreach (Archetype arch in ytyp.AllArchetypes)
                if (arch.Type == MetaName.CTimeArchetypeDef)
                {
                    TimeArchetype ta = arch as TimeArchetype;
                    uint t = ta.TimeFlags;
                    sb.Append(arch.Name);
                    sb.Append(",");
                    sb.Append(arch.AssetName);
                    sb.Append(",");
                    for (int i = 0; i < 32; i++)
                    {
                        bool v = ((t >> i) & 1) == 1;
                        sb.Append(v ? "1" : "0");
                        sb.Append(",");
                    }

                    sb.AppendLine();
                }

            string csv = sb.ToString();
        }

        public static void EnsureShadersGen9ConversionData()
        {
            if (ShadersGen9ConversionData != null) return;

            string path = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(path);
            string fpath = Path.Combine(dir, "ShadersGen9Conversion.xml");
            if (File.Exists(fpath) == false)
                throw new Exception("Unable to load ShadersGen9Conversion.xml"); //where's the XML file huh?
            string gen9xml = File.ReadAllText(fpath);
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(gen9xml);
            XmlNodeList shaders = xdoc.SelectNodes("ShadersGen9Conversion/Item");
            Dictionary<MetaHash, ShaderGen9XmlDataCollection> dict =
                new Dictionary<MetaHash, ShaderGen9XmlDataCollection>();
            List<ShaderParamInfoG9> infos = new List<ShaderParamInfoG9>();
            Dictionary<byte, byte> svdict = new Dictionary<byte, byte>();
            foreach (XmlNode shader in shaders)
            {
                infos.Clear();
                svdict.Clear();

                string name = Xml.GetChildInnerText(shader, "Name")?.ToLowerInvariant();
                MetaHash hash = new MetaHash(JenkHash.GenHash(name));
                ShaderGen9XmlDataCollection dc = new ShaderGen9XmlDataCollection();
                dc.Name = hash;
                dc.BufferSizes = Xml.GetChildRawIntArray(shader, "BufferSizes");
                dc.ParamsMapLegacyToGen9 = new Dictionary<MetaHash, MetaHash>();
                dc.ParamsMapGen9ToLegacy = new Dictionary<MetaHash, MetaHash>();

                XmlNodeList pnodes = shader.SelectNodes("Parameters/Item");
                foreach (XmlNode p in pnodes)
                {
                    string ptype = Xml.GetStringAttribute(p, "type");
                    string pname = Xml.GetStringAttribute(p, "name")?.ToLowerInvariant();
                    string pnameold = Xml.GetStringAttribute(p, "old")?.ToLowerInvariant();
                    uint phash = JenkHash.GenHash(pname);
                    uint phashold = JenkHash.GenHash(pnameold);
                    if (phash != 0)
                    {
                        if (pname.StartsWith("hash_"))
                            phash = (MetaHash)Convert.ToUInt32(pname.Substring(5), 16);
                        else
                            JenkIndex.Ensure(pname);
                    }

                    Enum.TryParse(ptype, out ShaderParamTypeG9 pt);
                    ShaderParamInfoG9 ps = new ShaderParamInfoG9();
                    ps.Name = phash;
                    ps.Type = pt;
                    switch (pt)
                    {
                        case ShaderParamTypeG9.Texture:
                            ps.TextureIndex = (byte)Xml.GetIntAttribute(p, "index");
                            break;
                        case ShaderParamTypeG9.Unknown:
                            ps.SamplerIndex = (byte)Xml.GetIntAttribute(p, "index");
                            break;
                        case ShaderParamTypeG9.Sampler:
                            ps.SamplerIndex = (byte)Xml.GetIntAttribute(p, "index");
                            svdict[ps.SamplerIndex] = (byte)Xml.GetIntAttribute(p, "sampler");
                            break;
                        case ShaderParamTypeG9.CBuffer:
                            ps.CBufferIndex = (byte)Xml.GetIntAttribute(p, "buffer");
                            ps.ParamLength = (ushort)Xml.GetUIntAttribute(p, "length");
                            ps.ParamOffset = (ushort)Xml.GetUIntAttribute(p, "offset");
                            break;
                    }

                    infos.Add(ps);

                    if (phash != 0 && phashold != 0)
                    {
                        dc.ParamsMapLegacyToGen9[phashold] = phash;
                        dc.ParamsMapGen9ToLegacy[phash] = phashold;
                    }
                }

                dc.ParamInfos = infos.ToArray();

                int scnt = 0;
                foreach (KeyValuePair<byte, byte> kvp in svdict)
                    if (kvp.Key >= scnt)
                        scnt = kvp.Key + 1;
                byte[] svals = new byte[scnt];
                foreach (KeyValuePair<byte, byte> kvp in svdict) svals[kvp.Key] = kvp.Value;
                dc.SamplerValues = svals;

                dict[hash] = dc;
            }

            ShadersGen9ConversionData = dict;
        }

        public class ShaderGen9XmlDataCollection
        {
            public int[] BufferSizes;
            public MetaHash Name;
            public ShaderParamInfoG9[] ParamInfos;
            public Dictionary<MetaHash, MetaHash> ParamsMapGen9ToLegacy;
            public Dictionary<MetaHash, MetaHash> ParamsMapLegacyToGen9;
            public byte[] SamplerValues;
        }

        private class ShaderXmlDataCollection
        {
            public MetaHash Name { get; set; }
            public Dictionary<MetaHash, int> FileNames { get; } = new Dictionary<MetaHash, int>();
            public Dictionary<byte, int> RenderBuckets { get; } = new Dictionary<byte, int>();

            public Dictionary<ShaderXmlVertexLayout, int> VertexLayouts { get; } =
                new Dictionary<ShaderXmlVertexLayout, int>();

            public Dictionary<MetaName, int> TexParams { get; } = new Dictionary<MetaName, int>();

            public Dictionary<MetaName, Dictionary<Vector4, int>> ValParams { get; } =
                new Dictionary<MetaName, Dictionary<Vector4, int>>();

            public Dictionary<MetaName, List<Vector4[]>> ArrParams { get; } =
                new Dictionary<MetaName, List<Vector4[]>>();

            public int GeomCount { get; set; }


            public void AddShaderUse(ShaderFX s, DrawableGeometry g)
            {
                GeomCount++;

                AddItem(s.FileName, FileNames);
                AddItem(s.RenderBucket, RenderBuckets);

                VertexDeclaration info = g.VertexBuffer?.Info;
                if (info != null)
                    AddItem(new ShaderXmlVertexLayout { Flags = info.Flags, Types = info.Types }, VertexLayouts);

                if (s.ParametersList?.Parameters == null) return;
                if (s.ParametersList?.Hashes == null) return;

                for (int i = 0; i < s.ParametersList.Count; i++)
                {
                    MetaName h = s.ParametersList.Hashes[i];
                    ShaderParameter p = s.ParametersList.Parameters[i];

                    if (p.DataType == 0) //texture
                    {
                        AddItem(h, TexParams);
                    }
                    else if (p.DataType == 1) //vector
                    {
                        Dictionary<Vector4, int> vp = GetItem(h, ValParams);
                        if (p.Data is Vector4 vec) AddItem(vec, vp);
                    }
                    else if (p.DataType > 1) //array
                    {
                        List<Vector4[]> ap = GetItem(h, ArrParams);
                        if (p.Data is Vector4[] arr)
                        {
                            bool found = false;
                            foreach (Vector4[] exarr in ap)
                            {
                                if (exarr.Length != arr.Length) continue;
                                bool match = true;
                                for (int j = 0; j < exarr.Length; j++)
                                    if (exarr[j] != arr[j])
                                    {
                                        match = false;
                                        break;
                                    }

                                if (match)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found) ap.Add(arr);
                        }
                    }
                }
            }

            public void AddItem<T>(T t, Dictionary<T, int> d)
            {
                if (d.ContainsKey(t))
                    d[t]++;
                else
                    d[t] = 1;
            }

            public U GetItem<T, U>(T t, Dictionary<T, U> d) where U : new()
            {
                U r = default(U);
                if (!d.TryGetValue(t, out r))
                {
                    r = new U();
                    d[t] = r;
                }

                return r;
            }

            public List<T> GetSortedList<T>(Dictionary<T, int> d)
            {
                List<KeyValuePair<T, int>> kvps = d.ToList();
                kvps.Sort((a, b) => { return b.Value.CompareTo(a.Value); });
                return kvps.Select(a => { return a.Key; }).ToList();
            }
        }

        private struct ShaderXmlVertexLayout
        {
            public VertexDeclarationTypes Types { get; set; }
            public uint Flags { get; set; }
            public VertexType VertexType => (VertexType)Flags;

            public override string ToString()
            {
                return Types + ", " + VertexType;
            }
        }
    }
}