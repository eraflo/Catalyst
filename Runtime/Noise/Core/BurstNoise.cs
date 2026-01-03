using Unity.Burst;
using Unity.Mathematics;
using System;

namespace Eraflo.Catalyst.Noise
{
    /// <summary>
    /// High-performance 4D Simplex Noise implementation.
    /// Burst-optimized using ReadOnlySpan to avoid static array fields.
    /// </summary>
    public static class BurstNoise
    {
        private const float F4 = 0.309016994374947451f;
        private const float G4 = 0.138196601125010504f;
        private const float F3 = 1.0f / 3.0f;
        private const float G3 = 1.0f / 6.0f;
        private const float F2 = 0.366025403784439f;
        private const float G2 = 0.211324865405187f;

        // Permutation table encoded as ReadOnlySpan (Burst-friendly)
        private static ReadOnlySpan<byte> PermutationTable => new byte[] {
            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 44, 160, 43, 39, 129, 253, 203, 41, 254, 25, 155, 105, 92, 65, 176, 38, 243, 174, 46, 145, 40, 109, 79, 179, 78, 64, 72, 161, 181, 51, 233, 141, 105, 132, 248, 15, 113, 115, 129, 115, 5, 31, 28, 248, 150, 133, 154, 28, 124, 59, 204, 198, 152, 252, 126, 110, 135, 82, 155, 142, 201, 115, 41, 115, 73, 66, 192, 125, 2, 53, 194, 170, 7, 101, 24, 212, 243, 94, 220, 254, 89, 16, 133, 112, 250, 110, 107, 139, 17, 221, 91, 103, 179, 68, 12, 47, 73, 81, 206, 94, 9, 54, 235, 164, 20, 14, 231, 202, 147, 58, 113, 112, 183, 5, 25, 60, 130, 153, 7, 232, 80, 2, 75, 160, 216, 225, 34, 243, 172, 242, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 151
        };

        // Gradients 4D encoded as sbyte (pairs of coordinates)
        private static ReadOnlySpan<sbyte> Grad4Table => new sbyte[] {
            0,1,1,1,  0,1,1,-1,  0,1,-1,1,  0,1,-1,-1,
            0,-1,1,1, 0,-1,1,-1, 0,-1,-1,1, 0,-1,-1,-1,
            1,0,1,1,  1,0,1,-1,  1,0,-1,1,  1,0,-1,-1,
            -1,0,1,1, -1,0,1,-1, -1,0,-1,1, -1,0,-1,-1,
            1,1,0,1,  1,1,0,-1,  1,-1,0,1,  1,-1,0,-1,
            -1,1,0,1, -1,1,0,-1, -1,-1,0,1, -1,-1,0,-1,
            1,1,1,0,  1,1,-1,0,  1,-1,1,0,  1,-1,-1,0,
            -1,1,1,0, -1,1,-1,0, -1,-1,1,0, -1,-1,-1,0
        };

        // Gradients 3D encoded as sbyte
        private static ReadOnlySpan<sbyte> Grad3Table => new sbyte[] {
            1,1,0, -1,1,0, 1,-1,0, -1,-1,0,
            1,0,1, -1,0,1, 1,0,-1, -1,0,-1,
            0,1,1, 0,-1,1, 0,1,-1, 0,-1,-1
        };

        private static int GetPerm(int i) => PermutationTable[i & 255];

        private static int3 GetGrad3(int i)
        {
            int idx = (i % 12) * 3;
            var table = Grad3Table;
            return new int3(table[idx], table[idx + 1], table[idx + 2]);
        }

        private static int4 GetGrad4(int i)
        {
            int idx = (i & 31) * 4;
            var table = Grad4Table;
            return new int4(table[idx], table[idx + 1], table[idx + 2], table[idx + 3]);
        }

        public static float Sample2D(float x, float y)
        {
            return Sample2D(new float2(x, y));
        }

        public static float Sample2D(in float2 coord)
        {
            float s = (coord.x + coord.y) * F2;
            int i = FastFloor(coord.x + s);
            int j = FastFloor(coord.y + s);
            float t = (i + j) * G2;
            float x0 = coord.x - (i - t);
            float y0 = coord.y - (j - t);
            int i1 = x0 > y0 ? 1 : 0;
            int j1 = x0 > y0 ? 0 : 1;
            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1.0f + 2.0f * G2;
            float y2 = y0 - 1.0f + 2.0f * G2;
            int ii = i & 255;
            int jj = j & 255;
            float n0 = Contribution2D(x0, y0, ii, jj);
            float n1 = Contribution2D(x1, y1, ii + i1, jj + j1);
            float n2 = Contribution2D(x2, y2, ii + 1, jj + 1);
            return 70.0f * (n0 + n1 + n2);
        }

        private static float Contribution2D(float x, float y, int i, int j)
        {
            float t = 0.5f - x * x - y * y;
            if (t < 0) return 0;
            t *= t;
            int gi = GetPerm(i + GetPerm(j)) % 12;
            int3 g = GetGrad3(gi);
            return t * t * (g.x * x + g.y * y);
        }

        public static float Sample3D(float x, float y, float z)
        {
            return Sample3D(new float3(x, y, z));
        }

