using System;
using System.IO;

namespace CorePixelEngine
{
    public class Sprite
    {
        public Int32 width = 0;
        public Int32 height = 0;
        public enum Mode { NORMAL, PERIODIC };
        public enum Flip { NONE = 0, HORIZ = 1, VERT = 2 };
        Pixel[] pColData = null;
        Mode modeSample = Mode.NORMAL;

        public Sprite()
        { pColData = null; width = 0; height = 0; }

        public Sprite(string sImageFile, ResourcePack pack)
        { PixelGameEngine.Instance.Platform.LoadFromFile(sImageFile, ref pColData, ref width, ref height, pack); }

        public Sprite(Int32 w, Int32 h) : this(w, h, new Pixel()) {}

        public Sprite(Int32 w, Int32 h, Pixel defaultPixel)
        {
            width = w; height = h;
            pColData = new Pixel[width * height];
            for (Int32 i = 0; i < width * height; i++)
                pColData[i] = defaultPixel;
        }

        public RCode LoadFromPGESprFile(string sImageFile, ResourcePack pack)
        {
            if (pColData != null) pColData = null;
            Func<FileStream, RCode> readData = (ifs) =>
            {
                BinaryReader reader = new BinaryReader(ifs);
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                byte[] pixels = reader.ReadBytes(width * height * sizeof(UInt32));

                if (pixels.Length % sizeof(UInt32) > 0)
                {
                    return RCode.FAIL;
                }

                pColData = new Pixel[width * height];

                for (int i = 0, p = 0; i < pixels.Length; i += sizeof(UInt32), p++)
                {
                    pColData[p] = new Pixel(pixels[i + 0], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
                }

                return RCode.OK;
            };
            
            // These are essentially Memory Surfaces represented by olc::Sprite
            // which load very fast, but are completely uncompressed
            if (pack == null)
            {
                try {
                    using FileStream ifs = File.Open(sImageFile, FileMode.Open);
                    return readData.Invoke(ifs);
                } catch (IOException) {
                    return RCode.FAIL;
                }
            }
            /*else
            {
                ResourceBuffer rb = pack->GetFileBuffer(sImageFile);
                std::istream is(&rb);
                ReadData(is);
                return olc::OK;
            }*/
            return RCode.FAIL;
        }

        public RCode SaveToPGESprFile(string sImageFile)
        {
            if (pColData == null) return RCode.FAIL;

            try {
                using BinaryWriter ofs = new BinaryWriter(File.Open(sImageFile, FileMode.Create));
                ofs.Write(width);
                ofs.Write(height);
                foreach (Pixel p in pColData)
                    ofs.Write(p.GetPacked());
            } catch (IOException) {
                return RCode.FAIL;
            }
        
            return RCode.OK;
        }

        public void SetSampleMode(Mode mode)
        { modeSample = mode; }

        public Pixel GetPixel(Int32 x, Int32 y)
        {
            if (modeSample == Mode.NORMAL)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    return pColData[y*width + x];
                else
                    return new Pixel(0, 0, 0, 0);
            }
            else
            {
                return pColData[Math.Abs(y%height)*width + Math.Abs(x%width)];
            }
        }
        public bool SetPixel(Int32 x, Int32 y, Pixel p)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                pColData[y*width + x] = p;
                return true;
            }
            else
                return false;
        }
        public Pixel GetPixel(VectorI2d a)
        { return GetPixel(a.x, a.y); }
        public bool SetPixel(VectorI2d a, Pixel p)
        { return SetPixel(a.x, a.y, p); }
        public Pixel Sample(float x, float y)
        {
            Int32 sx = Math.Min((Int32)((x * (float)width)), width - 1);
            Int32 sy = Math.Min((Int32)((y * (float)height)), height - 1);
            return GetPixel(sx, sy);
        }
        public Pixel SampleBL(float u, float v)
        {
            u = u * width - 0.5f;
            v = v * height - 0.5f;
            int x = (int)Math.Floor(u); // cast to int rounds toward zero, not downward
            int y = (int)Math.Floor(v); // Thanks @joshinils
            float u_ratio = u - x;
            float v_ratio = v - y;
            float u_opposite = 1 - u_ratio;
            float v_opposite = 1 - v_ratio;

            Pixel p1 = GetPixel(Math.Max(x, 0), Math.Max(y, 0));
            Pixel p2 = GetPixel(Math.Min(x + 1, (int)width - 1), Math.Max(y, 0));
            Pixel p3 = GetPixel(Math.Max(x, 0), Math.Min(y + 1, (int)height - 1));
            Pixel p4 = GetPixel(Math.Min(x + 1, (int)width - 1), Math.Min(y + 1, (int)height - 1));

            return new Pixel(
                (byte)((p1.r * u_opposite + p2.r * u_ratio) * v_opposite + (p3.r * u_opposite + p4.r * u_ratio) * v_ratio),
                (byte)((p1.g * u_opposite + p2.g * u_ratio) * v_opposite + (p3.g * u_opposite + p4.g * u_ratio) * v_ratio),
                (byte)((p1.b * u_opposite + p2.b * u_ratio) * v_opposite + (p3.b * u_opposite + p4.b * u_ratio) * v_ratio));
        }

        public Pixel[] GetData() => pColData;

        public uint[] GetPackedData() {
            uint[] data = new uint[pColData.Length];

            for (int i = 0; i < pColData.Length; i++)
            {
                data[i] = pColData[i].GetPacked();
            }

            return data; 
        }
    };
}