using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public interface Renderer
    {
        public void PrepareDevice();
        public RCode CreateDevice(IList<object> parameters, bool bFullScreen, bool bVSYNC);
        public RCode DestroyDevice();
        public void DisplayFrame();
        public void PrepareDrawing();
        public void DrawLayerQuad(VectorF2d offset, VectorF2d scale, Pixel tint);
        public void DrawDecalQuad(DecalInstance decal);
        public UInt32 CreateTexture(UInt32 width, UInt32 height);
        public void UpdateTexture(UInt32 id, Sprite spr);
        public UInt32 DeleteTexture(UInt32 id);
        public void ApplyTexture(UInt32 id);
        public void UpdateViewport(VectorI2d pos, VectorI2d size);
        public void ClearBuffer(Pixel p, bool bDepth);
        public void SetPixelGameEngine(PixelGameEngine pge);
        public static Renderer GetInstance() { return PixelGameEngine.Instance.Renderer; }
    }
}
