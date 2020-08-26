using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CorePixelEngine.Windows
{
    public class WinPlatform : Platform
    {
        private MainWindow window = null;
        private PixelGameEngine pge = null;
        private Dictionary<MouseButtons, int> mouseButtonMap = new Dictionary<MouseButtons, int>();
        private Dictionary<Keys, int> keyButtonMap = new Dictionary<Keys, int>();

        public RCode ApplicationCleanUp()
        {
            Renderer.GetInstance().DestroyDevice();
            window.Close();
            window.Dispose();
            return RCode.OK;
        }

        public RCode ApplicationStartUp()
        {
            mouseButtonMap[MouseButtons.Left] = 0;
            mouseButtonMap[MouseButtons.Right] = 1;
            mouseButtonMap[MouseButtons.Middle] = 2;
            mouseButtonMap[MouseButtons.XButton1] = 5;
            mouseButtonMap[MouseButtons.XButton2] = 6;

            keyButtonMap[Keys.None] = (int)Key.NONE;
            keyButtonMap[Keys.A] = (int)Key.A; keyButtonMap[Keys.B] = (int)Key.B; keyButtonMap[Keys.C] = (int)Key.C; keyButtonMap[Keys.D] = (int)Key.D; 
            keyButtonMap[Keys.E] = (int)Key.E; keyButtonMap[Keys.F] = (int)Key.F; keyButtonMap[Keys.G] = (int)Key.G; keyButtonMap[Keys.H] = (int)Key.H; 
            keyButtonMap[Keys.I] = (int)Key.I; keyButtonMap[Keys.J] = (int)Key.J; keyButtonMap[Keys.K] = (int)Key.K; keyButtonMap[Keys.L] = (int)Key.L; 
            keyButtonMap[Keys.M] = (int)Key.M; keyButtonMap[Keys.N] = (int)Key.N; keyButtonMap[Keys.O] = (int)Key.O; keyButtonMap[Keys.P] = (int)Key.P; 
            keyButtonMap[Keys.Q] = (int)Key.Q; keyButtonMap[Keys.R] = (int)Key.R; keyButtonMap[Keys.S] = (int)Key.S; keyButtonMap[Keys.T] = (int)Key.T;
            keyButtonMap[Keys.Y] = (int)Key.U; keyButtonMap[Keys.V] = (int)Key.V; keyButtonMap[Keys.W] = (int)Key.W; keyButtonMap[Keys.X] = (int)Key.X; 
            keyButtonMap[Keys.Y] = (int)Key.Y; keyButtonMap[Keys.Z] = (int)Key.Z;

            keyButtonMap[Keys.F1] = (int)Key.F1; keyButtonMap[Keys.F2] = (int)Key.F2; keyButtonMap[Keys.F3] = (int)Key.F3; keyButtonMap[Keys.F4] = (int)Key.F4;
            keyButtonMap[Keys.F5] = (int)Key.F5; keyButtonMap[Keys.F6] = (int)Key.F6; keyButtonMap[Keys.F7] = (int)Key.F7; keyButtonMap[Keys.F8] = (int)Key.F8;
            keyButtonMap[Keys.F9] = (int)Key.F9; keyButtonMap[Keys.F10] = (int)Key.F10; keyButtonMap[Keys.F11] = (int)Key.F11; keyButtonMap[Keys.F12] = (int)Key.F12;

            keyButtonMap[Keys.Down] = (int)Key.DOWN; keyButtonMap[Keys.Left] = (int)Key.LEFT; keyButtonMap[Keys.Right] = (int)Key.RIGHT; 
            keyButtonMap[Keys.Up] = (int)Key.UP; keyButtonMap[Keys.Enter] = (int)Key.ENTER; keyButtonMap[Keys.Return] = (int)Key.RETURN;

            keyButtonMap[Keys.Back] = (int)Key.BACK; keyButtonMap[Keys.Escape] = (int)Key.ESCAPE; keyButtonMap[Keys.Pause] = (int)Key.PAUSE; 
            keyButtonMap[Keys.Scroll] = (int)Key.SCROLL; keyButtonMap[Keys.Tab] = (int)Key.TAB; keyButtonMap[Keys.Delete] = (int)Key.DEL; 
            keyButtonMap[Keys.Home] = (int)Key.HOME; keyButtonMap[Keys.End] = (int)Key.END; keyButtonMap[Keys.Prior] = (int)Key.PGUP; 
            keyButtonMap[Keys.Next] = (int)Key.PGDN; keyButtonMap[Keys.Insert] = (int)Key.INS; keyButtonMap[Keys.Shift] = (int)Key.SHIFT; 
            keyButtonMap[Keys.ShiftKey] = (int)Key.SHIFT; keyButtonMap[Keys.Control] = (int)Key.CTRL; keyButtonMap[Keys.ControlKey] = (int)Key.CTRL; 
            keyButtonMap[Keys.Space] = (int)Key.SPACE;

            keyButtonMap[Keys.D0] = (int)Key.K0; keyButtonMap[Keys.D1] = (int)Key.K1; keyButtonMap[Keys.D2] = (int)Key.K2; keyButtonMap[Keys.D3] = (int)Key.K3; 
            keyButtonMap[Keys.D4] = (int)Key.K4; keyButtonMap[Keys.D5] = (int)Key.K5; keyButtonMap[Keys.D6] = (int)Key.K6; keyButtonMap[Keys.D7] = (int)Key.K7; 
            keyButtonMap[Keys.D8] = (int)Key.K8; keyButtonMap[Keys.D9] = (int)Key.K9;

            keyButtonMap[Keys.NumPad0] = (int)Key.NP0; keyButtonMap[Keys.NumPad1] = (int)Key.NP1; keyButtonMap[Keys.NumPad2] = (int)Key.NP2; 
            keyButtonMap[Keys.NumPad3] = (int)Key.NP3; keyButtonMap[Keys.NumPad4] = (int)Key.NP4; keyButtonMap[Keys.NumPad5] = (int)Key.NP5; 
            keyButtonMap[Keys.NumPad6] = (int)Key.NP6; keyButtonMap[Keys.NumPad7] = (int)Key.NP7; keyButtonMap[Keys.NumPad8] = (int)Key.NP8; 
            keyButtonMap[Keys.NumPad9] = (int)Key.NP9; keyButtonMap[Keys.Multiply] = (int)Key.NP_MUL; keyButtonMap[Keys.Add] = (int)Key.NP_ADD; 
            keyButtonMap[Keys.Divide] = (int)Key.NP_DIV; keyButtonMap[Keys.Subtract] = (int)Key.NP_SUB; keyButtonMap[Keys.Decimal] = (int)Key.NP_DECIMAL;
            keyButtonMap[Keys.Menu] = (int)Key.TAB;

            return RCode.OK;
        }
        
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

            // Bind event handlers
            window.KeyDown += WindowKeyDown;
            window.KeyUp += WindowKeyUp;
            window.MouseDown += WindowMouseDown;
            window.MouseUp += WindowMouseUp;
            window.MouseEnter += WindowMouseEnter;
            window.MouseLeave += WindowMouseLeave;
            window.MouseMove += WindowMouseMove;
            window.MouseWheel += WindowMouseWheel;
            window.GotFocus += WindowGotFocus;
            window.LostFocus += WindowLostFocus;

            return RCode.OK;
        }

        // Window Focus Handlers
        private void WindowLostFocus(object sender, EventArgs e) => pge.Input.UpdateKeyFocus(false);
        private void WindowGotFocus(object sender, EventArgs e) => pge.Input.UpdateKeyFocus(true);
        private void WindowMouseLeave(object sender, EventArgs e) => pge.Input.UpdateMouseFocus(false);
        private void WindowMouseEnter(object sender, EventArgs e) => pge.Input.UpdateMouseFocus(true);

        // Mouse Handlers
        private void WindowMouseWheel(object sender, MouseEventArgs e) => pge.Input.UpdateMouseWheel(e.Delta);
        private void WindowMouseMove(object sender, MouseEventArgs e) => pge.Input.UpdateMouse(e.X, e.Y);
        private void WindowMouseUp(object sender, MouseEventArgs e) => pge.Input.UpdateMouseState(mouseButtonMap[e.Button], false);
        private void WindowMouseDown(object sender, MouseEventArgs e) => pge.Input.UpdateMouseState(mouseButtonMap[e.Button], true);

        // Keyboard Handlers
        private void WindowKeyUp(object sender, KeyEventArgs e) => pge.Input.UpdateKeyState(keyButtonMap[e.KeyCode], false);
        private void WindowKeyDown(object sender, KeyEventArgs e) => pge.Input.UpdateKeyState(keyButtonMap[e.KeyCode], true);

        public RCode HandleSystemEvent() => RCode.OK;

        public void SetPixelGameEngine(PixelGameEngine pge) { this.pge = pge; }

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

        public RCode LoadFromFile(string sImageFile, ref Pixel[] colData, ref int width, ref int height, ResourcePack pack = null)
        {
            Bitmap img = (Bitmap)Bitmap.FromFile(sImageFile);

            width = img.Width;
            height = img.Height;
            colData = new Pixel[width * height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Color pixelColor = img.GetPixel(x, y);
                    colData[x + width * y] = new Pixel(pixelColor.R, pixelColor.G, pixelColor.B, pixelColor.A);
                }

            return RCode.FAIL;
        }
    }
}
