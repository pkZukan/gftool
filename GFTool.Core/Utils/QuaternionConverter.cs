using GFToolCore.Flatbuffers.Utils;
using System.Numerics;

namespace GFToolCore.Utils
{
    public static class PackedQuaternionExtensions
    {
        public const float PI_DIVISOR = (float)(System.Math.PI / UInt16.MaxValue);
        public const float PI_ADDEND = (float)(System.Math.PI / 4.0);

        public static float ExpandFloat(ulong i)
        {
            return i * PI_DIVISOR - PI_ADDEND;
        }

        public static ulong QuantizeFloat(float i)
        {
            short result = Convert.ToInt16((i + PI_ADDEND) / PI_DIVISOR);
            return Convert.ToUInt64(result & 0x7FFF);
        }

        public static Quaternion Unpack(this PackedQuaternion pq)
        {
            UInt64 pack = (ulong)((pq.Z << 32) & (pq.Y << 16) & (pq.X));
            int missingComponent = (int)(pack & 3);
            bool isNegative = (pack & 4) == 0;

            float tx = ExpandFloat((pack >> 3) & 0x7FFF);
            float ty = ExpandFloat((pack >> (15 + 3)) & 0x7FFF);
            float tz = ExpandFloat((pack >> (30 + 3)) & 0x7FFF);
            float tw = 1.0f - (tx * tx + ty * ty + tz * tz);

            if (tw < 0.0f)
            {
                tw = 0.0f;
            }

            tw = (float)System.Math.Sqrt(tw);

            Quaternion result;

            switch (missingComponent)
            {
                case 0:
                    result = new Quaternion(tw, ty, tz, tx);
                    break;
                case 1:
                    result = new Quaternion(ty, tw, tz, tx);
                    break;
                case 2:
                    result = new Quaternion(ty, tz, tw, tx);
                    break;
                case 3:
                default:
                    result = new Quaternion(tx, ty, tz, tw);
                    break;
            }
            if (isNegative)
            {
                result *= -1.0f;
            }

            return result;
        }

        public static PackedQuaternion Pack(this Quaternion q)
        {
            q = Quaternion.Normalize(q);

            List<float> qc = new List<float> { q.X, q.Y, q.Z, q.W };

            float maxVal = qc.Max();
            float minVal = qc.Min();
            uint isNegative = 0;

            if (System.Math.Abs(minVal) > maxVal)
            {
                maxVal = minVal;
                isNegative = 1;
            }

            uint maxIndex = Convert.ToUInt16(qc.IndexOf(maxVal));

            ulong tx = 0;
            ulong ty = 0;
            ulong tz = 0;

            if (isNegative == 1)
            {

                for (int i = 0; i < 4; i++)
                {
                    qc[i] *= -1.0f;
                }
            }

            switch (maxIndex)
            {
                case 0:
                    tx = QuantizeFloat(qc[2]);
                    ty = QuantizeFloat(qc[1]);
                    tz = QuantizeFloat(qc[3]);
                    break;
                case 1:
                    tx = QuantizeFloat(qc[0]);
                    ty = QuantizeFloat(qc[2]);
                    tz = QuantizeFloat(qc[3]);
                    break;
                case 2:
                    tx = QuantizeFloat(qc[0]);
                    ty = QuantizeFloat(qc[1]);
                    tz = QuantizeFloat(qc[3]);
                    break;
                case 3:
                    tx = QuantizeFloat(qc[0]);
                    ty = QuantizeFloat(qc[1]);
                    tz = QuantizeFloat(qc[2]);
                    break;
            }

            ulong pack = ((tz << 30) | (ty << 15) | (tx));
            pack = (pack << 3) | ((isNegative << 2) | maxIndex);

            PackedQuaternion packed = new PackedQuaternion()
            {
                X = Convert.ToUInt16(pack & UInt16.MaxValue),
                Y = Convert.ToUInt16((pack >> 16) & UInt16.MaxValue),
                Z = Convert.ToUInt16((pack >> 32) & UInt16.MaxValue)

            };

            return packed;
        }

    }

}
