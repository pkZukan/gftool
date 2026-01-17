using System;
using System.Collections.Generic;
using FlatSharp;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.GF.Animation;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Animation
    {
        public enum PlayType
        {
            Once,
            Looped
        }

        public string Name { get; }
        public PlayType LoopType { get; }
        public uint FrameCount { get; }
        public uint FrameRate { get; }
        public int TrackCount => tracks.Count;
        public IReadOnlyList<string> TrackNames => trackOrder;

        private readonly Dictionary<string, BoneTrack> tracks = new Dictionary<string, BoneTrack>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> trackOrder = new List<string>();

        public Animation(Trinity.Core.Flatbuffers.GF.Animation.Animation anim, string name)
        {
            Name = name;
            LoopType = anim.Info.DoesLoop != 0 ? PlayType.Looped : PlayType.Once;
            FrameCount = anim.Info.KeyFrames;
            FrameRate = anim.Info.FrameRate;

            if (anim.Skeleton?.Tracks != null)
            {
                foreach (var track in anim.Skeleton.Tracks)
                {
                    if (string.IsNullOrWhiteSpace(track.Name))
                    {
                        continue;
                    }

                    if (!tracks.ContainsKey(track.Name))
                    {
                        tracks[track.Name] = track;
                        trackOrder.Add(track.Name);
                    }

                    var normalized = NormalizeBoneName(track.Name);
                    if (!string.IsNullOrWhiteSpace(normalized) && !tracks.ContainsKey(normalized))
                    {
                        tracks[normalized] = track;
                    }
                }
            }
        }

        public float GetFrame(float timeSeconds)
        {
            float frameRate = FrameRate > 0 ? FrameRate : 30f;
            float frame = timeSeconds * frameRate;
            if (FrameCount > 0)
            {
                if (LoopType == PlayType.Looped)
                {
                    frame %= FrameCount;
                }
                frame = Math.Clamp(frame, 0f, Math.Max(0f, FrameCount - 1));
            }
            return frame;
        }

        public bool TryGetPose(string boneName, float frame, out Vector3? scale, out Quaternion? rotation, out Vector3? translation)
        {
            scale = null;
            rotation = null;
            translation = null;

            if (!TryGetTrack(boneName, out var track))
            {
                return false;
            }

            scale = SampleVector(track.Scale, frame);
            rotation = SampleRotation(track.Rotate, frame);
            translation = SampleVector(track.Translate, frame);
            return true;
        }

        public bool HasTrack(string boneName)
        {
            return TryGetTrack(boneName, out _);
        }

        private bool TryGetTrack(string boneName, out BoneTrack track)
        {
            if (string.IsNullOrWhiteSpace(boneName))
            {
                track = default;
                return false;
            }

            if (tracks.TryGetValue(boneName, out track))
            {
                return true;
            }

            var normalized = NormalizeBoneName(boneName);
            if (!string.IsNullOrWhiteSpace(normalized) && tracks.TryGetValue(normalized, out track))
            {
                return true;
            }

            track = default;
            return false;
        }

        private static string NormalizeBoneName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            name = name.Trim();

            // Common authoring/runtime namespaces.
            int lastColon = name.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < name.Length - 1)
            {
                name = name.Substring(lastColon + 1);
            }

            // Common hierarchy separators.
            int lastPipe = name.LastIndexOf('|');
            if (lastPipe >= 0 && lastPipe < name.Length - 1)
            {
                name = name.Substring(lastPipe + 1);
            }

            int lastSlash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
            if (lastSlash >= 0 && lastSlash < name.Length - 1)
            {
                name = name.Substring(lastSlash + 1);
            }

            return name.Trim();
        }

        private static Vector3? SampleVector(FlatBufferUnion<FixedVectorTrack, DynamicVectorTrack, Framed16VectorTrack, Framed8VectorTrack> channel, float frame)
        {
            Vector3? result = null;
            channel.Switch(
                defaultCase: () => { },
                case1: v => result = v.Co != null ? ToVector3(v.Co) : (Vector3?)null,
                case2: v => result = SampleDynamicVector(v.Co, frame),
                case3: v => result = SampleFramedVector(v.Frames, v.Co, frame),
                case4: v => result = SampleFramedVector(v.Frames, v.Co, frame)
            );
            return result;
        }

        private static Quaternion? SampleRotation(FlatBufferUnion<FixedRotationTrack, DynamicRotationTrack, Framed16RotationTrack, Framed8RotationTrack> channel, float frame)
        {
            Quaternion? result = null;
            channel.Switch(
                defaultCase: () => { },
                case1: v => result = v.Co != null ? ToQuaternion(v.Co) : (Quaternion?)null,
                case2: v => result = SampleDynamicRotation(v.Co, frame),
                case3: v => result = SampleFramedRotation(v.Frames, v.Co, frame),
                case4: v => result = SampleFramedRotation(v.Frames, v.Co, frame)
            );
            return result;
        }

        private static Vector3? SampleDynamicVector(IList<Vector3f> values, float frame)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            int index = Math.Clamp((int)MathF.Floor(frame), 0, values.Count - 1);
            return ToVector3(values[index]);
        }

        private static Vector3? SampleFramedVector<T>(IList<T> frames, IList<Vector3f> values, float frame) where T : struct
        {
            if (frames == null || values == null || frames.Count == 0 || values.Count == 0)
            {
                return null;
            }

            int count = Math.Min(frames.Count, values.Count);
            float keyFrame = frame;
            if (keyFrame <= GetFrame(frames[0]))
            {
                return ToVector3(values[0]);
            }
            if (keyFrame >= GetFrame(frames[count - 1]))
            {
                return ToVector3(values[count - 1]);
            }

            bool useCatmull = count >= 4;
            for (int i = 0; i < count - 1; i++)
            {
                float k1 = GetFrame(frames[i]);
                float k2 = GetFrame(frames[i + 1]);
                if (keyFrame >= k1 && keyFrame <= k2)
                {
                    float denom = k2 - k1;
                    if (denom <= 0.0f)
                    {
                        return ToVector3(values[i + 1]);
                    }

                    float t = (keyFrame - k1) / denom;
                    var v1 = ToVector3(values[i]);
                    var v2 = ToVector3(values[i + 1]);
                    if (!useCatmull)
                    {
                        return Vector3.Lerp(v1, v2, t);
                    }

                    return CatmullRomNonUniform(
                        ToVector3(values[Math.Max(i - 1, 0)]),
                        v1,
                        v2,
                        ToVector3(values[Math.Min(i + 2, count - 1)]),
                        GetFrame(frames[Math.Max(i - 1, 0)]),
                        k1,
                        k2,
                        GetFrame(frames[Math.Min(i + 2, count - 1)]),
                        keyFrame);
                }
            }

            return ToVector3(values[count - 1]);
        }

        private static Quaternion? SampleDynamicRotation(IList<PackedQuaternion> values, float frame)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            int index = Math.Clamp((int)MathF.Floor(frame), 0, values.Count - 1);
            return ToQuaternion(values[index]);
        }

        private static Quaternion? SampleFramedRotation<T>(IList<T> frames, IList<PackedQuaternion> values, float frame) where T : struct
        {
            if (frames == null || values == null || frames.Count == 0 || values.Count == 0)
            {
                return null;
            }

            int count = Math.Min(frames.Count, values.Count);
            float keyFrame = frame;
            if (keyFrame <= GetFrame(frames[0]))
            {
                return ToQuaternion(values[0]);
            }
            if (keyFrame >= GetFrame(frames[count - 1]))
            {
                return ToQuaternion(values[count - 1]);
            }

            bool useCatmull = count >= 4;
            for (int i = 0; i < count - 1; i++)
            {
                float k1 = GetFrame(frames[i]);
                float k2 = GetFrame(frames[i + 1]);
                if (keyFrame >= k1 && keyFrame <= k2)
                {
                    float denom = k2 - k1;
                    if (denom <= 0.0f)
                    {
                        return ToQuaternion(values[i + 1]);
                    }

                    float t = (keyFrame - k1) / denom;
                    var q1 = ToQuaternion(values[i]);
                    var q2 = ToQuaternion(values[i + 1]);
                    if (!useCatmull)
                    {
                        return Quaternion.Slerp(q1, q2, t);
                    }

                    return CatmullRomNonUniform(
                        ToQuaternion(values[Math.Max(i - 1, 0)]),
                        q1,
                        q2,
                        ToQuaternion(values[Math.Min(i + 2, count - 1)]),
                        GetFrame(frames[Math.Max(i - 1, 0)]),
                        k1,
                        k2,
                        GetFrame(frames[Math.Min(i + 2, count - 1)]),
                        keyFrame);
                }
            }

            return ToQuaternion(values[count - 1]);
        }

        private static float GetFrame<T>(T value) where T : struct
        {
            return value switch
            {
                byte b => b,
                ushort s => s,
                _ => 0f
            };
        }

        private static Vector3 ToVector3(Vector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static Vector3 CatmullRomNonUniform(
            in Vector3 p0,
            in Vector3 p1,
            in Vector3 p2,
            in Vector3 p3,
            float t0,
            float t1,
            float t2,
            float t3,
            float t)
        {
            if (t2 <= t1)
            {
                return p2;
            }

            float u = (t - t1) / (t2 - t1);
            float t10 = t1 - t0;
            float t21 = t2 - t1;
            float t32 = t3 - t2;

            if (t10 <= 0f)
            {
                t10 = t21;
            }
            if (t32 <= 0f)
            {
                t32 = t21;
            }

            Vector3 m1 = (p2 - p0) / (t2 - t0);
            Vector3 m2 = (p3 - p1) / (t3 - t1);
            m1 *= t21;
            m2 *= t21;

            float u2 = u * u;
            float u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p1 +
                   (u3 - 2f * u2 + u) * m1 +
                   (-2f * u3 + 3f * u2) * p2 +
                   (u3 - u2) * m2;
        }

        private static Quaternion CatmullRomNonUniform(
            in Quaternion q0,
            in Quaternion q1,
            in Quaternion q2,
            in Quaternion q3,
            float t0,
            float t1,
            float t2,
            float t3,
            float t)
        {
            // Quaternion Catmull Rom needs manifold math. Component wise Hermite is not suitable.
            // SQUAD style cubic interpolation keeps uneven key spacing stable.
            if (t2 <= t1)
            {
                return q2;
            }

            float u = (t - t1) / (t2 - t1);
            u = Math.Clamp(u, 0f, 1f);

            // Neighbors are kept in the same hemisphere to avoid long path flips.
            Quaternion qa = EnsureSameHemisphere(q0, q1);
            Quaternion qb = q1;
            Quaternion qc = EnsureSameHemisphere(q2, q1);
            Quaternion qd = EnsureSameHemisphere(q3, q2);

            float dt10 = MathF.Max(0.000001f, t1 - t0);
            float dt21 = MathF.Max(0.000001f, t2 - t1);
            float dt32 = MathF.Max(0.000001f, t3 - t2);

            // Shoemake tangents: control quats at q1 and q2, scaled by dt for uneven keys.
            Quaternion a1 = ComputeSquadControl(qa, qb, qc, dt10, dt21);
            Quaternion a2 = ComputeSquadControl(qb, qc, qd, dt21, dt32);

            return Squad(qb, qc, a1, a2, u);
        }

        private static Quaternion ComputeSquadControl(in Quaternion qPrev, in Quaternion q, in Quaternion qNext, float dtPrev, float dtNext)
        {
            // Control quaternions are computed from neighbor quats using weighted log and exp terms.
            // Inverse dt weights reduce the influence from far apart keys.
            Quaternion inv = ConjugateNormalized(q);
            Quaternion l1 = Log(EnsureSameHemisphere(Mul(inv, qPrev), Quaternion.Identity));
            Quaternion l2 = Log(EnsureSameHemisphere(Mul(inv, qNext), Quaternion.Identity));

            float wPrev = 1f / MathF.Max(0.000001f, dtPrev);
            float wNext = 1f / MathF.Max(0.000001f, dtNext);
            float wSum = wPrev + wNext;
            if (wSum <= 0f)
            {
                wPrev = wNext = 0.5f;
            }
            else
            {
                wPrev /= wSum;
                wNext /= wSum;
            }

            Quaternion v = (l1 * wPrev + l2 * wNext) * (-0.25f);
            Quaternion e = Exp(v);
            Quaternion outQ = Mul(q, e);
            if (outQ.LengthSquared > 0.0f)
            {
                outQ = outQ.Normalized();
            }
            return outQ;
        }

        private static Quaternion Squad(in Quaternion q1, in Quaternion q2, in Quaternion a1, in Quaternion a2, float t)
        {
            // SQUAD blends slerp(q1,q2,t) and slerp(a1,a2,t) using 2t times (1 minus t).
            var slerp12 = Quaternion.Slerp(q1, q2, t);
            var slerpA = Quaternion.Slerp(a1, a2, t);
            float h = 2f * t * (1f - t);
            return Quaternion.Slerp(slerp12, slerpA, h);
        }

        private static Quaternion ConjugateNormalized(in Quaternion q)
        {
            // Unit quats: inverse equals conjugate.
            // Normalization is applied defensively in case of drift.
            Quaternion n = q;
            if (n.LengthSquared > 0.0f)
            {
                n = n.Normalized();
            }
            return new Quaternion(-n.X, -n.Y, -n.Z, n.W);
        }

        private static Quaternion Mul(in Quaternion a, in Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
        }

        private static Quaternion Log(in Quaternion q)
        {
            // Log of a unit quaternion. Result is pure imaginary with w set to 0.
            Quaternion n = q;
            if (n.LengthSquared > 0.0f)
            {
                n = n.Normalized();
            }

            float w = Math.Clamp(n.W, -1f, 1f);
            float angle = MathF.Acos(w);
            float sin = MathF.Sin(angle);
            if (MathF.Abs(sin) < 1e-6f)
            {
                return new Quaternion(0f, 0f, 0f, 0f);
            }

            float scale = angle / sin;
            return new Quaternion(n.X * scale, n.Y * scale, n.Z * scale, 0f);
        }

        private static Quaternion Exp(in Quaternion q)
        {
            // Exp of a pure imaginary quaternion. W is ignored.
            float angle = MathF.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            float sin = MathF.Sin(angle);
            float cos = MathF.Cos(angle);
            if (angle < 1e-6f)
            {
                return new Quaternion(q.X, q.Y, q.Z, 1f).Normalized();
            }

            float scale = sin / angle;
            return new Quaternion(q.X * scale, q.Y * scale, q.Z * scale, cos);
        }

        private static Quaternion EnsureSameHemisphere(in Quaternion q, in Quaternion reference)
        {
            if (Dot(q, reference) < 0f)
            {
                return new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
            }
            return q;
        }

        private static float Dot(in Quaternion a, in Quaternion b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);
        }

        private static Quaternion ToQuaternion(PackedQuaternion packed)
        {
            var q = packed.Unpack();
            var result = new Quaternion(q.X, q.Y, q.Z, q.W);
            if (result.LengthSquared > 0.0f)
            {
                result = result.Normalized();
            }
            return result;
        }

    }
}
