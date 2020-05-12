using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public class Decal : IDisposable
    {
        public int id = -1;
        public Sprite sprite = null;
        public VectorF2d vUVScale = new VectorF2d(1.0f, 1.0f);

        public Decal(Sprite spr)
        {
            id = -1;
            if (spr == null) return;
            sprite = spr;
            id = (int)Renderer.GetInstance().CreateTexture((uint)sprite.width, (uint)sprite.height);
            Update();
        }

        public void Dispose()
        {
            if (id != -1)
            {
                Renderer.GetInstance().DeleteTexture((uint)id);
                id = -1;
            }
        }

        public void Update()
        {
            if (sprite == null) return;
            vUVScale = new VectorF2d (1.0f / (float)(sprite.width), 1.0f / (float)(sprite.height));
            Renderer.GetInstance().ApplyTexture((uint)id);
            Renderer.GetInstance().UpdateTexture((uint)id, sprite);
        }
    }

    public class DecalInstance
    {
        public Decal decal = null;
        public VectorF2d[] pos = { new VectorF2d(0.0f, 0.0f), new VectorF2d(0.0f, 0.0f), new VectorF2d(0.0f, 0.0f), new VectorF2d(0.0f, 0.0f) };
        public VectorF2d[] uv = { new VectorF2d(0.0f, 0.0f), new VectorF2d(0.0f, 1.0f), new VectorF2d(1.0f, 1.0f), new VectorF2d(1.0f, 0.0f) };
        public float[] w = { 1, 1, 1, 1 };
        public Pixel tint;
    };
}
