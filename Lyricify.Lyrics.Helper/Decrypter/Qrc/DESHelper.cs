namespace Lyricify.Lyrics.Decrypter.Qrc
{
    public static class DESHelper
    {
        public static readonly uint ENCRYPT = 1;
        public static readonly uint DECRYPT = 0;

        private static uint BITNUM(byte[] a, int b, int c)
        {
            return (uint)((a[(b) / 32 * 4 + 3 - (b) % 32 / 8] >> (7 - (b % 8))) & 0x01) << (c);
        }

        private static byte BITNUMINTR(uint a, int b, int c)
        {
            return (byte)((((a) >> (31 - (b))) & 0x00000001) << (c));
        }

        private static uint BITNUMINTL(uint a, int b, int c)
        {
            return ((((a) << (b)) & 0x80000000) >> (c));
        }

        private static uint SBOXBIT(byte a)
        {
            return (uint)(((a) & 0x20) | (((a) & 0x1f) >> 1) | (((a) & 0x01) << 4));
        }

        private static readonly byte[] sbox1 = {
            14,  4,  13,  1,   2, 15,  11,  8,   3, 10,   6, 12,   5,  9,   0,  7,
             0, 15,   7,  4,  14,  2,  13,  1,  10,  6,  12, 11,   9,  5,   3,  8,
             4,  1,  14,  8,  13,  6,   2, 11,  15, 12,   9,  7,   3, 10,   5,  0,
            15, 12,   8,  2,   4,  9,   1,  7,   5, 11,   3, 14,  10,  0,   6, 13
        };

        private static readonly byte[] sbox2 = {
            15,  1,   8, 14,   6, 11,   3,  4,   9,  7,   2, 13,  12,  0,   5, 10,
             3, 13,   4,  7,  15,  2,   8, 15,  12,  0,   1, 10,   6,  9,  11,  5,
             0, 14,   7, 11,  10,  4,  13,  1,   5,  8,  12,  6,   9,  3,   2, 15,
            13,  8,  10,  1,   3, 15,   4,  2,  11,  6,   7, 12,   0,  5,  14,  9
        };

        private static readonly byte[] sbox3 = {
            10,  0,   9, 14,   6,  3,  15,  5,   1, 13,  12,  7,  11,  4,   2,  8,
            13,  7,   0,  9,   3,  4,   6, 10,   2,  8,   5, 14,  12, 11,  15,  1,
            13,  6,   4,  9,   8, 15,   3,  0,  11,  1,   2, 12,   5, 10,  14,  7,
             1, 10,  13,  0,   6,  9,   8,  7,   4, 15,  14,  3,  11,  5,   2, 12
        };

        private static readonly byte[] sbox4 = {
             7, 13,  14,  3,   0,  6,   9, 10,   1,  2,   8,  5,  11, 12,   4, 15,
            13,  8,  11,  5,   6, 15,   0,  3,   4,  7,   2, 12,   1, 10,  14,  9,
            10,  6,   9,  0,  12, 11,   7, 13,  15,  1,   3, 14,   5,  2,   8,  4,
             3, 15,   0,  6,  10, 10,  13,  8,   9,  4,   5, 11,  12,  7,   2, 14
        };

        private static readonly byte[] sbox5 = {
             2, 12,   4,  1,   7, 10,  11,  6,   8,  5,   3, 15,  13,  0,  14,  9,
            14, 11,   2, 12,   4,  7,  13,  1,   5,  0,  15, 10,   3,  9,   8,  6,
             4,  2,   1, 11,  10, 13,   7,  8,  15,  9,  12,  5,   6,  3,   0, 14,
            11,  8,  12,  7,   1, 14,   2, 13,   6, 15,   0,  9,  10,  4,   5,  3
        };

        private static readonly byte[] sbox6 = {
            12,  1,  10, 15,   9,  2,   6,  8,   0, 13,   3,  4,  14,  7,   5, 11,
            10, 15,   4,  2,   7, 12,   9,  5,   6,  1,  13, 14,   0, 11,   3,  8,
             9, 14,  15,  5,   2,  8,  12,  3,   7,  0,   4, 10,   1, 13,  11,  6,
             4,  3,   2, 12,   9,  5,  15, 10,  11, 14,   1,  7,   6,  0,   8, 13
        };

        private static readonly byte[] sbox7 = {
             4, 11,   2, 14,  15,  0,   8, 13,   3, 12,   9,  7,   5, 10,   6,  1,
            13,  0,  11,  7,   4,  9,   1, 10,  14,  3,   5, 12,   2, 15,   8,  6,
             1,  4,  11, 13,  12,  3,   7, 14,  10, 15,   6,  8,   0,  5,   9,  2,
             6, 11,  13,  8,   1,  4,  10,  7,   9,  5,   0, 15,  14,  2,   3, 12
        };

        private static readonly byte[] sbox8 = {
            13,  2,   8,  4,   6, 15,  11,  1,  10,  9,   3, 14,   5,  0,  12,  7,
             1, 15,  13,  8,  10,  3,   7,  4,  12,  5,   6, 11,   0, 14,   9,  2,
             7, 11,   4,  1,   9, 12,  14,  2,   0,  6,  10, 13,  15,  3,   5,  8,
             2,  1,  14,  7,   4, 10,   8, 13,  15, 12,   9,  0,   3,  5,   6, 11
        };

        public static void KeySchedule(byte[] key, byte[][] schedule, uint mode)
        {
            uint i, j, toGen, C, D;
            uint[] key_rnd_shift = { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };
            uint[] key_perm_c = { 56, 48, 40, 32, 24, 16, 8, 0, 57, 49, 41, 33, 25, 17,
                9,1,58,50,42,34,26,18,10,2,59,51,43,35 };
            uint[] key_perm_d = { 62,54,46,38,30,22,14,6,61,53,45,37,29,21,
                13,5,60,52,44,36,28,20,12,4,27,19,11,3 };
            uint[] key_compression = { 13,16,10,23,0,4,2,27,14,5,20,9,
                22,18,11,3,25,7,15,6,26,19,12,1,
                40,51,30,36,46,54,29,39,50,44,32,47,
                43,48,38,55,33,52,45,41,49,35,28,31 };

            for (i = 0, j = 31, C = 0; i < 28; ++i, --j)
                C |= BITNUM(key, (int)key_perm_c[i], (int)j);

            for (i = 0, j = 31, D = 0; i < 28; ++i, --j)
                D |= BITNUM(key, (int)key_perm_d[i], (int)j);

            for (i = 0; i < 16; ++i)
            {
                C = ((C << (int)key_rnd_shift[i]) | (C >> (28 - (int)key_rnd_shift[i]))) & 0xfffffff0;
                D = ((D << (int)key_rnd_shift[i]) | (D >> (28 - (int)key_rnd_shift[i]))) & 0xfffffff0;

                if (mode == DECRYPT)
                    toGen = 15 - i;
                else
                    toGen = i;

                for (j = 0; j < 6; ++j)
                    schedule[toGen][j] = 0;

                for (j = 0; j < 24; ++j)
                    schedule[toGen][j / 8] |= BITNUMINTR(C, (int)key_compression[j], (int)(7 - (j % 8)));

                for (; j < 48; ++j)
                    schedule[toGen][j / 8] |= BITNUMINTR(D, (int)key_compression[j] - 27, (int)(7 - (j % 8)));
            }
        }

        private static void IP(uint[] state, byte[] input)
        {
            state[0] = BITNUM(input, 57, 31) | BITNUM(input, 49, 30) | BITNUM(input, 41, 29) | BITNUM(input, 33, 28) |
                BITNUM(input, 25, 27) | BITNUM(input, 17, 26) | BITNUM(input, 9, 25) | BITNUM(input, 1, 24) |
                BITNUM(input, 59, 23) | BITNUM(input, 51, 22) | BITNUM(input, 43, 21) | BITNUM(input, 35, 20) |
                BITNUM(input, 27, 19) | BITNUM(input, 19, 18) | BITNUM(input, 11, 17) | BITNUM(input, 3, 16) |
                BITNUM(input, 61, 15) | BITNUM(input, 53, 14) | BITNUM(input, 45, 13) | BITNUM(input, 37, 12) |
                BITNUM(input, 29, 11) | BITNUM(input, 21, 10) | BITNUM(input, 13, 9) | BITNUM(input, 5, 8) |
                BITNUM(input, 63, 7) | BITNUM(input, 55, 6) | BITNUM(input, 47, 5) | BITNUM(input, 39, 4) |
                BITNUM(input, 31, 3) | BITNUM(input, 23, 2) | BITNUM(input, 15, 1) | BITNUM(input, 7, 0);

            state[1] = BITNUM(input, 56, 31) | BITNUM(input, 48, 30) | BITNUM(input, 40, 29) | BITNUM(input, 32, 28) |
                BITNUM(input, 24, 27) | BITNUM(input, 16, 26) | BITNUM(input, 8, 25) | BITNUM(input, 0, 24) |
                BITNUM(input, 58, 23) | BITNUM(input, 50, 22) | BITNUM(input, 42, 21) | BITNUM(input, 34, 20) |
                BITNUM(input, 26, 19) | BITNUM(input, 18, 18) | BITNUM(input, 10, 17) | BITNUM(input, 2, 16) |
                BITNUM(input, 60, 15) | BITNUM(input, 52, 14) | BITNUM(input, 44, 13) | BITNUM(input, 36, 12) |
                BITNUM(input, 28, 11) | BITNUM(input, 20, 10) | BITNUM(input, 12, 9) | BITNUM(input, 4, 8) |
                BITNUM(input, 62, 7) | BITNUM(input, 54, 6) | BITNUM(input, 46, 5) | BITNUM(input, 38, 4) |
                BITNUM(input, 30, 3) | BITNUM(input, 22, 2) | BITNUM(input, 14, 1) | BITNUM(input, 6, 0);
        }

        private static void InvIP(uint[] state, byte[] input)
        {
            input[3] = (byte)(BITNUMINTR(state[1], 7, 7) | BITNUMINTR(state[0], 7, 6) | BITNUMINTR(state[1], 15, 5) |
                BITNUMINTR(state[0], 15, 4) | BITNUMINTR(state[1], 23, 3) | BITNUMINTR(state[0], 23, 2) |
                BITNUMINTR(state[1], 31, 1) | BITNUMINTR(state[0], 31, 0));

            input[2] = (byte)(BITNUMINTR(state[1], 6, 7) | BITNUMINTR(state[0], 6, 6) | BITNUMINTR(state[1], 14, 5) |
                BITNUMINTR(state[0], 14, 4) | BITNUMINTR(state[1], 22, 3) | BITNUMINTR(state[0], 22, 2) |
                BITNUMINTR(state[1], 30, 1) | BITNUMINTR(state[0], 30, 0));

            input[1] = (byte)(BITNUMINTR(state[1], 5, 7) | BITNUMINTR(state[0], 5, 6) | BITNUMINTR(state[1], 13, 5) |
                BITNUMINTR(state[0], 13, 4) | BITNUMINTR(state[1], 21, 3) | BITNUMINTR(state[0], 21, 2) |
                BITNUMINTR(state[1], 29, 1) | BITNUMINTR(state[0], 29, 0));

            input[0] = (byte)(BITNUMINTR(state[1], 4, 7) | BITNUMINTR(state[0], 4, 6) | BITNUMINTR(state[1], 12, 5) |
                BITNUMINTR(state[0], 12, 4) | BITNUMINTR(state[1], 20, 3) | BITNUMINTR(state[0], 20, 2) |
                BITNUMINTR(state[1], 28, 1) | BITNUMINTR(state[0], 28, 0));

            input[7] = (byte)(BITNUMINTR(state[1], 3, 7) | BITNUMINTR(state[0], 3, 6) | BITNUMINTR(state[1], 11, 5) |
                BITNUMINTR(state[0], 11, 4) | BITNUMINTR(state[1], 19, 3) | BITNUMINTR(state[0], 19, 2) |
                BITNUMINTR(state[1], 27, 1) | BITNUMINTR(state[0], 27, 0));

            input[6] = (byte)(BITNUMINTR(state[1], 2, 7) | BITNUMINTR(state[0], 2, 6) | BITNUMINTR(state[1], 10, 5) |
                BITNUMINTR(state[0], 10, 4) | BITNUMINTR(state[1], 18, 3) | BITNUMINTR(state[0], 18, 2) |
                BITNUMINTR(state[1], 26, 1) | BITNUMINTR(state[0], 26, 0));

            input[5] = (byte)(BITNUMINTR(state[1], 1, 7) | BITNUMINTR(state[0], 1, 6) | BITNUMINTR(state[1], 9, 5) |
                BITNUMINTR(state[0], 9, 4) | BITNUMINTR(state[1], 17, 3) | BITNUMINTR(state[0], 17, 2) |
                BITNUMINTR(state[1], 25, 1) | BITNUMINTR(state[0], 25, 0));

            input[4] = (byte)(BITNUMINTR(state[1], 0, 7) | BITNUMINTR(state[0], 0, 6) | BITNUMINTR(state[1], 8, 5) |
                BITNUMINTR(state[0], 8, 4) | BITNUMINTR(state[1], 16, 3) | BITNUMINTR(state[0], 16, 2) |
                BITNUMINTR(state[1], 24, 1) | BITNUMINTR(state[0], 24, 0));
        }

        private static uint F(uint state, byte[] key)
        {
            byte[] lrgstate = new byte[6];
            uint t1, t2;

            t1 = BITNUMINTL(state, 31, 0) | ((state & 0xf0000000) >> 1) | BITNUMINTL(state, 4, 5) |
                BITNUMINTL(state, 3, 6) | ((state & 0x0f000000) >> 3) | BITNUMINTL(state, 8, 11) |
                BITNUMINTL(state, 7, 12) | ((state & 0x00f00000) >> 5) | BITNUMINTL(state, 12, 17) |
                BITNUMINTL(state, 11, 18) | ((state & 0x000f0000) >> 7) | BITNUMINTL(state, 16, 23);

            t2 = BITNUMINTL(state, 15, 0) | ((state & 0x0000f000) << 15) | BITNUMINTL(state, 20, 5) |
                BITNUMINTL(state, 19, 6) | ((state & 0x00000f00) << 13) | BITNUMINTL(state, 24, 11) |
                BITNUMINTL(state, 23, 12) | ((state & 0x000000f0) << 11) | BITNUMINTL(state, 28, 17) |
                BITNUMINTL(state, 27, 18) | ((state & 0x0000000f) << 9) | BITNUMINTL(state, 0, 23);

            lrgstate[0] = (byte)((t1 >> 24) & 0x000000ff);
            lrgstate[1] = (byte)((t1 >> 16) & 0x000000ff);
            lrgstate[2] = (byte)((t1 >> 8) & 0x000000ff);
            lrgstate[3] = (byte)((t2 >> 24) & 0x000000ff);
            lrgstate[4] = (byte)((t2 >> 16) & 0x000000ff);
            lrgstate[5] = (byte)((t2 >> 8) & 0x000000ff);

            lrgstate[0] ^= key[0];
            lrgstate[1] ^= key[1];
            lrgstate[2] ^= key[2];
            lrgstate[3] ^= key[3];
            lrgstate[4] ^= key[4];
            lrgstate[5] ^= key[5];

            state = (uint)((sbox1[SBOXBIT((byte)(lrgstate[0] >> 2))] << 28) |
                (sbox2[SBOXBIT((byte)(((lrgstate[0] & 0x03) << 4) | (lrgstate[1] >> 4)))] << 24) |
                (sbox3[SBOXBIT((byte)(((lrgstate[1] & 0x0f) << 2) | (lrgstate[2] >> 6)))] << 20) |
                (sbox4[SBOXBIT((byte)(lrgstate[2] & 0x3f))] << 16) |
                (sbox5[SBOXBIT((byte)(lrgstate[3] >> 2))] << 12) |
                (sbox6[SBOXBIT((byte)(((lrgstate[3] & 0x03) << 4) | (lrgstate[4] >> 4)))] << 8) |
                (sbox7[SBOXBIT((byte)(((lrgstate[4] & 0x0f) << 2) | (lrgstate[5] >> 6)))] << 4) |
                sbox8[SBOXBIT((byte)(lrgstate[5] & 0x3f))]);

            state = BITNUMINTL(state, 15, 0) | BITNUMINTL(state, 6, 1) | BITNUMINTL(state, 19, 2) |
                BITNUMINTL(state, 20, 3) | BITNUMINTL(state, 28, 4) | BITNUMINTL(state, 11, 5) |
                BITNUMINTL(state, 27, 6) | BITNUMINTL(state, 16, 7) | BITNUMINTL(state, 0, 8) |
                BITNUMINTL(state, 14, 9) | BITNUMINTL(state, 22, 10) | BITNUMINTL(state, 25, 11) |
                BITNUMINTL(state, 4, 12) | BITNUMINTL(state, 17, 13) | BITNUMINTL(state, 30, 14) |
                BITNUMINTL(state, 9, 15) | BITNUMINTL(state, 1, 16) | BITNUMINTL(state, 7, 17) |
                BITNUMINTL(state, 23, 18) | BITNUMINTL(state, 13, 19) | BITNUMINTL(state, 31, 20) |
                BITNUMINTL(state, 26, 21) | BITNUMINTL(state, 2, 22) | BITNUMINTL(state, 8, 23) |
                BITNUMINTL(state, 18, 24) | BITNUMINTL(state, 12, 25) | BITNUMINTL(state, 29, 26) |
                BITNUMINTL(state, 5, 27) | BITNUMINTL(state, 21, 28) | BITNUMINTL(state, 10, 29) |
                BITNUMINTL(state, 3, 30) | BITNUMINTL(state, 24, 31);

            return (state);
        }

        public static void Crypt(byte[] input, byte[] output, byte[][] key)
        {
            uint[] state = new uint[2];
            uint idx, t;

            IP(state, input);

            for (idx = 0; idx < 15; ++idx)
            {
                t = state[1];
                state[1] = F(state[1], key[idx]) ^ state[0];
                state[0] = t;
            }

            state[0] = F(state[1], key[15]) ^ state[0];

            InvIP(state, output);
        }
        public static void TripleDESKeySetup(byte[] key, byte[][][] schedule, uint mode)
        {
            if (mode == ENCRYPT)
            {
                KeySchedule(key[0..], schedule[0], mode);
                KeySchedule(key[8..], schedule[1], DECRYPT);
                KeySchedule(key[16..], schedule[2], mode);
            }
            else /*if (mode == DES_DECRYPT*/
            {
                KeySchedule(key[0..], schedule[2], mode);
                KeySchedule(key[8..], schedule[1], ENCRYPT);
                KeySchedule(key[16..], schedule[0], mode);
            }
        }
        public static void TripleDESCrypt(byte[] input, byte[] output, byte[][][] key)
        {
            Crypt(input, output, key[0]);
            Crypt(output, output, key[1]);
            Crypt(output, output, key[2]);
        }
    }
}
