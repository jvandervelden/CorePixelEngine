using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using X11;

namespace CorePixelEngine.Linux
{
    public class LinuxPlatform : Platform
    {
        private IntPtr display;
        private int screen;
        private Window root;
        private Window mainWindow;
        private Atom deleteWindowMessage;
        private IntPtr ev;
        private PixelGameEngine pge;
        private Dictionary<long, Key> mapKeys = new Dictionary<long, Key>();

        public RCode ApplicationStartUp() { 
            mapKeys[0x00] = Key.NONE;
			mapKeys[0x61] = Key.A; mapKeys[0x62] = Key.B; mapKeys[0x63] = Key.C; mapKeys[0x64] = Key.D; mapKeys[0x65] = Key.E;
			mapKeys[0x66] = Key.F; mapKeys[0x67] = Key.G; mapKeys[0x68] = Key.H; mapKeys[0x69] = Key.I; mapKeys[0x6A] = Key.J;
			mapKeys[0x6B] = Key.K; mapKeys[0x6C] = Key.L; mapKeys[0x6D] = Key.M; mapKeys[0x6E] = Key.N; mapKeys[0x6F] = Key.O;
			mapKeys[0x70] = Key.P; mapKeys[0x71] = Key.Q; mapKeys[0x72] = Key.R; mapKeys[0x73] = Key.S; mapKeys[0x74] = Key.T;
			mapKeys[0x75] = Key.U; mapKeys[0x76] = Key.V; mapKeys[0x77] = Key.W; mapKeys[0x78] = Key.X; mapKeys[0x79] = Key.Y;
			mapKeys[0x7A] = Key.Z;
	
			mapKeys[(long)Keys.XK_F1] = Key.F1; mapKeys[(long)Keys.XK_F2] = Key.F2; mapKeys[(long)Keys.XK_F3] = Key.F3; mapKeys[(long)Keys.XK_F4] = Key.F4;
			mapKeys[(long)Keys.XK_F5] = Key.F5; mapKeys[(long)Keys.XK_F6] = Key.F6; mapKeys[(long)Keys.XK_F7] = Key.F7; mapKeys[(long)Keys.XK_F8] = Key.F8;
			mapKeys[(long)Keys.XK_F9] = Key.F9; mapKeys[(long)Keys.XK_F10] = Key.F10; mapKeys[(long)Keys.XK_F11] = Key.F11; mapKeys[(long)Keys.XK_F12] = Key.F12;
	
			mapKeys[(long)Keys.XK_Down] = Key.DOWN; mapKeys[(long)Keys.XK_Left] = Key.LEFT; mapKeys[(long)Keys.XK_Right] = Key.RIGHT; mapKeys[(long)Keys.XK_Up] = Key.UP;
			mapKeys[(long)Keys.XK_KP_Enter] = Key.ENTER; mapKeys[(long)Keys.XK_Return] = Key.ENTER;
	
			mapKeys[(long)Keys.XK_BackSpace] = Key.BACK; mapKeys[(long)Keys.XK_Escape] = Key.ESCAPE; mapKeys[(long)Keys.XK_Linefeed] = Key.ENTER;	mapKeys[(long)Keys.XK_Pause] = Key.PAUSE;
			mapKeys[(long)Keys.XK_Scroll_Lock] = Key.SCROLL; mapKeys[(long)Keys.XK_Tab] = Key.TAB; mapKeys[(long)Keys.XK_Delete] = Key.DEL; mapKeys[(long)Keys.XK_Home] = Key.HOME;
			mapKeys[(long)Keys.XK_End] = Key.END; mapKeys[(long)Keys.XK_Page_Up] = Key.PGUP; mapKeys[(long)Keys.XK_Page_Down] = Key.PGDN;	mapKeys[(long)Keys.XK_Insert] = Key.INS;
			mapKeys[(long)Keys.XK_Shift_L] = Key.SHIFT; mapKeys[(long)Keys.XK_Shift_R] = Key.SHIFT; mapKeys[(long)Keys.XK_Control_L] = Key.CTRL; mapKeys[(long)Keys.XK_Control_R] = Key.CTRL;
			mapKeys[(long)Keys.XK_space] = Key.SPACE; mapKeys[(long)Keys.XK_period] = Key.PERIOD;
	
			mapKeys[(long)Keys.XK_0] = Key.K0; mapKeys[(long)Keys.XK_1] = Key.K1; mapKeys[(long)Keys.XK_2] = Key.K2; mapKeys[(long)Keys.XK_3] = Key.K3; mapKeys[(long)Keys.XK_4] = Key.K4;
			mapKeys[(long)Keys.XK_5] = Key.K5; mapKeys[(long)Keys.XK_6] = Key.K6; mapKeys[(long)Keys.XK_7] = Key.K7; mapKeys[(long)Keys.XK_8] = Key.K8; mapKeys[(long)Keys.XK_9] = Key.K9;
	
			mapKeys[(long)Keys.XK_KP_0] = Key.NP0; mapKeys[(long)Keys.XK_KP_1] = Key.NP1; mapKeys[(long)Keys.XK_KP_2] = Key.NP2; mapKeys[(long)Keys.XK_KP_3] = Key.NP3; mapKeys[(long)Keys.XK_KP_4] = Key.NP4;
			mapKeys[(long)Keys.XK_KP_5] = Key.NP5; mapKeys[(long)Keys.XK_KP_6] = Key.NP6; mapKeys[(long)Keys.XK_KP_7] = Key.NP7; mapKeys[(long)Keys.XK_KP_8] = Key.NP8; mapKeys[(long)Keys.XK_KP_9] = Key.NP9;
			mapKeys[(long)Keys.XK_KP_Multiply] = Key.NP_MUL; mapKeys[(long)Keys.XK_KP_Add] = Key.NP_ADD; mapKeys[(long)Keys.XK_KP_Divide] = Key.NP_DIV; mapKeys[(long)Keys.XK_KP_Subtract] = Key.NP_SUB; mapKeys[(long)Keys.XK_KP_Decimal] = Key.NP_DECIMAL;
	
            return RCode.OK; 
        }

