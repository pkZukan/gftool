using GFTool.Core.Utils;
using GFTool.Renderer.Scene;
using Microsoft.CodeAnalysis.CodeActions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.TR.Resident;
using Trinity.Core.Utils;
using static OpenTK.Graphics.OpenGL.GL;
using static System.Net.Mime.MediaTypeNames;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Model : GraphicsObjects.Object
    {
        private PathString modelPath;

        public string Name { get; private set; }

        private int[] VAOs;
        private int[] VBOs;
        private int[] EBOs;

        private List<Vector3[]> Positions = new List<Vector3[]>();
        private List<Vector3[]> Normals = new List<Vector3[]>();
        private List<Vector2[]> UVs = new List<Vector2[]>();
        private List<int[]> BlendIndicies = new List<int[]>();
        private List<float[]> BlendWeights = new List<float[]>();

        private List<uint[]> Indices = new List<uint[]>();

        private Material[] materials;

        private Matrix4 modelMat;

        public Model(string model)
        {
            Name = Path.GetFileNameWithoutExtension(model);
            modelMat = Matrix4.Identity;
            modelPath = new PathString(model);

            var mdl = FlatBufferConverter.DeserializeFrom<TRMDL>(model);

            //Meshes
            var mesh = mdl.Meshes[0]; //LOD0
            ParseMesh(modelPath.Combine(mesh.PathName));

            //Materials
            foreach (var mat in mdl.Materials)
            {
                ParseMaterial(modelPath.Combine(mat));
            }

            //Skeleton
            if(mdl.Skeleton != null)
                ParseArmature(modelPath.Combine(mdl.Skeleton.PathName));
        }

        private void ParseMeshBuffer(TRVertexDeclaration vertDesc, TRBuffer vertBuf, TRBuffer indexBuf, TRIndexFormat polyType, long start, long count)
        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> norm = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> blend = new List<int>();
            List<float> weight = new List<float>();

            List<uint> indices = new List<uint>();
            long currPos = 0;

            //Parse vertex buffer
            using (var vertexBuf = new BinaryReader(new MemoryStream(vertBuf.Bytes)))
            { 
                while (currPos < vertexBuf.BaseStream.Length) //TODO: optimize to not have duplicate buffers
                {
                    for (int i = 0; i < vertDesc.vertexElements.Length; i++)
                    {
                        var att = vertDesc.vertexElements[i];
                        vertexBuf.BaseStream.Position = currPos + att.vertexElementOffset;
                        switch (att.vertexUsage)
                        {
                            case TRVertexUsage.POSITION:
                            {
                                pos.Add(new Vector3(vertexBuf.ReadSingle(), vertexBuf.ReadSingle(), vertexBuf.ReadSingle()));
                                break;
                            }
                            case TRVertexUsage.NORMAL:
                            {
                                norm.Add(new Vector3((float)vertexBuf.ReadHalf(), (float)vertexBuf.ReadHalf(), (float)vertexBuf.ReadHalf()));
                                vertexBuf.ReadHalf();
                                break;
                            }
                            case TRVertexUsage.TEX_COORD:
                            {
                                uv.Add(new Vector2(vertexBuf.ReadSingle(), vertexBuf.ReadSingle()));
                                break;
                            }
                            case TRVertexUsage.BLEND_INDEX:
                            {
                                blend.Add(vertexBuf.ReadInt32());
                                break;
                            }
                            case TRVertexUsage.BLEND_WEIGHTS:
                            {
                                weight.Add(vertexBuf.ReadSingle());
                                break;
                            }
                        }
                    }
                    currPos += vertDesc.vertexElementSizes[0].elementSize;
                }

                Positions.Add(pos.ToArray());
                Normals.Add(norm.ToArray());
                UVs.Add(uv.ToArray());
                BlendIndicies.Add(blend.ToArray());
                BlendWeights.Add(weight.ToArray());
            }

            //Parse index buffer
            using (var indBuf = new BinaryReader(new MemoryStream(indexBuf.Bytes)))
            {
                int indsize = (1 << (int)polyType);
                currPos = start * indsize;
                indBuf.BaseStream.Position = currPos;
                while (currPos < (start + count) * indsize)
                {
                    switch (polyType)
                    {
                        case TRIndexFormat.X8_Y8_Z8_UNSIGNED: indices.Add(indBuf.ReadByte()); break;
                        case TRIndexFormat.X16_Y16_Z16_UNSIGNED: indices.Add(indBuf.ReadUInt16()); break;
                        case TRIndexFormat.X32_Y32_Z32_UNSIGNED: indices.Add(indBuf.ReadUInt32()); break;
                        case TRIndexFormat.X64_Y64_Z64_UNSIGNED: indices.Add((uint)indBuf.ReadUInt64()); break;
                    }
                    currPos += indsize;
                }
                Indices.Add(indices.ToArray());
            }

        }

        private void ParseMesh(string file)
        {
            var msh = FlatBufferConverter.DeserializeFrom<TRMSH>(file);
            var buffers = FlatBufferConverter.DeserializeFrom<TRMBF>(modelPath.Combine(msh.bufferFilePath)).TRMeshBuffers;
            var shapeCnt = msh.TRMeshes.Count();
            for(int i = 0; i < shapeCnt; i++)
            {
                var meshShape = msh.TRMeshes[i];
                var vertBuf = buffers[i].VertexBuffer[0]; //LOD0
                var indexBuf = buffers[i].IndexBuffer[0]; //LOD0
                var polyType = meshShape.PolygonType;

                foreach (var part in meshShape.meshParts)
                {
                    ParseMeshBuffer(meshShape.vertexDeclaration[0], vertBuf, indexBuf, meshShape.PolygonType, part.indexOffset, part.indexCount);
                }
            }

        }

        private void ParseMaterial(string file)
        {
            List<Material> matlist = new List<Material>();
            var mats = FlatBufferConverter.DeserializeFrom<TRMTR>(file);
            foreach (var mat in mats.Materials)
            {
                matlist.Add(new Material(modelPath, mat));
            }
            materials = matlist.ToArray();
        }

        private void ParseArmature(string file)
        {
            var skel = FlatBufferConverter.DeserializeFrom<TRSKL>(file);
            var name = skel.TransformNodes[0].Name;
            //TODO
        }

        public override void Setup()
        {
            var submeshCnt = Positions.Count;
            VAOs = new int[submeshCnt];

            VBOs = new int[submeshCnt];
            EBOs = new int[Indices.Count()];

            for (int i = 0; i < submeshCnt; i++) 
            {
                // VAO
                GL.GenVertexArrays(1, out VAOs[i]);
                GL.BindVertexArray(VAOs[i]);

                // Sizes
                var vertSize = Positions[i].Length * Vector3.SizeInBytes;
                var normSize = Normals[i].Length * Vector3.SizeInBytes;
                var uvSize = UVs[i].Length * Vector2.SizeInBytes;
                var totalSize = vertSize + normSize + uvSize;

                //VBO
                GL.GenBuffers(1, out VBOs[i]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);
                GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

                //Upload vertex data to the buffer
                IntPtr offset = IntPtr.Zero;
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, vertSize, Positions[i].SelectMany(x => x.ToBytes()).ToArray()); offset += vertSize;  // Verts
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, normSize, Normals[i].SelectMany(x => x.ToBytes()).ToArray()); offset += normSize;    // Normals
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, uvSize, UVs[i].SelectMany(x => x.ToBytes()).ToArray()); offset += uvSize;            // TexCoords

                // EBO (indices)
                GL.GenBuffers(1, out EBOs[i]);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOs[i]);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indices[i].Length * sizeof(uint), Indices[i].ToArray(), BufferUsageHint.StaticDraw);

                offset = IntPtr.Zero;

                // Pos attribute
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += vertSize;
                GL.EnableVertexAttribArray(0);

                // Norm attribute
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += normSize;
                GL.EnableVertexAttribArray(1);

                // UV attribute
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, offset); offset += uvSize;
                GL.EnableVertexAttribArray(2);

                //Clear bindings
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }

            //Grab any errors from setup
            ErrorCode error = ErrorCode.NoError;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Error in model \"{0}\": {1}", Name, error.ToString()));
            }

            base.Setup();
        }

        public override void Draw(Matrix4 view, Matrix4 proj)
        {
            for (int i = 0; i < VAOs.Length; i++)
            {
                //Draw
                GL.BindVertexArray(VAOs[i]);

                foreach (var mat in materials)
                {
                    mat.SetUniforms(view, modelMat, proj);
                }

                // Draw the geometry
                GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
            }
        }
    }
}