        public static float Sample3D(in float3 coord)
        {
            float s = (coord.x + coord.y + coord.z) * F3;
            int i = FastFloor(coord.x + s);
            int j = FastFloor(coord.y + s);
            int k = FastFloor(coord.z + s);
            float t = (i + j + k) * G3;
            float x0 = coord.x - (i - t);
            float y0 = coord.y - (j - t);
            float z0 = coord.z - (k - t);
            int i1, j1, k1, i2, j2, k2;
            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }
            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3;
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3;
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            float n0 = Contribution3D(x0, y0, z0, ii, jj, kk);
            float n1 = Contribution3D(x1, y1, z1, ii + i1, jj + j1, kk + k1);
            float n2 = Contribution3D(x2, y2, z2, ii + i2, jj + j2, kk + k2);
            float n3 = Contribution3D(x3, y3, z3, ii + 1, jj + 1, kk + 1);
            return 32.0f * (n0 + n1 + n2 + n3);
        }

        private static float Contribution3D(float x, float y, float z, int i, int j, int k)
        {
            float t = 0.6f - x * x - y * y - z * z;
            if (t < 0) return 0;
            t *= t;
            int gi = GetPerm(i + GetPerm(j + GetPerm(k))) % 12;
            int3 g = GetGrad3(gi);
            return t * t * (g.x * x + g.y * y + g.z * z);
        }

        public static float Sample4D(float x, float y, float z, float w)
        {
            return Sample4D(new float4(x, y, z, w));
        }

        public static float Sample4D(in float4 coord)
        {
            float s = (coord.x + coord.y + coord.z + coord.w) * F4;
            int i = FastFloor(coord.x + s);
            int j = FastFloor(coord.y + s);
            int k = FastFloor(coord.z + s);
            int l = FastFloor(coord.w + s);
            float t = (i + j + k + l) * G4;
            float x0 = coord.x - (i - t);
            float y0 = coord.y - (j - t);
            float z0 = coord.z - (k - t);
            float w0 = coord.w - (l - t);
            int rankx = 0, ranky = 0, rankz = 0, rankw = 0;
            if (x0 > y0) rankx++; else ranky++;
            if (x0 > z0) rankx++; else rankz++;
            if (x0 > w0) rankx++; else rankw++;
            if (y0 > z0) ranky++; else rankz++;
            if (y0 > w0) ranky++; else rankw++;
            if (z0 > w0) rankz++; else rankw++;
            int i1 = rankx >= 3 ? 1 : 0;
            int j1 = ranky >= 3 ? 1 : 0;
            int k1 = rankz >= 3 ? 1 : 0;
            int l1 = rankw >= 3 ? 1 : 0;
            int i2 = rankx >= 2 ? 1 : 0;
            int j2 = ranky >= 2 ? 1 : 0;
            int k2 = rankz >= 2 ? 1 : 0;
            int l2 = rankw >= 2 ? 1 : 0;
            int i3 = rankx >= 1 ? 1 : 0;
            int j3 = ranky >= 1 ? 1 : 0;
            int k3 = rankz >= 1 ? 1 : 0;
            int l3 = rankw >= 1 ? 1 : 0;
            float x1 = x0 - i1 + G4;
            float y1 = y0 - j1 + G4;
            float z1 = z0 - k1 + G4;
            float w1 = w0 - l1 + G4;
            float x2 = x0 - i2 + 2.0f * G4;
            float y2 = y0 - j2 + 2.0f * G4;
            float z2 = z0 - k2 + 2.0f * G4;
            float w2 = w0 - l2 + 2.0f * G4;
            float x3 = x0 - i3 + 3.0f * G4;
            float y3 = y0 - j3 + 3.0f * G4;
            float z3 = z0 - k3 + 3.0f * G4;
            float w3 = w0 - l3 + 3.0f * G4;
            float x4 = x0 - 1.0f + 4.0f * G4;
            float y4 = y0 - 1.0f + 4.0f * G4;
            float z4 = z0 - 1.0f + 4.0f * G4;
            float w4 = w0 - 1.0f + 4.0f * G4;
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            int ll = l & 255;
            float n0 = Contribution4D(x0, y0, z0, w0, ii, jj, kk, ll);
            float n1 = Contribution4D(x1, y1, z1, w1, ii + i1, jj + j1, kk + k1, ll + l1);
            float n2 = Contribution4D(x2, y2, z2, w2, ii + i2, jj + j2, kk + k2, ll + l2);
            float n3 = Contribution4D(x3, y3, z3, w3, ii + i3, jj + j3, kk + k3, ll + l3);
            float n4 = Contribution4D(x4, y4, z4, w4, ii + 1, jj + 1, kk + 1, ll + 1);
            return 27.0f * (n0 + n1 + n2 + n3 + n4);
        }

        private static float Contribution4D(float x, float y, float z, float w, int i, int j, int k, int l)
        {
            float t = 0.6f - x * x - y * y - z * z - w * w;
            if (t < 0) return 0;
            t *= t;
            int gi = GetPerm(i + GetPerm(j + GetPerm(k + GetPerm(l)))) % 32;
            int4 g = GetGrad4(gi);
            return t * t * (g.x * x + g.y * y + g.z * z + g.w * w);
        }

        private static int FastFloor(float x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }
    }
}
