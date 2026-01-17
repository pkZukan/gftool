using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;

namespace GFTool.Renderer.Scene
{
    public class Camera : RefObject
    {
        public Matrix4 projMat { get; private set; }
        public Matrix4 viewMat { get; private set; }

        private ProjectionType projMode = ProjectionType.Perspective;
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 100.0f;
        public bool CanMove { get; set; } = true;

        private int Width, Height;
        private bool rotationInitialized = false;
        private float yaw;
        private float pitch;
        private Vector3 target = Vector3.Zero;
        private float distance = 5.0f;

        //Constants
        const float ROTATE_SPEED = 2.2f;
        const float PAN_SPEED = 2.0f;
        const float DOLLY_SPEED = 5.0f;
        const float PITCH_LIMIT = 1.553343f; // ~89 degrees

        const float MOVEMENT_SPEED = 6.0f;

        public enum ProjectionType
        {
            Perspective,
            Orthographic
        }

        public Camera(int width, int height)
        {
            Width = width;
            Height = height;
            Transform.Position = new Vector3(0, 1, 1);
            target = Vector3.Zero;
            distance = (Transform.Position - target).Length;
            var front = Vector3.Normalize(target - Transform.Position);
            pitch = MathF.Asin(front.Y);
            yaw = MathF.Atan2(front.Z, front.X);
            rotationInitialized = true;
            Transform.Rotation = Quaternion.FromEulerAngles(pitch, yaw, 0f);
            SetProjectionMode(ProjectionType.Perspective);
        }

        public void SetOrbitTarget(Vector3 newTarget, float? newDistance = null)
        {
            target = newTarget;
            if (newDistance.HasValue)
            {
                distance = Math.Max(0.1f, newDistance.Value);
            }

            UpdateOrbitPosition();
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

        public void Resize(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            UpdateProjMatrix();
        }


        private Vector3 CalculateCameraFront()
        {
            if (!rotationInitialized)
            {
                var euler = Transform.Rotation.ToEulerAngles();
                pitch = euler.X;
                yaw = euler.Y;
                rotationInitialized = true;
            }

            return FrontFromAngles(yaw, pitch);
        }

        public void ApplyRotationalDelta(float deltaX, float deltaY)
        {
            if (!CanMove) return;

            if (!rotationInitialized)
            {
                var euler = Transform.Rotation.ToEulerAngles();
                pitch = euler.X;
                yaw = euler.Y;
                rotationInitialized = true;
            }

            const float maxDelta = 0.25f;
            deltaX = Math.Clamp(deltaX, -maxDelta, maxDelta);
            deltaY = Math.Clamp(deltaY, -maxDelta, maxDelta);

            yaw += deltaX * ROTATE_SPEED;
            pitch += -deltaY * ROTATE_SPEED;
            pitch = Math.Clamp(pitch, -PITCH_LIMIT, PITCH_LIMIT);
            yaw = WrapAngle(yaw);

            Transform.Rotation = Quaternion.FromEulerAngles(pitch, yaw, 0f);
        }


        public void ApplyMovement(float x, float y, float z, float deltaSeconds)
        {
            if (!CanMove) return;
            if (deltaSeconds <= 0f) return;
            Vector3 forward = CalculateCameraFront();
            Vector3 right = Vector3.Cross(forward, Vector3.UnitY).Normalized();
            Vector3 up = Vector3.Cross(right, forward).Normalized();

            // Combine movement along each axis
            float move = MOVEMENT_SPEED * deltaSeconds;
            Transform.Position += right * z * move;
            Transform.Position += up * y * move;
            Transform.Position += forward * x * move;
            target += right * z * move;
            target += up * y * move;
            target += forward * x * move;
        }

        public void ApplyPan(float deltaX, float deltaY)
        {
            if (!CanMove) return;
            Vector3 forward = CalculateCameraFront();
            Vector3 right = Vector3.Cross(forward, Vector3.UnitY).Normalized();
            Vector3 up = Vector3.Cross(right, forward).Normalized();

            float scale = PAN_SPEED * distance;
            Vector3 pan = right * deltaX * scale + up * deltaY * scale;
            Transform.Position += pan;
            target += pan;
        }

        public void ApplyDolly(float delta)
        {
            if (!CanMove) return;
            distance = Math.Max(0.1f, distance - (delta * DOLLY_SPEED));
            UpdateOrbitPosition();
        }

        public void Update()
        {
            UpdateOrbitPosition();
            Vector3 front = CalculateCameraFront();
            viewMat = Matrix4.LookAt(
                Transform.Position,         // Camera position
                target, // Target position
                new Vector3(0, 1, 0)        // Up vector (y-axis up)
            );
        }

        private void UpdateOrbitPosition()
        {
            if (!rotationInitialized)
            {
                var euler = Transform.Rotation.ToEulerAngles();
                pitch = euler.X;
                yaw = euler.Y;
                rotationInitialized = true;
            }

            Vector3 front = FrontFromAngles(yaw, pitch);
            Transform.Position = target - (front * distance);
        }

        private static Vector3 FrontFromAngles(float yawRad, float pitchRad)
        {
            Vector3 front;
            front.X = (float)(Math.Cos(yawRad) * Math.Cos(pitchRad));
            front.Y = (float)(Math.Sin(pitchRad));
            front.Z = (float)(Math.Sin(yawRad) * Math.Cos(pitchRad));
            return Vector3.Normalize(front);
        }

        private static float WrapAngle(float radians)
        {
            const float twoPi = MathF.PI * 2f;
            if (radians > MathF.PI || radians < -MathF.PI)
            {
                radians %= twoPi;
                if (radians > MathF.PI) radians -= twoPi;
                if (radians < -MathF.PI) radians += twoPi;
            }
            return radians;
        }

        private void UpdateMatricies()
        {
            //
        }
    }
}
