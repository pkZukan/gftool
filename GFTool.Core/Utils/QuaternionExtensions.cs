using Trinity.Core.Flatbuffers.Utils;
using System.Collections.Generic;
using System.Numerics;

namespace Trinity.Core.Utils
{
    public static class QuaternionExtensions
    {
        public const float PI_DIVISOR = (float)(System.Math.PI / 65536.0);
        public const float PI_ADDEND = (float)(System.Math.PI / 4.0);
        private const float PI_HALF = (float)(System.Math.PI / 2.0);
        private const float SCALE = 0x7FFF;

        private static float ExpandFloat(ulong i)
        {
            return (float)(i * (PI_HALF / SCALE) - PI_ADDEND);
        }

        private static int QuantizeFloat(float f)
        {
            int result = (int)((f + PI_ADDEND) / PI_DIVISOR);
            return result & 0x7FFF;
        }

        public static Quaternion Unpack(this PackedQuaternion pq)
        {
            ulong pack = (ulong)(((ulong)pq.Z << 32) | ((ulong)pq.Y << 16) | pq.X);
            float q1 = ExpandFloat((pack >> 3) & 0x7FFF);
            float q2 = ExpandFloat((pack >> 18) & 0x7FFF);
            float q3 = ExpandFloat((pack >> 33) & 0x7FFF);

            float sum = q1 * q1 + q2 * q2 + q3 * q3;
            float maxComponent = 1.0f - sum;
            if (maxComponent < 0.0f)
            {
                maxComponent = 0.0f;
            }
            maxComponent = (float)System.Math.Sqrt(maxComponent);

            int missingComponent = (int)(pack & 0x3);
            bool isNegative = (pack & 0x4) != 0;

            var values = new List<float> { q1, q2, q3 };
            values.Insert(missingComponent, maxComponent);

            float w = values[3];
            float x = values[0];
            float y = values[1];
            float z = values[2];
            if (isNegative)
            {
                x = -x;
                y = -y;
                z = -z;
                w = -w;
            }

            return new Quaternion(x, y, z, w);
        }

        public static PackedQuaternion Pack(this Quaternion q)
        {
            q = Quaternion.Normalize(q);

            List<float> qList = new List<float> { q.W, q.X, q.Y, q.Z };
            float maxVal = qList.Max();
            float minVal = qList.Min();
            int isNegative = 0;
            if (System.Math.Abs(minVal) > maxVal)
            {
                maxVal = minVal;
                isNegative = 1;
            }

            int maxIndex = qList.IndexOf(maxVal);
            if (isNegative == 1)
            {
                for (int i = 0; i < qList.Count; i++)
                {
                    qList[i] = -qList[i];
                }
            }

            int tx;
            int ty;
            int tz;
            switch (maxIndex)
            {
                case 0:
                    tx = QuantizeFloat(qList[1]);
                    ty = QuantizeFloat(qList[2]);
                    tz = QuantizeFloat(qList[3]);
                    break;
                case 1:
                    tx = QuantizeFloat(qList[2]);
                    ty = QuantizeFloat(qList[3]);
                    tz = QuantizeFloat(qList[0]);
                    break;
                case 2:
                    tx = QuantizeFloat(qList[1]);
                    ty = QuantizeFloat(qList[3]);
                    tz = QuantizeFloat(qList[0]);
                    break;
                default:
                    tx = QuantizeFloat(qList[1]);
                    ty = QuantizeFloat(qList[2]);
                    tz = QuantizeFloat(qList[0]);
                    break;
            }

            ulong pack = ((ulong)tz << 30) | ((ulong)ty << 15) | (ulong)tx;
            pack = (pack << 3) | ((ulong)(isNegative << 2) | (ulong)maxIndex);
            uint x = (uint)(pack & 0xFFFF);
            uint y = (uint)((pack >> 16) & 0xFFFF);
            uint z = (uint)((pack >> 32) & 0xFFFF);

            if (maxIndex == 0)
            {
                x = System.Math.Min(65535u, x + 3);
            }
            else if (x > 0)
            {
                x -= 1;
            }

            return new PackedQuaternion
            {
                X = Convert.ToUInt16(x),
                Y = Convert.ToUInt16(y),
                Z = Convert.ToUInt16(z)
            };
        }


        public static Dictionary<string, float> ToDictionary(this Quaternion quaternion)
        {
            return new Dictionary<string, float>
            {
                { "W", quaternion.X },
                { "X", quaternion.Y },
                { "Y", quaternion.Z },
                { "Z", quaternion.W },
            };
        }
    }

}
