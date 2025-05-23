using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CodeWalker.GameFiles
{
    public class ResourceBuilder
    {
        protected const int RESOURCE_IDENT = 0x37435352;
        protected const int BASE_SIZE = 0x2000;
        private const int SKIP_SIZE = 16; //512;//256;//64;
        private const int ALIGN_SIZE = 16; //512;//64;

        public static void GetBlocks(IResourceBlock rootBlock, out IList<IResourceBlock> sys,
            out IList<IResourceBlock> gfx)
        {
            HashSet<IResourceBlock> systemBlocks = new HashSet<IResourceBlock>();
            HashSet<IResourceBlock> graphicBlocks = new HashSet<IResourceBlock>();
            HashSet<IResourceBlock> processed = new HashSet<IResourceBlock>();


            void addBlock(IResourceBlock block)
            {
                if (block is IResourceSystemBlock)
                {
                    if (!systemBlocks.Contains(block)) systemBlocks.Add(block);
                }
                else if (block is IResourceGraphicsBlock)
                {
                    if (!graphicBlocks.Contains(block)) graphicBlocks.Add(block);
                }
            }

            void addChildren(IResourceBlock block)
            {
                if (block is IResourceSystemBlock sblock)
                {
                    IResourceBlock[] references = sblock.GetReferences();
                    foreach (IResourceBlock reference in references)
                        if (!processed.Contains(reference))
                        {
                            processed.Add(reference);
                            addBlock(reference);
                            addChildren(reference);
                        }

                    Tuple<long, IResourceBlock>[] parts = sblock.GetParts();
                    foreach (Tuple<long, IResourceBlock> part in parts) addChildren(part.Item2);
                }
            }

            addBlock(rootBlock);
            addChildren(rootBlock);


            sys = new List<IResourceBlock>();
            foreach (IResourceBlock s in systemBlocks) sys.Add(s);
            gfx = new List<IResourceBlock>();
            foreach (IResourceBlock s in graphicBlocks) gfx.Add(s);
        }

        public static void AssignPositions(IList<IResourceBlock> blocks, uint basePosition,
            out RpfResourcePageFlags pageFlags, uint maxPageCount)
        {
            if (blocks.Count > 0 && blocks[0] is Meta)
            {
                //use naive packing strategy for Meta resources, due to crashes caused by the improved packing
                AssignPositionsForMeta(blocks, basePosition, out pageFlags);
                return;
            }

            bool sys = basePosition == 0x50000000;

            long pad(long p)
            {
                return (ALIGN_SIZE - p % ALIGN_SIZE) % ALIGN_SIZE;
            }

            long largestBlockSize = 0; // find largest structure
            long startPageSize = BASE_SIZE; // 0x2000; // find starting page size
            long totalBlockSize = 0;
            foreach (IResourceBlock block in blocks)
            {
                long blockLength = block.BlockLength;
                totalBlockSize += blockLength;
                totalBlockSize += pad(totalBlockSize);
                if (largestBlockSize < blockLength) largestBlockSize = blockLength;
            }

            while (startPageSize < largestBlockSize) startPageSize *= 2;


            pageFlags = new RpfResourcePageFlags();
            int pageSizeMult = 1;

            while (true)
            {
                if (blocks.Count == 0) break;

                ResourceBuilderBlockSet blockset = new ResourceBuilderBlockSet(blocks, sys);
                ResourceBuilderBlock rootblock = blockset.RootBlock;
                long currentPosition = 0L;
                long currentPageSize = startPageSize;
                long currentPageStart = 0L;
                long currentPageSpace = startPageSize;
                long currentRemainder = totalBlockSize;
                int pageCount = 1;
                uint[] pageCounts = new uint[9];
                int pageCountIndex = 0;
                long targetPageSize = Math.Max(65536 * pageSizeMult, startPageSize >> (sys ? 5 : 2));
                long minPageSize = Math.Max(512 * pageSizeMult, Math.Min(targetPageSize, startPageSize) >> 4);
                uint baseShift = 0u;
                int baseSize = 512;
                while (baseSize < minPageSize)
                {
                    baseShift++;
                    baseSize *= 2;
                    if (baseShift >= 0xF) break;
                }

                int baseSizeMax = baseSize << 8;
                long baseSizeMaxTest = startPageSize;
                while (baseSizeMaxTest < baseSizeMax)
                {
                    pageCountIndex++;
                    baseSizeMaxTest *= 2;
                }

                pageCounts[pageCountIndex] = 1;

                while (true)
                {
                    bool isroot = sys && currentPosition == 0;
                    ResourceBuilderBlock block = isroot ? rootblock : blockset.TakeBestBlock(currentPageSpace);
                    long blockLength = block?.Length ?? 0;
                    if (block != null)
                    {
                        //add this block to the current page.
                        block.Block.FilePosition = basePosition + currentPosition;
                        long opos = currentPosition;
                        currentPosition += blockLength;
                        currentPosition += pad(currentPosition);
                        long usedspace = currentPosition - opos;
                        currentPageSpace -= usedspace;
                        currentRemainder -= usedspace; //blockLength;// 
                    }
                    else if (blockset.Count > 0)
                    {
                        //allocate a new page
                        currentPageStart += currentPageSize;
                        currentPosition = currentPageStart;
                        block = blockset.FindBestBlock(long.MaxValue); //just find the biggest block
                        blockLength = block?.Length ?? 0;
                        while (blockLength <= currentPageSize >> 1) //determine best new page size
                        {
                            if (currentPageSize <= minPageSize) break;
                            if (pageCountIndex >= 8) break;
                            if (currentPageSize <= targetPageSize &&
                                currentRemainder >= currentPageSize - minPageSize) break;

                            currentPageSize = currentPageSize >> 1;
                            pageCountIndex++;
                        }

                        currentPageSpace = currentPageSize;
                        pageCounts[pageCountIndex]++;
                        pageCount++;
                    }
                    else
                    {
                        break;
                    }
                }


                pageFlags = new RpfResourcePageFlags(pageCounts, baseShift);

                if (pageCount == pageFlags.Count && pageFlags.Size >= currentPosition &&
                    pageCount <= maxPageCount) //make sure page counts fit in the flags value
                    break;

                startPageSize *= 2;
                pageSizeMult *= 2;
            }
        }

        public static void AssignPositionsForMeta(IList<IResourceBlock> blocks, uint basePosition,
            out RpfResourcePageFlags pageFlags)
        {
            // find largest structure
            long largestBlockSize = 0;
            foreach (IResourceBlock block in blocks)
                if (largestBlockSize < block.BlockLength)
                    largestBlockSize = block.BlockLength;

            // find minimum page size
            long currentPageSize = 0x2000;
            while (currentPageSize < largestBlockSize)
                currentPageSize *= 2;

            long currentPageCount;
            long currentPosition;
            while (true)
            {
                currentPageCount = 0;
                currentPosition = 0;

                // reset all positions
                foreach (IResourceBlock block in blocks)
                    block.FilePosition = -1;

                foreach (IResourceBlock block in blocks)
                {
                    if (block.FilePosition != -1)
                        throw new Exception("Block was already assigned a position!");

                    // check if new page is necessary...
                    // if yes, add a new page and align to it
                    long maxSpace = currentPageCount * currentPageSize - currentPosition;
                    if (maxSpace < block.BlockLength + SKIP_SIZE)
                    {
                        currentPageCount++;
                        currentPosition = currentPageSize * (currentPageCount - 1);
                    }

                    // set position
                    block.FilePosition = basePosition + currentPosition;
                    currentPosition += block.BlockLength; // + SKIP_SIZE; //is padding everywhere really necessary??

                    // align...
                    if (currentPosition % ALIGN_SIZE != 0)
                        currentPosition += ALIGN_SIZE - currentPosition % ALIGN_SIZE;
                }

                // break if everything fits...
                if (currentPageCount < 128)
                    break;

                currentPageSize *= 2;
            }

            pageFlags = new RpfResourcePageFlags(
                RpfResourceFileEntry.GetFlagsFromBlocks((uint)currentPageCount, (uint)currentPageSize, 0));
        }


        public static void AssignPositions2(IList<IResourceBlock> blocks, uint basePosition,
            out RpfResourcePageFlags pageFlags, uint maxPageCount, bool gen9)
        {
            if (blocks.Count > 0 && blocks[0] is Meta) //TODO: try remove this?
            {
                //use naive packing strategy for Meta resources, due to crashes caused by the improved packing
                AssignPositionsForMeta(blocks, basePosition, out pageFlags);
                return;
            }

            //find optimal BaseShift value for the smallest block size
            //for small system blocks should be 0, but for large physical blocks can be much bigger
            //also, the largest block needs to fit into the largest page.
            //BaseSize = 0x2000 << BaseShift   (max BaseShift = 0xF)
            //then allocate page counts for the page sizes:
            //allows for 5 page sizes, each double the size of the previous, with max counts 0x7F, 0x3F, 0xF, 3, 1
            //also allows for 4 tail pages, each half the size of the previous, only one page of each size [TODO?] 

            bool sys = basePosition == 0x50000000;
            long maxPageSizeMult = 16L; //the biggest page is 16x the base page size.
            long maxPageSize = (0x2000 << 0xF) * maxPageSizeMult; //this is the size of the biggest possible page [4GB!]
            long maxBlockSize = 0L;
            long minBlockSize = blocks.Count == 0 ? 0 : maxPageSize;
            if (gen9)
                foreach (IResourceBlock block in blocks)
                {
                    if (block.BlockLength_Gen9 > maxBlockSize) maxBlockSize = block.BlockLength_Gen9;
                    if (block.BlockLength_Gen9 < minBlockSize) minBlockSize = block.BlockLength_Gen9;
                }
            else
                foreach (IResourceBlock block in blocks)
                {
                    if (block.BlockLength > maxBlockSize) maxBlockSize = block.BlockLength;
                    if (block.BlockLength < minBlockSize) minBlockSize = block.BlockLength;
                }

            int baseShift = 0; //want to find the best value for this
            long baseSize = 0x2000L; //corresponding size for the baseShift value
            while ((baseSize < minBlockSize || baseSize * maxPageSizeMult < maxBlockSize) && baseShift < 0xF)
            {
                baseShift++;
                baseSize = 0x2000L << baseShift;
            }

            if (baseSize * maxPageSizeMult < maxBlockSize) throw new Exception("Unable to fit largest block!");


            List<IResourceBlock> sortedBlocks = new List<IResourceBlock>();
            IResourceBlock rootBlock = sys && blocks.Count > 0 ? blocks[0] : null;
            foreach (IResourceBlock block in blocks)
            {
                if (block == null) continue;
                if (block != rootBlock) sortedBlocks.Add(block);
            }

            if (gen9)
                sortedBlocks.Sort((a, b) => b.BlockLength_Gen9.CompareTo(a.BlockLength_Gen9));
            else
                sortedBlocks.Sort((a, b) => b.BlockLength.CompareTo(a.BlockLength));
            if (rootBlock != null) sortedBlocks.Insert(0, rootBlock);


            uint[] pageCounts = new uint[5];
            List<long>[] pageSizes = new List<long>[5];
            Dictionary<IResourceBlock, (int, int, long)>
                blockPages = new Dictionary<IResourceBlock, (int, int, long)>(); //(pageSizeIndex, pageIndex, offset)
            while (true)
            {
                for (int i = 0; i < 5; i++)
                {
                    pageCounts[i] = 0;
                    pageSizes[i] = null;
                }

                int largestPageSizeI = 0;
                long largestPageSize = baseSize;
                while (largestPageSize < maxBlockSize)
                {
                    largestPageSizeI++;
                    largestPageSize *= 2;
                }

                for (int i = 0; i < sortedBlocks.Count; i++)
                {
                    IResourceBlock block = sortedBlocks[i];
                    long size = gen9 ? block.BlockLength_Gen9 : block.BlockLength;
                    if (i == 0) //first block should always go in the first page, it's either root block or largest
                    {
                        pageSizes[largestPageSizeI] = new List<long> { size }; //allocate the first new page
                        blockPages[block] = (largestPageSizeI, 0, 0);
                    }
                    else
                    {
                        int pageSizeIndex = 0;
                        long pageSize = baseSize;
                        while (size > pageSize &&
                               pageSizeIndex < largestPageSizeI) //find the smallest page that will fit this block
                        {
                            pageSizeIndex++;
                            pageSize *= 2;
                        }

                        bool found = false; //find an existing page of this size or larger which has space
                        int testPageSizeI = pageSizeIndex;
                        long testPageSize = pageSize;
                        while (found == false && testPageSizeI <= largestPageSizeI)
                        {
                            List<long> list = pageSizes[testPageSizeI];
                            if (list != null)
                                for (int p = 0; p < list.Count; p++)
                                {
                                    long s = list[p];
                                    s += (ALIGN_SIZE - s % ALIGN_SIZE) % ALIGN_SIZE;
                                    long o = s;
                                    s += size;
                                    if (s <= testPageSize)
                                    {
                                        list[p] = s;
                                        found = true;
                                        blockPages[block] = (testPageSizeI, p, o);
                                        break;
                                    }
                                }

                            testPageSizeI++;
                            testPageSize *= 2;
                        }

                        if (found == false) //couldn't find an existing page for this block, so allocate a new page
                        {
                            List<long> list = pageSizes[pageSizeIndex];
                            if (list == null)
                            {
                                list = new List<long>();
                                pageSizes[pageSizeIndex] = list;
                            }

                            int pageIndex = list.Count;
                            list.Add(size);
                            blockPages[block] = (pageSizeIndex, pageIndex, 0);
                        }
                    }
                }

                bool testOk = true;
                uint totalPageCount = 0u;
                for (int i = 0; i < 5; i++)
                {
                    uint pc = (uint)(pageSizes[i]?.Count ?? 0);
                    pageCounts[i] = pc;
                    totalPageCount += pc;
                }

                if (totalPageCount > maxPageCount) testOk = false;
                if (pageCounts[0] > 0x7F) testOk = false;
                if (pageCounts[1] > 0x3F) testOk = false;
                if (pageCounts[2] > 0xF) testOk = false;
                if (pageCounts[3] > 0x3) testOk = false;
                if (pageCounts[4] > 0x1) testOk = false;
                if (testOk) break; //everything fits, so we're done here
                if (baseShift >= 0xF) throw new Exception("Unable to pack blocks with largest possible base!");
                baseShift++;
                baseSize = 0x2000 << baseShift;
            }


            long pageOffset = 0L; //pages are allocated, assign actual positions
            long[] pageOffsets = new long[5]; //base offsets for each page size
            for (int i = 4; i >= 0; i--)
            {
                pageOffsets[i] = pageOffset;
                long pageSize = baseSize * (1 << i);
                uint pageCount = pageCounts[i];
                pageOffset += pageSize * pageCount;
            }

            foreach (KeyValuePair<IResourceBlock, (int, int, long)> kvp in blockPages)
            {
                IResourceBlock block = kvp.Key;
                int pageSizeIndex = kvp.Value.Item1;
                int pageIndex = kvp.Value.Item2;
                long offset = kvp.Value.Item3;
                long pageSize = baseSize * (1 << pageSizeIndex);
                long blockPosition = pageOffsets[pageSizeIndex] + pageSize * pageIndex + offset;
                block.FilePosition = basePosition + blockPosition;
            }


            uint v = (uint)baseShift & 0xF;
            v += (pageCounts[4] & 0x1) << 4;
            v += (pageCounts[3] & 0x3) << 5;
            v += (pageCounts[2] & 0xF) << 7;
            v += (pageCounts[1] & 0x3F) << 11;
            v += (pageCounts[0] & 0x7F) << 17;
            pageFlags = new RpfResourcePageFlags(v);
        }


        public static byte[] Build(ResourceFileBase fileBase, int version, bool compress = true, bool gen9 = false)
        {
            fileBase.FilePagesInfo = new ResourcePagesInfo();

            IList<IResourceBlock> systemBlocks;
            IList<IResourceBlock> graphicBlocks;
            GetBlocks(fileBase, out systemBlocks, out graphicBlocks);

            //AssignPositions(systemBlocks, 0x50000000, out var systemPageFlags, 128);
            //AssignPositions(graphicBlocks, 0x60000000, out var graphicsPageFlags, 128 - systemPageFlags.Count);

            AssignPositions2(systemBlocks, 0x50000000, out RpfResourcePageFlags systemPageFlags, 128, gen9);
            AssignPositions2(graphicBlocks, 0x60000000, out RpfResourcePageFlags graphicsPageFlags, 128 - systemPageFlags.Count, gen9);


            fileBase.FilePagesInfo.SystemPagesCount = (byte)systemPageFlags.Count;
            fileBase.FilePagesInfo.GraphicsPagesCount = (byte)graphicsPageFlags.Count;


            MemoryStream systemStream = new MemoryStream();
            MemoryStream graphicsStream = new MemoryStream();
            ResourceDataWriter resourceWriter = new ResourceDataWriter(systemStream, graphicsStream);
            resourceWriter.IsGen9 = gen9;

            resourceWriter.Position = 0x50000000;
            foreach (IResourceBlock block in systemBlocks)
            {
                resourceWriter.Position = block.FilePosition;

                long pos_before = resourceWriter.Position;
                block.Write(resourceWriter);
                long pos_after = resourceWriter.Position;
                long blen = resourceWriter.IsGen9 ? block.BlockLength_Gen9 : block.BlockLength;

                if (pos_after - pos_before != blen) throw new Exception("error in system length");
            }

            resourceWriter.Position = 0x60000000;
            foreach (IResourceBlock block in graphicBlocks)
            {
                resourceWriter.Position = block.FilePosition;

                long pos_before = resourceWriter.Position;
                block.Write(resourceWriter);
                long pos_after = resourceWriter.Position;
                long blen = resourceWriter.IsGen9 ? block.BlockLength_Gen9 : block.BlockLength;

                if (pos_after - pos_before != blen) throw new Exception("error in graphics length");
            }


            int sysDataSize = (int)systemPageFlags.Size;
            byte[] sysData = new byte[sysDataSize];
            systemStream.Flush();
            systemStream.Position = 0;
            systemStream.Read(sysData, 0, (int)systemStream.Length);


            int gfxDataSize = (int)graphicsPageFlags.Size;
            byte[] gfxData = new byte[gfxDataSize];
            graphicsStream.Flush();
            graphicsStream.Position = 0;
            graphicsStream.Read(gfxData, 0, (int)graphicsStream.Length);


            uint uv = (uint)version;
            uint sv = (uv >> 4) & 0xF;
            uint gv = (uv >> 0) & 0xF;
            uint sf = systemPageFlags.Value + (sv << 28);
            uint gf = graphicsPageFlags.Value + (gv << 28);


            int tdatasize = sysDataSize + gfxDataSize;
            byte[] tdata = new byte[tdatasize];
            Buffer.BlockCopy(sysData, 0, tdata, 0, sysDataSize);
            Buffer.BlockCopy(gfxData, 0, tdata, sysDataSize, gfxDataSize);


            byte[] cdata = compress ? Compress(tdata) : tdata;


            int dataSize = 16 + cdata.Length;
            byte[] data = new byte[dataSize];

            byte[] h1 = BitConverter.GetBytes((uint)0x37435352);
            byte[] h2 = BitConverter.GetBytes(version);
            byte[] h3 = BitConverter.GetBytes(sf);
            byte[] h4 = BitConverter.GetBytes(gf);
            Buffer.BlockCopy(h1, 0, data, 0, 4);
            Buffer.BlockCopy(h2, 0, data, 4, 4);
            Buffer.BlockCopy(h3, 0, data, 8, 4);
            Buffer.BlockCopy(h4, 0, data, 12, 4);
            Buffer.BlockCopy(cdata, 0, data, 16, cdata.Length);

            return data;
        }


        public static byte[] AddResourceHeader(RpfResourceFileEntry entry, byte[] data)
        {
            if (data == null) return null;
            byte[] newdata = new byte[data.Length + 16];
            byte[] h1 = BitConverter.GetBytes((uint)0x37435352);
            byte[] h2 = BitConverter.GetBytes(entry.Version);
            byte[] h3 = BitConverter.GetBytes(entry.SystemFlags);
            byte[] h4 = BitConverter.GetBytes(entry.GraphicsFlags);
            Buffer.BlockCopy(h1, 0, newdata, 0, 4);
            Buffer.BlockCopy(h2, 0, newdata, 4, 4);
            Buffer.BlockCopy(h3, 0, newdata, 8, 4);
            Buffer.BlockCopy(h4, 0, newdata, 12, 4);
            Buffer.BlockCopy(data, 0, newdata, 16, data.Length);
            return newdata;
        }


        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true);
                ds.Write(data, 0, data.Length);
                ds.Close();
                byte[] deflated = ms.GetBuffer();
                byte[] outbuf = new byte[ms.Length]; //need to copy to the right size buffer...
                Array.Copy(deflated, outbuf, outbuf.Length);
                return outbuf;
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
                MemoryStream outstr = new MemoryStream();
                ds.CopyTo(outstr);
                byte[] deflated = outstr.GetBuffer();
                byte[] outbuf = new byte[outstr.Length]; //need to copy to the right size buffer...
                Array.Copy(deflated, outbuf, outbuf.Length);
                return outbuf;
            }
        }

        public class ResourceBuilderBlock
        {
            public IResourceBlock Block;
            public long Length;

            public ResourceBuilderBlock(IResourceBlock block)
            {
                Block = block;
                Length = block?.BlockLength ?? 0;
            }
        }

        public class ResourceBuilderBlockSet
        {
            public Dictionary<ResourceBuilderBlock, LinkedListNode<ResourceBuilderBlock>> BlockDict =
                new Dictionary<ResourceBuilderBlock, LinkedListNode<ResourceBuilderBlock>>();

            public LinkedList<ResourceBuilderBlock> BlockList = new LinkedList<ResourceBuilderBlock>();
            public bool IsSystemSet;
            public ResourceBuilderBlock RootBlock;

            public ResourceBuilderBlockSet(IList<IResourceBlock> blocks, bool sys)
            {
                IsSystemSet = sys;
                if (sys && blocks.Count > 0) RootBlock = new ResourceBuilderBlock(blocks[0]);
                List<ResourceBuilderBlock> list = new List<ResourceBuilderBlock>();
                int start = sys ? 1 : 0;
                for (int i = start; i < blocks.Count; i++)
                {
                    ResourceBuilderBlock bb = new ResourceBuilderBlock(blocks[i]);
                    list.Add(bb);
                }

                list.Sort((a, b) => b.Length.CompareTo(a.Length));
                foreach (ResourceBuilderBlock bb in list)
                {
                    LinkedListNode<ResourceBuilderBlock> ln = BlockList.AddLast(bb);
                    BlockDict[bb] = ln;
                }
            }

            public int Count => BlockList.Count;

            public ResourceBuilderBlock FindBestBlock(long maxSize)
            {
                LinkedListNode<ResourceBuilderBlock> n = BlockList.First;
                while (n != null && n.Value.Length > maxSize) n = n.Next;
                return n?.Value;
            }

            public ResourceBuilderBlock TakeBestBlock(long maxSize)
            {
                ResourceBuilderBlock r = FindBestBlock(maxSize);
                if (r != null)
                    if (BlockDict.TryGetValue(r, out LinkedListNode<ResourceBuilderBlock> ln))
                    {
                        BlockList.Remove(ln);
                        BlockDict.Remove(r);
                    }

                return r;
            }
        }
    }
}