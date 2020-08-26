using System;
using System.Collections.Generic;
using System.Numerics;

namespace CorePixelEngine
{
    public class VectorD2d
    {
        public double x => _x;
        public double y => _y;

        protected double _x = 0;
        protected double _y = 0;

        public VectorD2d(double x, double y) { this._x = x; this._y = y; }
        public VectorD2d() : this(0, 0) { }
        public VectorD2d(VectorD2d v) : this(v.x, v.y) { }

        public double mag() { return Math.Sqrt(x * x + y * y); }
        public double mag2() { return x * x + y * y; }
        public VectorD2d norm() { double r = 1 / mag(); return new VectorD2d(x * r, y * r); }
        public VectorD2d perp() => new VectorD2d(-y, x);
        public double dot(VectorD2d rhs) { return this.x * rhs.x + this.y * rhs.y; }
        public double cross(VectorD2d rhs) { return this.x * rhs.y - this.y * rhs.x; }
        public void assign(VectorD2d vect) { this._x = vect.x; this._y = vect.y; }
        public static VectorD2d operator +(VectorD2d lhs, VectorD2d rhs) => new VectorD2d(lhs.x + rhs.x, lhs.y + rhs.y);
        public static VectorD2d operator -(VectorD2d lhs, VectorD2d rhs) => new VectorD2d(lhs.x - rhs.x, lhs.y - rhs.y);
        public static VectorD2d operator *(VectorD2d lhs, double rhs) => new VectorD2d(lhs.x * rhs, lhs.y * rhs);
        public static VectorD2d operator *(double lhs, VectorD2d rhs) => new VectorD2d(lhs * rhs.x, lhs * rhs.y);
        public static VectorD2d operator /(VectorD2d lhs, double rhs) => new VectorD2d(lhs.x / rhs, lhs.y / rhs);
        public static VectorD2d operator /(double lhs, VectorD2d rhs) => new VectorD2d(lhs / rhs.x, lhs / rhs.y);
    };

    public class VectorI2d : VectorD2d 
    {
        public new int x { get => (int)_x; set => _x = value; }
        public new int y { get => (int)_y; set => _y = value; }

        private VectorI2d(double x, double y) : base(x, y) { }
        public VectorI2d(int x, int y) : base(x, y) { }
        public VectorI2d() : this(0, 0) { }
        public VectorI2d(VectorI2d v) : this((int)v.x, (int)v.y) { }

        public static implicit operator VectorI2d(VectorU2d v) => new VectorI2d(v.x, v.y);
        public static implicit operator VectorI2d(VectorF2d v) => new VectorI2d(v.x, v.y);

        public static VectorI2d operator +(VectorI2d lhs, VectorI2d rhs) => new VectorI2d(lhs.x + rhs.x, lhs.y + rhs.y);
        public static VectorI2d operator -(VectorI2d lhs, VectorI2d rhs) => new VectorI2d(lhs.x - rhs.x, lhs.y - rhs.y);
        public static VectorI2d operator *(VectorI2d lhs, VectorI2d rhs) => new VectorI2d(lhs.x * rhs.x, lhs.y * rhs.x);
        public static VectorI2d operator *(VectorI2d lhs, double rhs) => new VectorI2d(lhs.x * rhs, lhs.y * rhs);
        public static VectorI2d operator *(double lhs, VectorI2d rhs) => new VectorI2d(lhs * rhs.x, lhs * rhs.y);
        public static VectorI2d operator /(VectorI2d lhs, double rhs) => new VectorI2d(lhs.x / rhs, lhs.y / rhs);
        public static VectorI2d operator /(double lhs, VectorI2d rhs) => new VectorI2d(lhs / rhs.x, lhs / rhs.y);
        public static VectorI2d operator /(VectorI2d lhs, VectorI2d rhs) => new VectorI2d(lhs.x / rhs.x, lhs.y / rhs.y);
    }

    public class VectorU2d : VectorD2d 
    {
        public new uint x { get => (uint)_x; set => _x = value; }
        public new uint y { get => (uint)_y; set => _y = value; }

        private VectorU2d(double x, double y) : base(x, y) { }
        public VectorU2d(uint x, uint y) : base(x, y) { }
        public VectorU2d() : this(0, 0) { }
        public VectorU2d(VectorU2d v) : this((uint)v.x, (uint)v.y) { }

        public static implicit operator VectorU2d(VectorI2d v) => new VectorU2d(v.x, v.y);
        public static implicit operator VectorU2d(VectorF2d v) => new VectorU2d(v.x, v.y);

        public static VectorU2d operator +(VectorU2d lhs, VectorU2d rhs) => new VectorU2d(lhs.x + rhs.x, lhs.y + rhs.y);
        public static VectorU2d operator -(VectorU2d lhs, VectorU2d rhs) => new VectorU2d(lhs.x - rhs.x, lhs.y - rhs.y);
        public static VectorU2d operator *(VectorU2d lhs, VectorU2d rhs) => new VectorU2d(lhs.x * rhs.x, lhs.y * rhs.x);
        public static VectorU2d operator *(VectorU2d lhs, double rhs) => new VectorU2d(lhs.x * rhs, lhs.y * rhs);
        public static VectorU2d operator *(double lhs, VectorU2d rhs) => new VectorU2d(lhs * rhs.x, lhs * rhs.y);
        public static VectorU2d operator /(VectorU2d lhs, double rhs) => new VectorU2d(lhs.x / rhs, lhs.y / rhs);
        public static VectorU2d operator /(double lhs, VectorU2d rhs) => new VectorU2d(lhs / rhs.x, lhs / rhs.y);
    }

    public class VectorF2d : VectorD2d 
    {
        public new float x { get => (float)_x; set => _x = value; }
        public new float y { get => (float)_y; set => _y = value; }
        private VectorF2d(double x, double y) : base(x, y) { }
        public VectorF2d(float x, float y) : base(x, y) { }
        public VectorF2d() : this(0, 0) { }
        public VectorF2d(VectorF2d v) : this((float)v.x, (float)v.y) { }

        public static implicit operator VectorF2d(VectorU2d v) => new VectorF2d(v.x, v.y);
        public static implicit operator VectorF2d(VectorI2d v) => new VectorF2d(v.x, v.y);

        public static VectorF2d operator +(VectorF2d lhs, VectorF2d rhs) => new VectorF2d(lhs.x + rhs.x, lhs.y + rhs.y);
        public static VectorF2d operator -(VectorF2d lhs, VectorF2d rhs) => new VectorF2d(lhs.x - rhs.x, lhs.y - rhs.y);
        public static VectorF2d operator *(VectorF2d lhs, VectorF2d rhs) => new VectorF2d(lhs.x * rhs.x, lhs.y * rhs.x);
        public static VectorF2d operator *(VectorF2d lhs, double rhs) => new VectorF2d(lhs.x * rhs, lhs.y * rhs);
        public static VectorF2d operator *(double lhs, VectorF2d rhs) => new VectorF2d(lhs * rhs.x, lhs * rhs.y);
        public static VectorF2d operator /(VectorF2d lhs, double rhs) => new VectorF2d(lhs.x / rhs, lhs.y / rhs);
        public static VectorF2d operator /(double lhs, VectorF2d rhs) => new VectorF2d(lhs / rhs.x, lhs / rhs.y);
    }
}
