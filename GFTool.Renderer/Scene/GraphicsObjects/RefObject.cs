using OpenTK.Mathematics;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class RefObject
    {
        public long ID { get; private set; }
        private static long nextID = 0;

        public List<RefObject> children;

        public Transform Transform { get; protected set; }

        public RefObject()
        {
            ID = nextID++;
            Transform = new Transform();
            children = new List<RefObject>();
        }

        public virtual void Setup() 
        {
            foreach (var child in children)
            { 
                child?.Setup();
            }
        }
        public virtual void Draw(Matrix4 view, Matrix4 proj) 
        {
            foreach (var child in children)
            { 
                child?.Draw(view, proj);
            }
        }

        public void Translate(Vector3 translation)
        {
            Transform.Position += translation;
        }

        public long GetID()
        {
            return ID;
        }

        public void AddChild(RefObject child)
        { 
            children.Add(child);
            child.Setup();
        }

        public RefObject Find(Type type)
        {
            RefObject ob = null;

            foreach (var c in children)
            {
                if (ob != null) return ob;

                if (this.GetType() == type)
                    ob = this;
                else
                    ob = c.Find(type);
            }

            return ob;
        }
    }
}
