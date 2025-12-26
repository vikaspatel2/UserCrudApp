using System;
using System.Collections.Generic;

public static class MyQrEncoder
{
    // Step 1: Encode data in byte mode for version 1-L QR codes
    public static List<bool> EncodeByteMode(string data)
    {
        List<bool> bits = new List<bool>();

        // [1] Mode indicator (byte mode)
        bits.AddRange(new bool[] { false, true, false, false });  // 0100

        // [2] Character count (8 bits for version 1)
        int len = data.Length;
        for (int i = 7; i >= 0; i--)
            bits.Add(((len >> i) & 1) == 1);

        // [3] Data (8 bits per char)
        foreach (char c in data)
        {
            for (int i = 7; i >= 0; i--)
                bits.Add(((c >> i) & 1) == 1);
        }

        // [4] Terminator (up to 4 bits)
        int maxDataBits = 152; // Version 1-L: 19 bytes = 152 bits
        int terminatorLen = Math.Min(4, maxDataBits - bits.Count);
        for (int i = 0; i < terminatorLen; i++) bits.Add(false);

        // [5] Pad to nearest byte
        while (bits.Count % 8 != 0) bits.Add(false);

        // [6] Add pad bytes 0xEC, 0x11 alternately up to 19 bytes
        // [6] Add pad bytes: MUST start with 0xEC, then 0x11 alternately
        int bytesNeeded = 19; // Version 1-L
        bool useEC = true;

        while (bits.Count / 8 < bytesNeeded)
        {
            byte padByte = useEC ? (byte)0xEC : (byte)0x11;
            useEC = !useEC;

            for (int i = 7; i >= 0; i--)
                bits.Add(((padByte >> i) & 1) == 1);
        }


        return bits;
    }

    public static byte[] BitsToBytes(List<bool> bits)
    {
        int len = bits.Count / 8;
        byte[] result = new byte[len];
        for (int i = 0; i < len; i++)
        {
            byte b = 0;
            for (int j = 0; j < 8; j++)
            {
                if (bits[i * 8 + j]) b |= (byte)(1 << (7 - j));
            }
            result[i] = b;
        }
        return result;
    }

