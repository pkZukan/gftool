using OpenTK.Mathematics;

namespace GFTool.Renderer.Scene
{
    public class Camera : GraphicsObjects.Object
    {
        public Matrix4 projMat { get; private set; }
        public Matrix4 viewMat { get; private set; }

        private ProjectionType projMode = ProjectionType.Perspective;
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 100.0f;
        public bool CanMove { get; set; } = true;

        private int Width, Height;

        //Constants
        const float SENSITIVITY_X = 0.1f;
        const float SENSITIVITY_Y = 0.1f;

        const float MOVEMENT_SPEED = 0.1f;

        public enum ProjectionType
        { 
            Perspective,
            Orthographic
        }

        public Camera(int width, int height)
        {
            Width = width;
            Height = height;
            Rotation = Quaternion.FromEulerAngles(0, 90, 0); //Camera defaults to facing forward
            SetProjectionMode(ProjectionType.Perspective);
        }

        public void SetProjectionMode(ProjectionType mode)
        { 
            projMode = mode;
            UpdateProjMatrix();
        }

        public void UpdateProjMatrix()
        {
            float aspectRatio = Width / (float)Height;

            if (projMode == ProjectionType.Perspective)
            {
                float fov = MathHelper.DegreesToRadians(45.0f);
                projMat = Matrix4.CreatePerspectiveFieldOfView(fov, aspectRatio, NearPlane, FarPlane);
            }
            else if (projMode == ProjectionType.Orthographic)
            {
                float orthoSize = 10.0f;
                float left = -orthoSize * aspectRatio;
                float right = orthoSize * aspectRatio;
                float bottom = -orthoSize;
                float top = orthoSize;

                projMat = Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, NearPlane, FarPlane);
            }
        }


        private Vector3 CalculateCameraFront()
        {
            Vector3 front = new Vector3();
            Vector3 currRot = Rotation.ToEulerAngles();

            float yawRad = currRot.Y;
            float pitchRad = currRot.X;

            front.X = (float)(Math.Cos(yawRad) * Math.Cos(pitchRad));
            front.Y = (float)(Math.Sin(pitchRad));
            front.Z = (float)(Math.Sin(yawRad) * Math.Cos(pitchRad));

            return Vector3.Normalize(front);
        }

        public void ApplyRotationalDelta(float deltaX, float deltaY)
        {
            if (!CanMove) return;

            // Get current rotation
            Vector3 currRot = Rotation.ToEulerAngles();
            currRot.Y += MathHelper.DegreesToRadians(deltaX* SENSITIVITY_X);
            currRot.X += MathHelper.DegreesToRadians(deltaY * SENSITIVITY_Y);

            Rotation = Quaternion.FromEulerAngles(currRot);
        }


        public void ApplyMovement(float x, float y, float z)
        {
            if (!CanMove) return;
            Vector3 forward = CalculateCameraFront();
            Vector3 right = Vector3.Cross(forward, Vector3.UnitY).Normalized();
            Vector3 up = Vector3.Cross(right, forward).Normalized();

            // Combine movement along each axis
            Position += right * z * MOVEMENT_SPEED;
            Position += up * y * MOVEMENT_SPEED; 
            Position += forward * x * MOVEMENT_SPEED;
        }

        public void Update()
        {
            Vector3 front = CalculateCameraFront();
            viewMat = Matrix4.LookAt(
                Position,               // Camera position
                Position + front,       // Target (position + front vector)
                new Vector3(0, 1, 0)    // Up vector (y-axis up)
            );
        }

        private void UpdateMatricies()
        {
            //
        }
    }
}
