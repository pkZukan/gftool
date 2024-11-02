using OpenTK.Mathematics;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Object
    {
        public long ID { get; private set; }
        private static long nextID = 0;

        public List<Object> children;

        public Vector3 Position { get; protected set; } = new Vector3();
        public Quaternion Rotation { get; protected set; } = new Quaternion();

        public Object()
        {
            ID = nextID++;
            children = new List<Object>();
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
            Position += translation;
        }

        public long GetID()
        {
            return ID;
        }

        public void AddChild(Object child)
        { 
            children.Add(child);
            child.Setup();
        }

        public Object Find(Type type)
        {
            Object ob = null;

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
