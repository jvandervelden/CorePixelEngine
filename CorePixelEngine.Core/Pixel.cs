using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public class Pixel
    {
        public const byte nDefaultAlpha = 0xFF;
        public const UInt32 nDefaultPixel = (UInt32)((nDefaultAlpha << 24) & 0xFF000000);
        public static Pixel
            GREY = new Pixel(192, 192, 192),    DARK_GREY = new Pixel(128, 128, 128),    VERY_DARK_GREY = new Pixel(64, 64, 64),
            RED = new Pixel(255, 0, 0),            DARK_RED = new Pixel(128, 0, 0),        VERY_DARK_RED = new Pixel(64, 0, 0),
            YELLOW = new Pixel(255, 255, 0),    DARK_YELLOW = new Pixel(128, 128, 0),    VERY_DARK_YELLOW = new Pixel(64, 64, 0),
            GREEN = new Pixel(0, 255, 0),        DARK_GREEN = new Pixel(0, 128, 0),        VERY_DARK_GREEN = new Pixel(0, 64, 0),
            CYAN = new Pixel(0, 255, 255),        DARK_CYAN = new Pixel(0, 128, 128),        VERY_DARK_CYAN = new Pixel(0, 64, 64),
            BLUE = new Pixel(0, 0, 255),        DARK_BLUE = new Pixel(0, 0, 128),        VERY_DARK_BLUE = new Pixel(0, 0, 64),
            MAGENTA = new Pixel(255, 0, 255),    DARK_MAGENTA = new Pixel(128, 0, 128),    VERY_DARK_MAGENTA = new Pixel(64, 0, 64),
            WHITE = new Pixel(255, 255, 255),    BLACK = new Pixel(0, 0, 0),                BLANK = new Pixel(0, 0, 0, 0);

        public UInt32 n { get => GetPacked(); set => UnPack(value); }
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public enum Mode { NORMAL, MASK, ALPHA, CUSTOM };

        public Pixel() : this(nDefaultPixel) { }

        public Pixel(byte red, byte green, byte blue, byte alpha = nDefaultAlpha)
        { 
            r = red; g = green; b = blue; a = alpha;
        }

        public Pixel(float red, float green, float blue, float alpha = 1.0f) 
            : this((byte)(red * 255.0f), (byte)(green * 255.0f), (byte)(blue * 255.0f), (byte)(alpha * 255.0f)) { }

        public Pixel(UInt32 p) : this(0, 0, 0) => UnPack(p);

        public static bool operator ==(Pixel lhs, Pixel rhs) => Equals(lhs, rhs);

        public static bool operator !=(Pixel lhs, Pixel rhs) => !Equals(lhs, rhs);

        public UInt32 GetPacked() => (UInt32)(r | g << 8 | b << 16 | a << 24);

        public void UnPack(UInt32 value) 
        {
            r = (byte)(value >> 0);
            g = (byte)(value >> 8);
            b = (byte)(value >> 16);
            a = (byte)(value >> 24);
        }

        public override bool Equals(object obj)
        {
            return obj != null
                && obj.GetType() == typeof(Pixel)
                && ((Pixel)obj).GetPacked() == this.GetPacked();
        }

        public override int GetHashCode() => HashCode.Combine(this.GetPacked());
    };
}
