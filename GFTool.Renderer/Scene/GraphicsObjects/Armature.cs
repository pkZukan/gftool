using OpenTK.Mathematics;
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
        public class Bone
        { 
            public string Name {  get; private set; }
            public Transform Transform { get; private set; } = new Transform();

            public Bone Parent;
            public List<Bone> Children;

            public Bone(TRTransformNode node)
            { 
                Name = node.Name;
                Transform.Position = new Vector3(node.Transform.Translation.X, node.Transform.Translation.Y, node.Transform.Translation.Z);
                Transform.Rotation = Quaternion.FromEulerAngles(node.Transform.Rotation.X, node.Transform.Rotation.Y, node.Transform.Rotation.Z);
                Transform.Scale = new Vector3(node.Transform.Scale.X, node.Transform.Scale.Y, node.Transform.Scale.Z);
                Parent = null;
                Children = new List<Bone>();
            }

            public void AddChild(Bone bone)
            { 
                bone.Parent = this;
                bone.Children.Add(bone);
            }
        }

        public List<Bone> Bones = new List<Bone>();

        public Armature(TRSKL skel)
        {
            foreach (var transNode in skel.TransformNodes)
            {
                var bone = new Bone(transNode);
                Bones.Add(bone);
            }
        }
    }
}
