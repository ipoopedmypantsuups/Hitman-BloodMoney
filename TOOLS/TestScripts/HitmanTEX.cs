using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Skript47
{
    public class HitmanTEX
    {
        public int[] head = new int[4];
        public byte[] frame = new byte[0];
        public List<Block> _block = new List<Block>();
        public List<BlockData> _blockData = new List<BlockData>();
        public string path = string.Empty;

        public HitmanTEX(byte[] data)
        {
            using (var br = new BinaryReader(new MemoryStream(data)))
            {
                for (int i = 0; i < head.Length; i++)
                {
                    head[i] = br.ReadInt32();
                }
                br.BaseStream.Position = 0;
                frame = br.ReadBytes(head[0]);

                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    var _pack = new int[2048];
                    for (int i = 0; i < _pack.Length; i++)
                    {
                        _pack[i] = br.ReadInt32();
                        if (_pack[i] != 0)
                        {
                            var b = new byte[BitConverter.ToInt32(data, _pack[i])];
                            Array.Copy(data, _pack[i], b, 0, b.Length);
                            _block.Add(new Block(b) { pos = _pack[i] });
                        }
                        else
                        {
                            _block.Add(new Block(new byte[0]));
                        }
                    }
                    _pack = new int[2048];
                    for (int i = 0; i < _pack.Length; i++)
                    {
                        _pack[i] = br.ReadInt32();
                        if (_pack[i] != 0)
                        {
                            var b = new byte[BitConverter.ToInt32(data, _pack[i]) * 4 + 4];
                            Array.Copy(data, _pack[i], b, 0, b.Length);
                            _blockData.Add(new BlockData(b) { pos = _pack[i] });
                        }
                        else
                        {
                            _blockData.Add(new BlockData(new byte[0]));
                        }
                    }
                }

                else
                {
                    br.BaseStream.Position = 16;
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        var s = br.ReadInt32();
                        var pos = br.BaseStream.Position -= 4;
                        _block.Add(new Block(br.ReadBytes(s)) { pos = (int)pos });
                        br.ReadBytes(16 - (int)br.BaseStream.Position % 16);

                    }
                }
            }
        }

        public void SaveAsFile(string dir, string name, bool toDDS, bool open, bool writeComment, int id)
        {
            if (toDDS)
            {
                var image = new GameImage() { width = _block[id].width, height = _block[id].height, body = _block[id].body, type = _block[id].typeDDS() };
                if (_block[id].type == "8V8U")
                {
                    image.type = "A8B8G8R8";
                    image.body = _block[id].NormalMapFromAG();
                }
                if (_block[id].type == "NLAP")
                {
                    image.type = "A8B8G8R8";
                    image.body = new[] { _block[id].ImageFromNLAP() };
                }
                var path = string.Format(@"{0}/{1} {2} {3} {4}.dds", dir, name, id, string.Join("-", _block[id].flag), _block[id].comment.Replace("/", "+"));
                File.WriteAllBytes(path, image.ToDDS(999));
                if (open)
                {
                    System.Diagnostics.Process.Start(path);
                }
            }
            else
            {
                var path = string.Format(@"{0}/{1} {2} {3} {4}.HitmanIMG", dir, id, string.Join("-", _block[id].flag), _block[id].comment.Replace("/", "+"));
                File.WriteAllBytes(path, _block[id].backUP);
            }
            if (writeComment)
            {
                var path = string.Format(@"{0}/{1} {2}.txt", dir, name, id);
                File.WriteAllText(path, _block[id].comment);
            }
        }

        public void ImportFile(byte[] data, string line, int id, out string done)
        {
            var image = new GameImage(data, line);
            if (image.type != "Unknown")
            {
                if (_block[id].type != "NLAP")
                {
                    _block[id].comment = line;
                    _block[id].width = image.width;
                    _block[id].height = image.height;
                    _block[id].body = image.body;
                    if (image.type == "DXT1")
                    {
                        _block[id].type = "1TXD";
                    }
                    if (image.type == "DXT3")
                    {
                        _block[id].type = "3TXD";
                    }
                    if (image.type == "DXT5")
                    {
                        _block[id].type = "1TXD";
                        _block[id].body = _block[id].DXT1MapFromDXT5();
                    }
                    if (image.type == "L8")
                    {
                        _block[id].type = "  8I";
                    }
                    if (image.type == "A8R8G8B8")
                    {
                        if (_block[id].type == "8V8U")
                        {
                            _block[id].body = _block[id].NormalMapToAG();
                        }
                        else
                        {
                            _block[id].type = "ABGR";
                        }
                    }
                    done = "";
                }
                else
                {
                    done = "NLAP type cant be replaced!";
                }
            }
            else
            {
                done = "Unknown DDS file type!";
            }
        }

        public class BlockData
        {
            public int pos = 0;
            public int[] unk = new int[0];
            public byte[] backUP;

            public BlockData(byte[] data)
            {
                if (data.Length > 0)
                {
                    backUP = data;
                    using (var br = new BinaryReader(new MemoryStream(data)))
                    {
                        unk = new int[br.ReadInt32()];
                        for (int i = 0; i < unk.Length; i++)
                        {
                            unk[i] = br.ReadInt32();
                        }
                        backUP = new byte[4 + unk.Length * 4];
                        Array.Copy(data, pos, backUP, 0, backUP.Length);
                    }
                }
            }
        }

        public class Block
        {
            public int pos;
            public int size;
            public int ID;
            public int width;
            public int height;
            public UInt16[] flag = new UInt16[6];
            public string type;
            public string comment;
            public byte[][] body = new byte[0][];
            public byte[] pal;
            public byte[] backUP;

            public string GetLine()
            {
                var result = string.Format("{0}({1})", ID, string.Join("_", flag));
                return result;
            }

            public string typeTEX()
            {
                if (flag[0] == 0)
                {
                    return "Diff";
                }
                if (flag[0] == 2)
                {
                    return "HUD";
                }
                if (flag[0] == 64)
                {
                    return "Normal";
                }
                if (flag[0] == 1024)
                {
                    return "Cubemap";
                }
                else
                {
                    return "Unknown";
                }
            }

            public string typeDDS()
            {
                if (type == "1TXD")
                {
                    return "DXT1";
                }
                else if (type == "3TXD")
                {
                    return "DXT3";
                }
                else if (type == "8V8U")
                {
                    return "A8B8G8R8";
                }
                else if (type == "NLAP")
                {
                    return "A8B8G8R8";
                }
                else if (type == "  8I")
                {
                    return "L8";
                }
                else if (type == "ABGR")
                {
                    return "A8B8G8R8";
                }
                else
                {
                    return "Unknown";
                }
            }

            public Block(byte[] data)
            {
                if (data.Length > 0)
                {
                    backUP = data;
                    using (var br = new BinaryReader(new MemoryStream(data)))
                    {
                        size = br.ReadInt32();
                        type = Encoding.Default.GetString(br.ReadBytes(4));
                        br.ReadBytes(4);
                        ID = br.ReadInt32();
                        height = br.ReadInt16();
                        width = br.ReadInt16();
                        body = new byte[br.ReadInt32()][];
                        for (int i = 0; i < flag.Length; i++)
                        {
                            flag[i] = br.ReadUInt16();
                        }
                        var str = new StringBuilder();
                        byte charX;
                        while ((charX = br.ReadByte()) != 0)
                        {
                            str.Append(Encoding.Default.GetString(new byte[] { charX }));
                        }
                        comment = str.ToString();
                        for (int i = 0; i < body.Length; i++)
                        {
                            body[i] = br.ReadBytes(br.ReadInt32());
                        }
                        if (type == "NLAP")
                        {
                            pal = br.ReadBytes(br.ReadInt32() * 4);
                        }
                        backUP = data;
                    }
                }
            }

            public byte[][] ColorsFromNLAP()
            {
                var result = new byte[pal.Length / 4][];
                using (var br = new BinaryReader(new MemoryStream(pal)))
                {
                    for (int i = 0; i < result.Length; i++) { result[i] = br.ReadBytes(4); }
                }
                return result;
            }

            public byte[] ImageFromNLAP()
            {
                var result = new byte[body.Length];
                var colors = ColorsFromNLAP();
                using (var br = new BinaryReader(new MemoryStream(body[0])))
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var bw = new BinaryWriter(ms))
                        {
                            for (int i = 0; i < body[0].Length; i++)
                            {
                                bw.Write(colors[br.ReadByte()]);
                            }
                        }
                        return ms.ToArray();
                    }
                }
            }

            public byte[][] DXT1MapFromDXT5()
            {
                var result = new byte[body.Length][];
                for (int j = 0; j < body.Length; j++)
                {
                    using (var br = new BinaryReader(new MemoryStream(body[j])))
                    {
                        using (var ms = new MemoryStream())
                        {
                            using (var bw = new BinaryWriter(ms))
                            {
                                for (int i = 0; i < body[j].Length / 16; i++)
                                {
                                    br.ReadBytes(8);
                                    bw.Write(br.ReadBytes(8));
                                }
                                result[j] = ms.ToArray();
                            }
                        }
                    }
                }
                return result;
            }

            public byte[][] NormalMapFromAG()
            {
                var result = new byte[body.Length][];
                for (int j = 0; j < body.Length; j++)
                {
                    using (var br = new BinaryReader(new MemoryStream(body[j])))
                    {
                        using (var ms = new MemoryStream())
                        {
                            using (var bw = new BinaryWriter(ms))
                            {
                                for (int i = 0; i < body[j].Length / 2; i++)
                                {
                                    var A = br.ReadByte();
                                    var G = br.ReadByte();
                                    bw.Write(new byte[] { 255, A, G, 255 });
                                }
                                result[j] = ms.ToArray();
                            }

                        }
                    }
                }
                return result;
            }

            public byte[][] NormalMapToAG()
            {
                var result = new byte[body.Length][];
                for (int j = 0; j < body.Length; j++)
                {
                    using (var br = new BinaryReader(new MemoryStream(body[j])))
                    {
                        using (var ms = new MemoryStream())
                        {
                            using (var bw = new BinaryWriter(ms))
                            {
                                for (int i = 0; i < body[j].Length / 4; i++)
                                {
                                    var R = br.ReadByte();
                                    var G = br.ReadByte();
                                    var B = br.ReadByte();
                                    var A = br.ReadByte();
                                    bw.Write(new byte[] { G, B });
                                }
                                result[j] = ms.ToArray();
                            }
                        }
                    }
                }
                return result;
            }

            public void BumpClean()
            {
                if (type == "8V8U")
                {
                    width = 1;
                    height = 1;
                    body = new byte[1][] { new byte[] { 0x82, 0x7D, 0x82, 0x7D, } };
                }
            }

            public void SetColor(Color c)
            {
                type = "ABGR";
                width = 1;
                height = 1;
                body = new byte[1][] { new byte[] { (byte)c.B, (byte)c.G, (byte)c.R, (byte)c.A } };
            }

            public byte[] Build()
            {
                var result = new byte[0];
                var ms = new MemoryStream();
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(0);
                    bw.Write(Encoding.Default.GetBytes(type));
                    bw.Write(Encoding.Default.GetBytes(type));
                    bw.Write(ID);
                    bw.Write((Int16)height);
                    bw.Write((Int16)width);
                    bw.Write(body.Length);
                    for (int i = 0; i < flag.Length; i++)
                    {
                        bw.Write(flag[i]);
                    }
                    bw.Write(Encoding.Default.GetBytes(comment));
                    bw.Write((byte)0);
                    for (int i = 0; i < body.Length; i++)
                    {
                        bw.Write(body[i].Length);
                        bw.Write(body[i]);
                    }
                    if (type == "NLAP")
                    {
                        bw.Write(pal.Length / 4);
                        bw.Write(pal);
                    }
                    bw.Seek(0, SeekOrigin.Begin);
                    bw.Write((int)bw.BaseStream.Length);
                }
                result = ms.ToArray();
                return result;
            }
        }

        public byte[] Build()
        {
            var result = new byte[0];
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            {
                for (int i = 0; i < head.Length; i++)
                {
                    bw.Write(head[i]);
                }
                for (int i = 0; i < _block.Count; i++)
                {
                    if (_block[i].backUP != null)
                    {
                        _block[i].pos = (int)bw.BaseStream.Position;
                        _block[i].ID = i;
                        bw.Write(_block[i].Build());
                        while (bw.BaseStream.Position % 16 != 0)
                        {
                            bw.Write((byte)0);
                        }
                    }
                }
                for (int j = 0; j < _blockData.Count; j++)
                {
                    if (_blockData[j].backUP != null)
                    {
                        _blockData[j].pos = (int)bw.BaseStream.Position;
                        bw.Write(_blockData[j].backUP);
                        while (bw.BaseStream.Position % 16 != 0)
                        {
                            bw.Write((byte)0);
                        }
                    }
                }
                var offset = (int)bw.BaseStream.Position;
                for (int i = 0; i < _block.Count; i++)
                {
                    bw.Write(_block[i].pos);
                }
                for (int i = 0; i < _blockData.Count; i++)
                {
                    bw.Write(_blockData[i].pos);
                }
                bw.Seek(0, SeekOrigin.Begin);
                bw.Write(offset);
                bw.Write(offset + 8192);
                result = ms.ToArray();
            }
            return result;
        }

    }
}