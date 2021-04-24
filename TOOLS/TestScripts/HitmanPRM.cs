using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Skript47
{
    public class HitmanPRM
    {
        public void SaveAsFile (string dir, int id)
        {
            if (_block[id].tar != null)
            {
                if (_block[id].mesh)
                {
                    var countV = _block[id].tar[0];
                    var blockV = _block[id].tar[1];
                    var blockF = _block[id].tar[3];
                    var meshHBM = new HitmanPRM.MeshHBM(_block[blockV].data, _block[blockF].data, countV);
                    File.WriteAllText(string.Format(@"{0}/{1} {2}.obj", dir, Path.GetFileNameWithoutExtension(path), id), meshHBM.ToOBJ());
                }
            }
        }

        public static float[] ColorToFloat(byte[] signed)
        {
            return new float[] { ((float)signed[2] + 1f) / 128f - 1, ((float)signed[1] + 1f) / 128f - 1, ((float)signed[0] + 1f) / 128f - 1 };
        }

        public void ImportMesh(string path)
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(path)))
            {
                using (var br = new BinaryReader(ms))
                {
                    var way = br.ReadString();
                    var num = br.ReadInt32();
                    var countV = br.ReadInt32();
                    var ver = br.ReadBytes(br.ReadInt32());
                    var ind = br.ReadBytes(br.ReadInt32());
                    _block[num].tar[0] = countV;
                    _block[_block[num].tar[1]].data = ver;
                    _block[_block[num].tar[3]].data = ind;
                }
            }
        }

        public void RemoveMesh(int num)
        {
            var countV = _block[num].tar[0];
            var blockV = _block[num].tar[1];
            var blockF = _block[num].tar[3];
            _block[blockF].data = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, };
        }

        public void ImportOBJ(int num, string path)
        {
            var countV = _block[num].tar[0];
            var blockV = _block[num].tar[1];
            var blockF = _block[num].tar[3];
            var meshHBM = new HitmanPRM.MeshHBM(_block[blockV].data, _block[blockF].data, countV);

            var OBJ = new OBJ(path);
            var vertexList = OBJ.BuildVertexArray(0, false);

            meshHBM._p = new float[vertexList.Length][];
            meshHBM._t = new float[vertexList.Length][];
            meshHBM._a1 = new int[vertexList.Length][];
            meshHBM._a2 = new int[vertexList.Length][];
            for (int i = 0; i < vertexList.Length; i++)
            {
                // P
                meshHBM._p[i] = new[] { vertexList[i][0], vertexList[i][1], vertexList[i][2] };
                // N
                var res = new byte[3];
                res[0] = (byte)(((vertexList[i][3] + 1) * 127) + 1);
                res[1] = (byte)(((vertexList[i][4] + 1) * 127) + 1);
                res[2] = (byte)(((vertexList[i][5] + 1) * 127) + 1);
                var nh = BitConverter.ToInt32(new byte[] { (byte)res[2], (byte)res[1], (byte)res[0], 0 }, 0);
                meshHBM._a1[i] = new[] { nh, -1 };

                // T
                meshHBM._t[i] = new[] { vertexList[i][6], vertexList[i][7] };
                // N2?
                meshHBM._a2[i] = new[] { 8421888, 8388732, 0 };
            }

            meshHBM._face = OBJ.BuildIndexArray(0);

            _block[blockF].data = meshHBM.BuildIndexArray();
            _block[blockV].data = meshHBM.BuildVertexArray();
            _block[num].tar[0] = meshHBM._p.Length;
        }

        public class Block
        {
            public uint pos;
            public uint size;
            public uint unk1;
            public uint unk2;
            public byte[] data;
            public int[] tar;
            public int stepV;
            public bool mesh = false;

            public void CopyHEX()
            {
                if (data.Length > 0)
                {
                    Clipboard.SetText(BitConverter.ToString(data).Replace("-", " "));
                    if (data.Length > 600)
                    {
                        MessageBox.Show(BitConverter.ToString(data).Replace("-", " ").Remove(600));
                    }
                    else
                    {
                        MessageBox.Show(BitConverter.ToString(data).Replace("-", " ").Replace("-", " "));
                    }
                }
            }
        };

        public class MeshHBM
        {
            public int step;
            public float[][] _p;
            public float[][] _n;
            public int[][] _a1;
            public float[][] _t;
            public int[][] _a2;
            public int[][] _face;

            public MeshHBM(byte[] dataA, byte[] dataB, int vCount)
            {
                using (var ms = new MemoryStream(dataA))
                {
                    using (var br = new BinaryReader(ms))
                    {
                        step = dataA.Length / vCount;
                        _p = new float[vCount][];
                        if (step == 16)
                        {
                            _p = new float[vCount][];
                            for (int i = 0; i < vCount; i++)
                            {
                                _p[i] = new[] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                                br.ReadInt32();
                            }
                        }
                        if (step == 36)
                        {
                            _t = new float[vCount][];
                            for (int i = 0; i < vCount; i++)
                            {
                                _p[i] = new[] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                                _t[i] = new[] { br.ReadSingle(), -br.ReadSingle() };
                                br.ReadInt32();
                                br.ReadInt32();
                                br.ReadSingle();
                                br.ReadSingle();
                            }
                        }
                        if (step == 40)
                        {
                            _t = new float[vCount][];
                            _a1 = new int[vCount][];
                            _a2 = new int[vCount][];
                            for (int i = 0; i < vCount; i++)
                            {
                                _p[i] = new[] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                                _a1[i] = new[] { br.ReadInt32(), br.ReadInt32() };
                                _t[i] = new[] { br.ReadSingle(), -br.ReadSingle() };
                                _a2[i] = new[] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
                            }
                        }
                        if (step == 52)
                        {
                            _n = new float[vCount][];
                            _t = new float[vCount][];
                            _a1 = new int[vCount][];
                            _a2 = new int[vCount][];
                            for (int i = 0; i < vCount; i++)
                            {
                                _p[i] = new[] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                                _n[i] = new[] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                                _a1[i] = new[] { br.ReadInt32(), br.ReadInt32() };
                                _t[i] = new[] { br.ReadSingle(), br.ReadSingle() };
                                _a2[i] = new[] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
                            }
                        }
                    }
                }
                using (var ms = new MemoryStream(dataB))
                {
                    using (var br = new BinaryReader(ms))
                    {
                        br.ReadInt16();
                        _face = new int[br.ReadInt16() / 3][];
                        for (int i = 0; i < _face.Length; i++)
                        {
                            _face[i] = new int[3];
                            _face[i][2] = (Int16)(br.ReadInt16() + 1);
                            _face[i][1] = (Int16)(br.ReadInt16() + 1);
                            _face[i][0] = (Int16)(br.ReadInt16() + 1);
                        }
                    }
                }
            }

            public string ToOBJ()
            {
                var OBJ = new StringBuilder();
                OBJ.AppendLine("#Step " + step.ToString());
                foreach (var element in _p)
                {
                    if (element != null)
                    {
                        OBJ.AppendLine(string.Format("v {0}", string.Join(" ", Array.ConvertAll(element, x => x.ToString("0.000000")))));
                    }
                }
                if (_n != null)
                {
                    foreach (var element in _n)
                    {
                        OBJ.AppendLine(string.Format("vn {0}", string.Join(" ", Array.ConvertAll(element, x => x.ToString("0.000000")))));
                    }
                }
                if (_t != null)
                {
                    foreach (var element in _t)
                    {
                        OBJ.AppendLine(string.Format("vt {0}", string.Join(" ", Array.ConvertAll(element, x => x.ToString("0.000000")))));
                    }
                }
                if (_a1 != null & _a2 != null)
                {
                    for (int i = 0; i < _a1.Length; i++)
                    {
                        var signed = BitConverter.GetBytes(_a1[i][0]);
                        var signedS0 = HitmanPRM.ColorToFloat(signed);
                        OBJ.Append(string.Format("vn {0}", string.Join(" ", Array.ConvertAll(signedS0, x => x.ToString("0.000000")))));
                        OBJ.AppendLine();

                        signed = BitConverter.GetBytes(_a2[i][0]);
                        signedS0 = HitmanPRM.ColorToFloat(signed);

                        //OBJ.Append(string.Format("vn {0}", string.Join(" ", Array.ConvertAll(signedS0, x => x.ToString("0.000000")))));
                        OBJ.AppendLine();

                        signed = BitConverter.GetBytes(_a2[i][1]);
                        signedS0 = HitmanPRM.ColorToFloat(signed);

                      //OBJ.Append(string.Format("vn {0}", string.Join(" ", Array.ConvertAll(signedS0, x => x.ToString("0.000000")))));

                        OBJ.AppendLine();
                    }
                }
                OBJ.AppendLine("g mesh");
                var lineForm = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
                if (_a1 == null)
                {
                    lineForm = "f {0}/{0} {1}/{1} {2}/{2}";
                }
                if (_t == null)
                {
                    lineForm = "f {0} {1} {2}";
                }
                foreach (var element in _face)
                {
                    OBJ.AppendLine(string.Format(lineForm, element[0], element[1], element[2]));
                }
                return OBJ.ToString();
            }

            public byte[] BuildVertexArray()
            {
                using (var ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms))
                    {
                        for (int i = 0; i < _p.Length; i++)
                        {
                            bw.Write(_p[i][0]);
                            bw.Write(_p[i][1]);
                            bw.Write(_p[i][2]);
                            if (_a1 != null)
                            {
                                bw.Write(_a1[i][0]);
                                bw.Write(_a1[i][1]);
                            }
                            if (_t != null)
                            {
                                bw.Write(_t[i][0]);
                                bw.Write(-_t[i][1]);
                            }
                            if (_a2 != null)
                            {
                                bw.Write(new byte[] { 0x00, 0x7F, 0x7F, 0x00 });
                                bw.Write(new byte[] { 0x7F, 0x7F, 0x00, 0x00 });
                                bw.Write(new byte[] { 0xCD, 0xCD, 0xCD, 0xCD });
                            }
                        }
                        return ms.ToArray();
                    }
                }
            }

            public byte[] BuildIndexArray()
            {
                using (var ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((Int16)1);
                        bw.Write((Int16)(_face.Length * 3));
                        for (int i = 0; i < _face.Length; i++)
                        {
                            bw.Write((Int16)_face[i][2]);
                            bw.Write((Int16)_face[i][1]);
                            bw.Write((Int16)_face[i][0]);
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        public Block[] _block;
        public int modelnum;
        public byte[] body;
        public int meshCount = 0;
        public string path = string.Empty;

        public HitmanPRM(byte[] data)
        {
            var body = data;
            using (var br = new BinaryReader(new MemoryStream(data)))
            {
                var ChunkPos = br.ReadUInt32();
                var ChunkNum = br.ReadUInt32();
                var ChunkPos2 = br.ReadUInt32();
                var Zero = br.ReadUInt32();
                br.BaseStream.Position = ChunkPos;
                _block = new Block[ChunkNum];
                for (int i = 0; i < _block.Length; i++)
                {
                    _block[i] = new Block();
                    _block[i].pos = br.ReadUInt32();
                    _block[i].size = br.ReadUInt32();
                    _block[i].unk1 = br.ReadUInt32();
                    _block[i].unk2 = br.ReadUInt32();
                    _block[i].data = new byte[_block[i].size];
                    Array.Copy(body, _block[i].pos, _block[i].data, 0, (int)_block[i].size);
                    if (_block[i].size == 0x10)
                    {
                        _block[i].tar = new int[4];
                        _block[i].tar[0] = BitConverter.ToInt32(_block[i].data, 0);
                        _block[i].tar[1] = BitConverter.ToInt32(_block[i].data, 4);
                        _block[i].tar[2] = BitConverter.ToInt32(_block[i].data, 8);
                        _block[i].tar[3] = BitConverter.ToInt32(_block[i].data, 12);
                        if (_block[i].tar[0] != 0 & _block[i].tar[1] != 0 & _block[i].tar[3] != 0 & _block[i].unk1 == 1)
                        {
                            if (_block[i].tar[1] < _block.Length & _block[i].tar[3] < _block.Length)
                            {
                                _block[i].stepV = _block[_block[i].tar[1]].data.Length / _block[i].tar[0];
                                if (_block[i].stepV > 12)
                                {
                                    _block[i].mesh = true;
                                    meshCount++;
                                }
                            }
                        }
                    }
                }
            }
        }

        public byte[] Build()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    var ChunkPos = _block.Select(x => x.data.Length).Sum();
                    bw.Write(ChunkPos);
                    bw.Write(_block.Length);
                    bw.Write(ChunkPos);
                    bw.Write(0);
                    _block[0].data = ms.ToArray();
                }
            }
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    uint offset = 0;
                    for (int i = 0; i < _block.Length; i++)
                    {
                        if (!_block[i].mesh)
                        {
                            bw.Write(_block[i].data);
                        }
                        else
                        {
                            bw.Write(_block[i].tar[0]);
                            bw.Write(_block[i].tar[1]);
                            bw.Write(_block[i].tar[2]);
                            bw.Write(_block[i].tar[3]);
                        }
                        _block[i].size = (uint)_block[i].data.Length;
                        _block[i].pos = offset;
                        offset += _block[i].size;
                    }
                    foreach (var element in _block)
                    {
                        bw.Write(element.pos);
                        bw.Write(element.size);
                        bw.Write(element.unk1);
                        bw.Write(element.unk2);
                    }
                    return ms.ToArray();
                }
            }
        }
    }

    public class OBJ
    {
        public Vertex _vertex = new Vertex();
        public List<Group> _group = new List<Group>();
        static readonly Regex regvp = new Regex(@"^v\s", RegexOptions.Compiled);
        static readonly Regex regvn = new Regex(@"^vn\s", RegexOptions.Compiled);
        static readonly Regex regvt = new Regex(@"^vt\s", RegexOptions.Compiled);
        static readonly Regex regg = new Regex(@"^g\s", RegexOptions.Compiled);
        static readonly Regex regf = new Regex(@"^f" + @"\s+(\d+)/(\d+)/(\d+)" + @"\s+(\d+)/(\d+)/(\d+)" + @"\s+(\d+)/(\d+)/(\d+)", RegexOptions.Compiled);
        static char[] space = new[] { ' ' };

        public class Vertex
        {
            public List<float[]> P = new List<float[]>();
            public List<float[]> N = new List<float[]>();
            public List<float[]> T = new List<float[]>();
        }

        public class Group
        {
            public string name;
            public string usemtl;
            public Face _face = new Face();

            public class Face
            {
                public List<int[]> V = new List<int[]>();
                public List<int[]> N = new List<int[]>();
                public List<int[]> T = new List<int[]>();
            }

            public Group(string g)
            {
                name = g;
            }

            public Group(string g, List<int[]> _faceV, List<int[]> _faceN, List<int[]> _faceT)
            {
                name = g;
                _face.V = _faceV;
                _face.N = _faceN;
                _face.T = _faceT;
            }
        }

        public OBJ(List<float[]> _vertexP, List<float[]> _vertexN, List<float[]> _vertexT, Group tempG)
        {
            _vertex.P = _vertexP;
            _vertex.N = _vertexN;
            _vertex.T = _vertexT;
            _group.Add(tempG);
        }

        public OBJ(string path)
        {
            var data = File.ReadAllLines(path);
            var SSO_REE = StringSplitOptions.RemoveEmptyEntries;
            foreach (var line in data)
            {
                if (regvp.Match(line).Success)
                {
                    var d = line.Split(space, SSO_REE);
                    _vertex.P.Add(new[] { float.Parse(d[1]), float.Parse(d[2]), float.Parse(d[3]) });
                }
                else if (regvn.Match(line).Success)
                {
                    var d = line.Split(space, SSO_REE);
                    _vertex.N.Add(new[] { float.Parse(d[1]), float.Parse(d[2]), float.Parse(d[3]) });
                }
                else if (regvt.Match(line).Success)
                {
                    var d = line.Split(space, SSO_REE);
                    _vertex.T.Add(new[] { float.Parse(d[1]), float.Parse(d[2]) });
                }
                else if (regg.Match(line).Success)
                {
                    var d = line.Split(space, SSO_REE);
                    _group.Add(new Group(d[1]));
                }
                else
                {
                    var m0 = regf.Match(line);
                    if (m0.Success)
                    {
                        var d = line.Replace("/", " ").Split(space, SSO_REE);
                        _group[_group.Count - 1]._face.V.Add(new[] { int.Parse(d[1]) - 1, int.Parse(d[4]) - 1, int.Parse(d[7]) - 1 });
                        _group[_group.Count - 1]._face.T.Add(new[] { int.Parse(d[2]) - 1, int.Parse(d[5]) - 1, int.Parse(d[8]) - 1 });
                        _group[_group.Count - 1]._face.N.Add(new[] { int.Parse(d[3]) - 1, int.Parse(d[6]) - 1, int.Parse(d[9]) - 1 });
                    }
                }
            }
        }

        public Vertex VArrayToOBJ()
        {
            var result = new Vertex();
            var vertexList = BuildVertexArray(0, false);
            for (int j = 0; j < vertexList.Length; j++)
            {
                result.P.Add(new[] { vertexList[j][0], vertexList[j][1], vertexList[j][2] });
                result.N.Add(new[] { vertexList[j][3], vertexList[j][4], vertexList[j][5] });
                result.T.Add(new[] { vertexList[j][6], vertexList[j][7] });
            }
            return result;
        }

        public float[][] BuildVertexArray(int g, bool flipUV)
        {
            var vertexList = new List<float[]>();
            for (int j = 0; j < _group[g]._face.V.Count; j++)
            {
                for (int k = 0; k < _group[g]._face.V[j].Length; k++)
                {
                    var px = (_vertex.P[_group[g]._face.V[j][k]][0]);
                    var py = (_vertex.P[_group[g]._face.V[j][k]][1]);
                    var pz = (_vertex.P[_group[g]._face.V[j][k]][2]);
                    var nx = (_vertex.N[_group[g]._face.N[j][k]][0]);
                    var ny = (_vertex.N[_group[g]._face.N[j][k]][1]);
                    var nz = (_vertex.N[_group[g]._face.N[j][k]][2]);
                    var ux = (_vertex.T[_group[g]._face.T[j][k]][0]);
                    var uy = (_vertex.T[_group[g]._face.T[j][k]][1]);
                    if (flipUV)
                    {
                        vertexList.Add(new[] { px, py, pz, nx, ny, nz, ux, uy * -1 });
                    }
                    else
                    {
                        vertexList.Add(new[] { px, py, pz, nx, ny, nz, ux, uy });
                    }
                }
            }
            return vertexList.ToArray();
        }

        public int[][] BuildIndexArray(int g)
        {
            var indexList = new List<int[]>();
            int size = _group[g]._face.V.Count;
            for (int i = 0; i < size; i++)
            {
                indexList.Add(new[] { i * 3, i * 3 + 1, i * 3 + 2 });
            }
            return indexList.ToArray();
        }

        public void SaveOBJ(string path)
        {
            var OBJ = new StringBuilder("#Hitman SKD - By Skript47").AppendLine().AppendLine();
            string f = "0.000000";
            foreach (var i in _vertex.P)
            {
                OBJ.AppendLine("v " + i[0].ToString(f) + " " + i[1].ToString(f) + " " + i[2].ToString(f));
            }
            OBJ.AppendFormat("# {0} vertices", _vertex.P.Count).AppendLine();
            foreach (var i in _vertex.N)
            {
                OBJ.AppendLine("vn " + i[0].ToString(f) + " " + i[1].ToString(f) + " " + i[2].ToString(f));
            }
            OBJ.AppendFormat("# {0} vertex normals", _vertex.N.Count).AppendLine().AppendLine();
            foreach (var i in _vertex.T)
            {
                OBJ.AppendLine("vt " + i[0].ToString(f) + " " + i[1].ToString(f));
            }
            OBJ.AppendFormat("# {0} texture coords", _vertex.T.Count).AppendLine().AppendLine();
            foreach (var g in _group)
            {
                OBJ.AppendLine("g " + g.name).AppendLine("s 1");
                for (int j = 0; j < g._face.V.Count; j++)
                {
                    OBJ.Append("f");
                    OBJ.AppendFormat(" {0}/{1}/{2}", g._face.V[j][0] + 1, g._face.T[j][0] + 1, g._face.N[j][0] + 1);
                    OBJ.AppendFormat(" {0}/{1}/{2}", g._face.V[j][1] + 1, g._face.T[j][1] + 1, g._face.N[j][1] + 1);
                    OBJ.AppendFormat(" {0}/{1}/{2}", g._face.V[j][2] + 1, g._face.T[j][2] + 1, g._face.N[j][2] + 1);
                    OBJ.AppendLine();
                }
                OBJ.AppendLine();
            }
            File.WriteAllText(path, OBJ.ToString());
        }
    }
}
