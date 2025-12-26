namespace UserCrudApp.Helpers
{
    public class ReedSolomonEncoder
    {
        static readonly byte[] LogTable = new byte[256];
        static readonly byte[] ExpTable = new byte[512];

        static ReedSolomonEncoder()
        {
            int x = 1; // MUST be int, not byte

            for (int i = 0; i < 255; i++)
            {
                ExpTable[i] = (byte)x;
                LogTable[(byte)x] = (byte)i;

                x <<= 1;               // multiply by 2
                if ((x & 0x100) != 0)  // if overflow beyond 8 bits
                    x ^= 0x11D;        // reduce by QR polynomial

                x &= 0xFF;             // keep it within 8 bits
            }

            for (int i = 255; i < 512; i++)
                ExpTable[i] = ExpTable[i - 255];
        }

        // Multiply in GF(256)
        static byte GFmul(byte a, byte b)
        {
            return a == 0 || b == 0 ? (byte)0 : ExpTable[LogTable[a] + LogTable[b]];
        }

        // Main interface: error correction for degree N (7 for v1-L)
        public static byte[] Encode(byte[] data, int ecLength)
        {
            byte[] gen = GeneratorPoly(ecLength);
            byte[] msg = new byte[data.Length + ecLength];
            Array.Copy(data, msg, data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                byte coef = msg[i];
                if (coef == 0) continue;
                for (int j = 0; j < gen.Length; j++)
                    msg[i + j] ^= GFmul(gen[j], coef);
            }
            byte[] ecc = new byte[ecLength];
            Array.Copy(msg, data.Length, ecc, 0, ecLength);
            return ecc;
        }

        // QR generator poly (by brute force for QR v1-L, 7 ECC bytes)
        static byte[] GeneratorPoly(int degree)
        {
            List<byte> poly = new List<byte> { 1 };
            for (int i = 0; i < degree; i++)
            {
                List<byte> next = new List<byte>(poly.Count + 1);
                next.Add(0);
                next.AddRange(poly);
                for (int j = 0; j < poly.Count; j++)
                    next[j] ^= GFmul(poly[j], ExpTable[i]);
                poly = next;
            }
            return poly.ToArray();
        }
    }
}
