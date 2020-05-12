using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public class LayerDesc
    {
        public VectorF2d vOffset = new VectorF2d(0, 0);
        public VectorF2d vScale = new VectorF2d(1, 1);
        public bool bShow = false;
        public bool bUpdate = false;
        public Sprite pDrawTarget = null;
        public UInt32 nResID = 0;
        public IList<DecalInstance> vecDecalInstance = new List<DecalInstance>();
        public Pixel tint = Pixel.WHITE;
        public Action funcHook = null;
    };
}
