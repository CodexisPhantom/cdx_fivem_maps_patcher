using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CodeWalker.Core.Utils
{
    public class Gen9Converter
    {
        //converts files from legacy to enhanced format.

        public string InputFolder;//source of files to convert
        public string OutputFolder;//destination for converted files
        public bool ProcessSubfolders = true;//recurse all the subfolders?
        public bool OverwriteExisting = true;//replace existing files in the output folder? (otherwise ignore)
        public bool CopyUnconverted = true;//also copy files that don't need converting?
        public Func<string, string, bool> QuestionFunc;//(message, title, result) called from the calling thread only
        public Action<string> ErrorAction;//this will be called from the calling thread only, for prechecks.
        public Action<string> LogAction;//this will be called from the conversion task thread during processing.
        public Action<float> ProgressAction;//will be called for each file being converted
        public Action<bool> StartStopAction;//(bool start) called when actual conversion process begins/ends


        public void Convert()
        {
            string inputFolder = InputFolder?.Replace('/', '\\');
            string outputFolder = OutputFolder?.Replace('/', '\\');
            bool subfolders = ProcessSubfolders;
            bool overwrite = OverwriteExisting;
            bool copyunconverted = CopyUnconverted;
            if (string.IsNullOrEmpty(inputFolder))
            {
                Error("Please select an input folder.");
                return;
            }
            if (string.IsNullOrEmpty(outputFolder))
            {
                Error("Please select an output folder.");
                return;
            }
            if (inputFolder.EndsWith("\\") == false)
            {
                inputFolder = inputFolder + "\\";
            }
            if (outputFolder.EndsWith("\\") == false)
            {
                outputFolder = outputFolder + "\\";
            }
            if (inputFolder.Equals(outputFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                Error("Input folder and Output folder must be different.");
                return;
            }
            if (Directory.Exists(inputFolder) == false)
            {
                Error($"Input folder {inputFolder} does not exist.");
                return;
            }
            if (Directory.Exists(outputFolder) == false)
            {
                if (Question($"Output folder {outputFolder} does not exist.\nWould you like to create it?", "Create output folder?", true) == true)
                {
                    try
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                    catch (Exception ex)
                    {
                        Error($"Error creating Output folder:\n{ex.Message}");
                        return;
                    }
                    if (Directory.Exists(outputFolder) == false)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            string[] allpaths = null;
            try
            {
                SearchOption soption = subfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                allpaths = Directory.GetFileSystemEntries(inputFolder, "*", soption);
                if ((allpaths == null) || (allpaths.Length == 0))
                {
                    Error($"Input folder {inputFolder} is empty.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Error($"Error listing Input folder contents:\n{ex.Message}");
                return;
            }

            float singleFileProgress = 1.0f / allpaths.Length;
            int curFile = 0;

            Task.Run(new Action(() =>
            {
                StartStop(true);
                Log($"Conversion process started at {DateTime.Now}");
                Log($"Input folder: {inputFolder}\r\nOutput folder: {outputFolder}");

                bool exgen9 = RpfManager.IsGen9;
                RpfManager.IsGen9 = true;

                foreach (string path in allpaths)
                {
                    try
                    {
                        float baseprogress = curFile * singleFileProgress;
                        curFile++;
                        Progress(baseprogress);
                        string pathl = path.ToLowerInvariant();
                        string relpath = path.Substring(inputFolder.Length);
                        string outpath = outputFolder + relpath;
                        if ((overwrite == false) && File.Exists(outpath))
                        {
                            Log($"{relpath} - output file already exists, skipping.");
                            continue;
                        }
                        if (File.Exists(path) == false)
                        {
                            //Log($"{relpath} - input file does not exist, skipping.");
                            continue;
                        }
                        string outdir = Path.GetDirectoryName(outpath);
                        if (Directory.Exists(outdir) == false)
                        {
                            Directory.CreateDirectory(outdir);
                        }

                        //Log($"{relpath}...");
                        string ext = Path.GetExtension(pathl);
                        if (ext == ".rpf")
                        {
                            //Log($"{relpath} - Cannot convert RPF files! Extract the contents of the RPF and convert that instead.");
                            Log($"{relpath} - Converting contents...");

                            if (File.Exists(outpath))
                            {
                                File.Delete(outpath);
                            }
                            File.Copy(path, outpath);

                            //var rpflog = new Action<string>((str) => { });
                            RpfFile rpf = new RpfFile(outpath, relpath);
                            rpf.ScanStructure(Log, Log);//rpflog

                            //check if anything needs converting first?
                            //make sure to process child rpf's first!
                            //defragment each rpf after finished converting items in it.

                            List<RpfFile> rpflist = new List<RpfFile>();
                            Stack<RpfFile> rpfstack = new Stack<RpfFile>();
                            rpfstack.Push(rpf);
                            while (rpfstack.Count > 0) //recurse the whole rpf hierarchy into the list
                            {
                                RpfFile trpf = rpfstack.Pop();
                                if (trpf == null) continue;
                                if (trpf.Children != null)
                                {
                                    foreach (RpfFile crpf in trpf.Children)
                                    {
                                        rpfstack.Push(crpf);
                                    }
                                }
                                rpflist.Add(trpf);
                            }
                            rpflist.Reverse();//reverse the list so children always come before parents
                            float rpfprogressstep = singleFileProgress / Math.Max(1, rpflist.Count);
                            int curRpf = 0;
                            HashSet<RpfFile> changedparents = new HashSet<RpfFile>();
                            foreach (RpfFile trpf in rpflist)
                            {
                                float currpfprogress = baseprogress + (curRpf * rpfprogressstep);
                                curRpf++;
                                Progress(currpfprogress);

                                if (trpf == null) continue;
                                if (trpf.AllEntries == null) continue;

                                bool changed = changedparents.Contains(trpf);

                                List<RpfResourceFileEntry> allentries = new List<RpfResourceFileEntry>();
                                foreach (RpfEntry entry in trpf.AllEntries)
                                {
                                    if (entry is RpfResourceFileEntry rfe) allentries.Add(rfe);
                                }
                                allentries.Sort((a, b) => a.FileOffset.CompareTo(b.FileOffset));

                                float entryprogressstep = rpfprogressstep / Math.Max(1, allentries.Count);
                                int curEntry = 0;
                                foreach (RpfResourceFileEntry rfe in allentries)
                                {
                                    float curentryprogress = currpfprogress + (curEntry * entryprogressstep);
                                    curEntry++;

                                    if (RequiresConversion(rfe) == false) continue;

                                    Progress(curentryprogress);

                                    RpfDirectoryEntry dir = rfe.Parent;
                                    string name = rfe.Name;
                                    string type = Path.GetExtension(rfe.NameLower);
                                    byte[] datain = trpf.ExtractFile(rfe);//unfortunately the data extracted here is decompressed but we need a compressed resource file to convert.
                                    datain = ResourceBuilder.Compress(datain); //not completely ideal to recompress it...
                                    datain = ResourceBuilder.AddResourceHeader(rfe, datain);

                                    byte[] dataout = TryConvert(datain, type, Log, rfe.Path, false, out bool converted);

                                    if ((converted == false) || (dataout == null))
                                    {
                                        Log($"{rfe.Path} - ERROR - unable to convert!");
                                        continue;
                                    }

                                    RpfFile.CreateFile(dir, name, dataout, true);

                                    changed = true;
                                }

                                if (changed)
                                {
                                    Log($"{trpf.Path} - Defragmenting");
                                    RpfFile.Defragment(trpf, null, false);
                                    
                                    if (trpf.Parent != null) //flag the parent as changed to force defragment it also
                                    {
                                        changedparents.Add(trpf.Parent);
                                    }
                                }
                            }


                        }
                        else
                        {
                            byte[] datain = File.ReadAllBytes(path);
                            byte[] dataout = TryConvert(datain, ext, Log, relpath, copyunconverted, out bool converted);
                            if (dataout != null)
                            {
                                File.WriteAllBytes(outpath, dataout);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing {path}:\r\n{ex}");
                    }
                }

                RpfManager.IsGen9 = exgen9;

                Log($"Conversion process completed at {DateTime.Now}");
                StartStop(false);
            }));

        }

        public static byte[] TryConvert(byte[] data, string fileType, bool copyunconverted = false)
        {
            Action<string> log = new Action<string>((str) => { });
            string relpath = fileType;
            byte[] dataout = TryConvert(data, fileType, log, relpath, copyunconverted, out bool converted);
            if (converted) return dataout;
            if (copyunconverted) return dataout;
            return null;
        }
        public static byte[] TryConvert(byte[] data, string fileType, Action<string> log, string relpath, bool copyunconverted, out bool converted)
        {
            converted = false;
            string exmsg = " - already gen9 format";
            if (copyunconverted) exmsg += ", directly copying file.";
            RpfResourceFileEntry rfe = (RpfResourceFileEntry)null;
            switch (fileType)
            {
                case ".ytd":
                    YtdFile ytd = new YtdFile();
                    ytd.Load(data);
                    rfe = ytd.RpfFileEntry as RpfResourceFileEntry;
                    if (rfe?.Version == 5)
                    {
                        log($"{relpath}{exmsg}");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - converting...");
                        converted = true;
                        return ytd.Save();
                    }
                case ".ydr":
                    YdrFile ydr = new YdrFile();
                    ydr.Load(data);
                    rfe = ydr.RpfFileEntry as RpfResourceFileEntry;
                    if (rfe?.Version == 159)
                    {
                        log($"{relpath}{exmsg}");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - converting...");
                        converted = true;
                        return ydr.Save();
                    }
                case ".ydd":
                    YddFile ydd = new YddFile();
                    ydd.Load(data);
                    rfe = ydd.RpfFileEntry as RpfResourceFileEntry;
                    if (rfe?.Version == 159)
                    {
                        log($"{relpath}{exmsg}");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - converting...");
                        converted = true;
                        return ydd.Save();
                    }
                case ".yft":
                    YftFile yft = new YftFile();
                    yft.Load(data);
                    rfe = yft.RpfFileEntry as RpfResourceFileEntry;
                    if (rfe?.Version == 171)
                    {
                        log($"{relpath}{exmsg}");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - converting...");
                        converted = true;
                        return yft.Save();
                    }
                case ".ypt":
                    YptFile ypt = new YptFile();
                    ypt.Load(data);
                    rfe = ypt.RpfFileEntry as RpfResourceFileEntry;
                    if (rfe?.Version == 71)
                    {
                        log($"{relpath}{exmsg}");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - converting...");
                        converted = true;
                        return ypt.Save();
                    }
                case ".rpf":
                    log($"{relpath} - Cannot convert RPF files! Extract the contents of the RPF and convert that instead.");
                    return null;
                default:
                    if (copyunconverted)
                    {
                        log($"{relpath} - conversion not required, directly copying file.");
                        return data;
                    }
                    else
                    {
                        log($"{relpath} - conversion not required, skipping.");
                        return null;
                    }
            }

        }


        public static bool RequiresConversion(RpfResourceFileEntry rfe)
        {
            if (rfe == null) return false;
            if (string.IsNullOrEmpty(rfe.NameLower)) return false;
            int didx = rfe.NameLower.LastIndexOf('.');
            if (didx < 0) return false;
            string ext = rfe.NameLower.Substring(didx);
            switch (ext)
            {
                case ".ytd": return rfe.Version != 5;
                case ".ydr": return rfe.Version != 159;
                case ".ydd": return rfe.Version != 159;
                case ".yft": return rfe.Version != 171;
                case ".ypt": return rfe.Version != 71;
            }
            return false;
        }


        private bool Question(string msg, string title, bool nullChoice)
        {
            if (QuestionFunc != null) return QuestionFunc(msg, title);
            return nullChoice;
        }
        private void Error(string msg)
        {
            if (ErrorAction != null) ErrorAction(msg);
        }
        private void Log(string msg)
        {
            if (LogAction != null) LogAction(msg);
        }
        private void Progress(float prog)
        {
            if (ProgressAction != null) ProgressAction(prog);
        }
        private void StartStop(bool start)
        {
            if (StartStopAction != null) StartStopAction(start);
        }



    }
}
