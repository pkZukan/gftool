using System.Buffers.Binary;
using GFTool.Core.Flatbuffers.TR.Scene;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;

namespace TrinitySceneView.Loading;

public static class SceneTransforms
{
    public static Matrix4 FromSrt(SRT? srt)
    {
        if (srt == null) return Matrix4.Identity;
        var scale = Vector(srt.Scale, Vector3.One);
        var rotation = Vector(srt.Rotate, Vector3.Zero);
        var translation = Vector(srt.Translate, Vector3.Zero);
        // poke-dot's scene loader uses Godot's YXZ Euler order, in radians.
        return Matrix4.CreateScale(scale) * Matrix4.CreateRotationZ(rotation.Z)
            * Matrix4.CreateRotationX(rotation.X) * Matrix4.CreateRotationY(rotation.Y)
            * Matrix4.CreateTranslation(translation);
    }

    private static Vector3 Vector(Vector3f? v, Vector3 fallback) => v == null ? fallback : new(v.X, v.Y, v.Z);

    public static IReadOnlyList<Matrix4> FromInstances(TRINS instances)
    {
        if (instances.TypeName != "SrtInstanceBuffer" || instances.Version != 1)
            throw new InvalidDataException($"Unsupported instance buffer '{instances.TypeName}', version {instances.Version}.");
        var bytes = instances.Buffer?.Data ?? Array.Empty<byte>();
        if ((ulong)instances.Count * 64 != (ulong)bytes.Length)
            throw new InvalidDataException("Instance count does not match the matrix buffer size.");
        var result = new Matrix4[checked((int)instances.Count)];
        for (int i = 0; i < result.Length; i++)
        {
            var m = new float[16];
            for (int j = 0; j < 16; j++)
            {
                m[j] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 64 + j * 4, 4));
                if (!float.IsFinite(m[j])) throw new InvalidDataException("Instance transform contains a non-finite value.");
            }
            result[i] = new Matrix4(m[0], m[1], m[2], 0, m[4], m[5], m[6], 0,
                m[8], m[9], m[10], 0, m[12], m[13], m[14], 1);
        }
        return result;
    }
}
