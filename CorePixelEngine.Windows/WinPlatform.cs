using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CorePixelEngine.Windows
{
    public class WinPlatform : Platform
    {
        MainWindow window = null;

        public RCode ApplicationCleanUp()
        {
            Renderer.GetInstance().DestroyDevice();
            window.Close();
            window.Dispose();
            return RCode.OK;
        }

        public RCode ApplicationStartUp() => RCode.OK;
        
        public RCode CreateGraphics(bool bFullScreen, bool bEnableVSYNC, VectorI2d vViewPos, VectorI2d vViewSize)
        {
            Dictionary<string, object> renderParams = new Dictionary<string, object>();
            
            renderParams["windowPtr"] = window.ThreadSafeHandle;

            if (Renderer.GetInstance().CreateDevice(renderParams, bFullScreen, bEnableVSYNC) != RCode.OK) return RCode.FAIL;

            Renderer.GetInstance().UpdateViewport(vViewPos, vViewSize);

            return RCode.OK;
        }

        public RCode CreateWindowPane(VectorI2d vWindowPos, VectorI2d vWindowSize, bool bFullScreen)
        {
            window = new MainWindow(vWindowPos, vWindowSize, bFullScreen);
            return RCode.OK;
        }

        public RCode HandleSystemEvent() => RCode.OK;

        public void SetPixelGameEngine(PixelGameEngine pge) { }

        public RCode SetWindowTitle(string s)
        {
            window.Title = s;
            return RCode.OK;
        }

        public RCode StartSystemEventLoop() 
        {
            Application.Run(window);
            PixelGameEngine.Instance.Terminate();
            return RCode.OK;
        }

        public RCode ThreadCleanUp() => RCode.OK;

        public RCode ThreadStartUp() => RCode.OK;
    }
}