        public RCode ApplicationCleanUp() { return RCode.OK; }

        public RCode ThreadStartUp() {
            ev = Marshal.AllocHGlobal(24 * sizeof(long));
            return RCode.OK;
        }

        public RCode ThreadCleanUp() { 
            Marshal.FreeHGlobal(ev);
            Xlib.XDestroyWindow(display, mainWindow);
            Xlib.XCloseDisplay(display);
            return RCode.OK; 
        }

        public RCode CreateGraphics(bool bFullScreen, bool bEnableVSYNC, VectorI2d vViewPos, VectorI2d vViewSize) { 

            Dictionary<string, object> renderParams = new Dictionary<string, object>();
            renderParams["displayPtr"] = display;
            renderParams["windowPtr"] = (IntPtr)mainWindow;

            if (Renderer.GetInstance().CreateDevice(renderParams, bFullScreen, bEnableVSYNC) != RCode.OK) return RCode.FAIL;

            Renderer.GetInstance().UpdateViewport(vViewPos, vViewSize);

            return RCode.OK; 
        }

        public RCode CreateWindowPane(VectorI2d vWindowPos, VectorI2d vWindowSize, bool bFullScreen) {
            display = Xlib.XOpenDisplay(null);
            screen = Xlib.XDefaultScreen(display);
            root = Xlib.XRootWindow(display, screen);
            mainWindow = Xlib.XCreateSimpleWindow(
                display, root, 
                vWindowPos.x, vWindowPos.y, 
                (uint)vWindowSize.x, (uint)vWindowSize.y, 1, 
                Xlib.XBlackPixel(display, screen), Xlib.XWhitePixel(display, screen));

            EventMask eventMask = 
                // Keyboard
                EventMask.KeyPressMask | EventMask.KeyReleaseMask | 
                // Mouse
                EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask |
                // Window
                EventMask.FocusChangeMask | EventMask.EnterWindowMask | EventMask.LeaveWindowMask;
            Xlib.XSelectInput(display, mainWindow, eventMask);
            Xlib.XMapWindow(display, mainWindow);

            // Disable windows re-sizing
            XSizeHints sizeHints = new XSizeHints() {
                flags = XSizeHintsFlags.PMaxSize | XSizeHintsFlags.PMinSize,
                min_width = vWindowSize.x,
                max_width = vWindowSize.x,
			    min_height = vWindowSize.y,
                max_height = vWindowSize.y
            };
            XlibExt.XSetWMNormalHints(display, mainWindow, ref sizeHints);

            // Setup delete message when the user closes the window
            deleteWindowMessage = Xlib.XInternAtom(display, "WM_DELETE_WINDOW", false);
            XlibExt.XSetWMProtocols(display, mainWindow, new Atom[] { deleteWindowMessage }, 1);

            return RCode.OK;
        }

