using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Model : RefObject
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
        private List<string> MaterialNames = new List<string>();

        private Armature armature;

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
                        case TRIndexFormat.BYTE: indices.Add(indBuf.ReadByte()); break;
                        case TRIndexFormat.SHORT: indices.Add(indBuf.ReadUInt16()); break;
                        case TRIndexFormat.INT: indices.Add(indBuf.ReadUInt32()); break;
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
            var shapeCnt = msh.Meshes.Count();
            for(int i = 0; i < shapeCnt; i++)
            {
                var meshShape = msh.Meshes[i];
                var vertBuf = buffers[i].VertexBuffer[0]; //LOD0
                var indexBuf = buffers[i].IndexBuffer[0]; //LOD0
                var polyType = meshShape.IndexType;

                foreach (var part in meshShape.meshParts)
                {
                    MaterialNames.Add(part.MaterialName);
                    ParseMeshBuffer(meshShape.vertexDeclaration[0], vertBuf, indexBuf, meshShape.IndexType, part.indexOffset, part.indexCount);
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
            armature = new Armature(skel);
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
                var blendSize = BlendIndicies[i].Length * 4;
                var weightSize = BlendWeights[i].Length * 4;
                var totalSize = vertSize + normSize + uvSize;

                //VBO
                GL.GenBuffers(1, out VBOs[i]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);
                GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

                //Upload vertex data to the buffer
                IntPtr offset = IntPtr.Zero;
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, vertSize, Positions[i].SelectMany(x => x.ToBytes()).ToArray()); offset += vertSize;          // Verts
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, normSize, Normals[i].SelectMany(x => x.ToBytes()).ToArray()); offset += normSize;            // Normals
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, uvSize, UVs[i].SelectMany(x => x.ToBytes()).ToArray()); offset += uvSize;                    // TexCoords
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, blendSize, BlendIndicies[i].SelectMany(x => x.ToBytes()).ToArray()); offset += blendSize;    // BlendInds
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, weightSize, BlendWeights[i].SelectMany(x => x.ToBytes()).ToArray()); offset += weightSize;   // BlendWeights

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

                // index attribute
                GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Int, false, 4, offset); offset += blendSize;
                GL.EnableVertexAttribArray(3);

                // weight attribute
                GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, 4, offset); offset += weightSize;
                GL.EnableVertexAttribArray(4);

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
                //Bind appropriate mat
                materials.Where(m => m.Name == MaterialNames[i]).First().Use(view, modelMat, proj);
                
                // Draw the geometry
                GL.BindVertexArray(VAOs[i]);
                GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
            }
        }
    }
}
