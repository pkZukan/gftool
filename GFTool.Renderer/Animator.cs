using GFTool.Renderer.Scene.GraphicsObjects;

namespace GFTool.Renderer
{
    public class Animator
    {
        private Animation animation;
        private float animTime;
        private float step = 1;

        public Animator() 
        { 
            //
        }

        public void PlayAnim(Animation anim)
        { 
            animation = anim;
            animTime = 0.0f;
        }

        public void StopAnim()
        {
            //
        }

        private void UpdateAnim()
        { 
            //
        }
    }
}