        public RCode SetWindowTitle(string title) { 
            Xlib.XStoreName(display, mainWindow, title);
            return RCode.OK; 
        }

        public RCode StartSystemEventLoop() { return RCode.OK; }

        public RCode HandleSystemEvent() { 
            while(Xlib.XPending(display) > 0)
            { 
                Xlib.XNextEvent(display, ev);
                XAnyEvent xevent = Marshal.PtrToStructure<XAnyEvent>(ev);

                switch (xevent.type)
                {
                    case (int)Event.ClientMessage:
                        XClientMessageEvent xClientMessageEvent = Marshal.PtrToStructure<XClientMessageEvent>(ev);
                        if (xClientMessageEvent.data == (IntPtr)deleteWindowMessage) 
                            PixelGameEngine.Instance.Terminate();
                        break;
                    case (int)Event.KeyPress:
                    case (int)Event.KeyRelease:
                        XKeyEvent xKeyEvent = Marshal.PtrToStructure<XKeyEvent>(ev);
                        KeySym sym = XlibExt.XLookupKeysym(new XKeyEvent[] { xKeyEvent }, 0);
                        pge.Input.UpdateKeyState((int)mapKeys[(long)sym], xevent.type == (int)Event.KeyPress);
                        break;
                    case (int)Event.ButtonPress:
                    case (int)Event.ButtonRelease:
                        XButtonEvent xButtonEvent = Marshal.PtrToStructure<XButtonEvent>(ev);
                        bool pressed = xevent.type == (int)Event.ButtonPress;

                        switch (xButtonEvent.button)
                        {
                            case 1:
                            case 2:
                            case 3:
                                pge.Input.UpdateMouseState((int)xButtonEvent.button - 1, pressed);
                                break;
                            case 4:
                                pge.Input.UpdateMouseWheel(120);
                                break;
                            case 5:
                                pge.Input.UpdateMouseWheel(-120);
                                break;
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                                pge.Input.UpdateMouseState((int)xButtonEvent.button - 3, pressed);
                                break;
                        }
                        break;
                    case (int)Event.MotionNotify:
                        XMotionEvent xMotionEvent = Marshal.PtrToStructure<XMotionEvent>(ev);
                        pge.Input.UpdateMouse(xMotionEvent.x, xMotionEvent.y);
                        break;
                    case (int)Event.EnterNotify:
                        pge.Input.UpdateMouseFocus(true);
                        break;
                    case (int)Event.LeaveNotify:
                        pge.Input.UpdateMouseFocus(false);
                        break;
                    case (int)Event.FocusIn:
                        pge.Input.UpdateKeyFocus(true);
                        break;
                    case (int)Event.FocusOut:
                        pge.Input.UpdateKeyFocus(false);
                        break;
                }
            }
            return RCode.OK; 
        }

        public void SetPixelGameEngine(PixelGameEngine pge) { 
            this.pge = pge; 
        }
    }
}