    public static bool[,] MakeMatrix(byte[] codewords)
    {
        var a = BitConverter.ToString(codewords);
        int size = 21;
        bool[,] m = new bool[size, size];
        bool[,] reserved = new bool[size, size];

        // Finder patterns
        void Finder(int x, int y)
        {
            for (int dy = 0; dy < 7; dy++)
                for (int dx = 0; dx < 7; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    int r = (dx == 0 || dx == 6 || dy == 0 || dy == 6) ? 1
                        : (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4) ? 1 : 0;
                    m[xx, yy] = r == 1;
                    reserved[xx, yy] = true;
                }
        }
        Finder(0, 0); Finder(size - 7, 0); Finder(0, size - 7);

        void Separator(int x, int y)
        {
            for (int i = -1; i <= 7; i++)
                for (int j = -1; j <= 7; j++)
                {
                    int xx = x + i, yy = y + j;
                    if (xx < 0 || yy < 0 || xx >= size || yy >= size) continue;
                    if (i == -1 || i == 7 || j == -1 || j == 7)
                    {
                        reserved[xx, yy] = true;
                        m[xx, yy] = false;
                    }
                }
        }
        Separator(0, 0);
        Separator(size - 7, 0);
        Separator(0, size - 7);


        // Timing patterns and reserve
        for (int i = 8; i < size - 8; i++)
        {
            m[i, 6] = (i % 2 == 0);
            reserved[i, 6] = true;
            m[6, i] = (i % 2 == 0);
            reserved[6, i] = true;
        }

        // Dark module (Version 1 requirement)
        m[8, size - 8] = true;
        reserved[8, size - 8] = true;


        // Reserve format info
        for (int i = 0; i <= 8; i++)
        {
            if (i != 6)
            {
                reserved[8, i] = true;
                reserved[i, 8] = true;
            }
        }
        for (int i = size - 8; i < size; i++)
        {
            reserved[8, i] = true;
            reserved[i, 8] = true;
        }

        // Reserve remaining format info areas (bottom-left & top-right)
        for (int i = 0; i < 8; i++)
        {
            reserved[size - 1 - i, 8] = true; // bottom-left
            reserved[8, size - 1 - i] = true; // top-right
        }

        int bitIndex = 0;
        int row = size - 1;
        int col = size - 1;
        int direction = -1;

        while (col > 0)
        {
            if (col == 6) col--; // skip timing column

            while (row >= 0 && row < size)
            {
                for (int c = 0; c < 2; c++)
                {
                    int x = col - c;
                    int y = row;

                    //if (!reserved[x, y])
                    //{
                    //    bool val = false;
                    //    if (bitIndex < codewords.Length * 8)
                    //    {
                    //        int byteIndex = bitIndex / 8;
                    //        int bitInByte = 7 - (bitIndex % 8);
                    //        val = ((codewords[byteIndex] >> bitInByte) & 1) == 1;
                    //    }
                    //    m[x, y] = val;
                    //    bitIndex++;
                    //}
                    if (!reserved[x, y])
                    {
                        int byteIndex = bitIndex >> 3;
                        int bitInByte = 7 - (bitIndex & 7);

                        bool val = byteIndex < codewords.Length &&
                                   ((codewords[byteIndex] >> bitInByte) & 1) == 1;

                        m[x, y] = val;
                        bitIndex++;
                    }
                    Console.WriteLine(bitIndex);

                }
                row += direction;
            }

            row -= direction;
            direction = -direction;
            col -= 2;
        }


        // --- Mask (pattern 0: (x+y)%2==0) ---
        for (int yy = 0; yy < size; yy++)
            for (int xx = 0; xx < size; xx++)
                if (!reserved[xx, yy])
                    m[xx, yy] ^= ((xx + yy) % 2 == 0);

        //// ===== WRITE FORMAT INFO (AFTER MASK) =====
        //int format = FormatInfoBits();
        //int[] formatBits = new int[15];
        //for (int i = 0; i < 15; i++)
        //    formatBits[i] = (format >> (14 - i)) & 1;

        //// Vertical (top-left)
        //for (int i = 0; i < 6; i++) m[8, i] = formatBits[i] == 1;
        //m[8, 7] = formatBits[6] == 1;
        //m[8, 8] = formatBits[7] == 1;
        //m[7, 8] = formatBits[8] == 1;
        //for (int i = 9; i < 15; i++) m[14 - i, 8] = formatBits[i] == 1;

        //// Horizontal (top-right & bottom-left)
        //for (int i = 0; i < 8; i++) m[size - 1 - i, 8] = formatBits[i] == 1;
        //for (int i = 8; i < 15; i++) m[8, size - 15 + i] = formatBits[i] == 1;
        // ===== WRITE FORMAT INFO (L + Mask 0) =====
        int format = 0b111011111000100; // EC=L, Mask=0

        int[] fb = new int[15];
        for (int i = 0; i < 15; i++)
            fb[i] = (format >> i) & 1;

        // ---- top-left format info ----
        m[8, 0] = fb[0] == 1;
        m[8, 1] = fb[1] == 1;
        m[8, 2] = fb[2] == 1;
        m[8, 3] = fb[3] == 1;
        m[8, 4] = fb[4] == 1;
        m[8, 5] = fb[5] == 1;
        m[8, 7] = fb[6] == 1;
        m[8, 8] = fb[7] == 1;
        m[7, 8] = fb[8] == 1;
        m[5, 8] = fb[9] == 1;
        m[4, 8] = fb[10] == 1;
        m[3, 8] = fb[11] == 1;
        m[2, 8] = fb[12] == 1;
        m[1, 8] = fb[13] == 1;
        m[0, 8] = fb[14] == 1;

        // ---- mirror copy ----
        m[size - 1, 8] = fb[0] == 1;
        m[size - 2, 8] = fb[1] == 1;
        m[size - 3, 8] = fb[2] == 1;
        m[size - 4, 8] = fb[3] == 1;
        m[size - 5, 8] = fb[4] == 1;
        m[size - 6, 8] = fb[5] == 1;
        m[size - 7, 8] = fb[6] == 1;

        m[8, size - 8] = fb[7] == 1;
        m[8, size - 7] = fb[8] == 1;
        m[8, size - 6] = fb[9] == 1;
        m[8, size - 5] = fb[10] == 1;
        m[8, size - 4] = fb[11] == 1;
        m[8, size - 3] = fb[12] == 1;
        m[8, size - 2] = fb[13] == 1;
        m[8, size - 1] = fb[14] == 1;


        return m;
    }

    //public static int FormatInfoBits()
    //{
    //    // For Level L (01), Mask 0 (000): 01 000, with BCH code it's 0b111011111000100
    //    return 0b111011111000100;
    //}
}