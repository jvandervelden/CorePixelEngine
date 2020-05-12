using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public interface Platform
    {
        public RCode ApplicationStartUp();
        public RCode ApplicationCleanUp();
        public RCode ThreadStartUp();
        public RCode ThreadCleanUp();
        public RCode CreateGraphics(bool bFullScreen, bool bEnableVSYNC, VectorI2d vViewPos, VectorI2d vViewSize);
        public RCode CreateWindowPane(VectorI2d vWindowPos, VectorI2d vWindowSize, bool bFullScreen);
        public RCode SetWindowTitle(string s);
        public RCode StartSystemEventLoop();
        public RCode HandleSystemEvent();
        public void SetPixelGameEngine(PixelGameEngine pge);
    }
}
