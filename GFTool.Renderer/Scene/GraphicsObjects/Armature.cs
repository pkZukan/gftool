using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Armature
    {
        public struct Bone
        { 
            public string Name {  get; private set; }
            public Transform Transform { get; private set; }

            public Bone(TRTransformNode node)
            { 
                Name = node.Name;
                Transform = node.Transform;
            }
        }

        public List<Bone> Bones = new List<Bone>();

        public Armature(TRSKL skel)
        {
            foreach (var transNode in skel.TransformNodes)
                Bones.Add(new Bone(transNode));
        }
    }
}
