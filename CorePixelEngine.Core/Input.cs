using System;
using System.Collections.Generic;
using System.Text;

namespace CorePixelEngine
{
    public class Input
    {
        private const byte nMouseButtons = 7;
        // State of keyboard        
        private bool[] pKeyNewState = new bool[256];
        private bool[] pKeyOldState = new bool[256];
        private HWButton[] pKeyboardState = new HWButton[256];

        // State of mouse
        private bool[] pMouseNewState = new bool[nMouseButtons];
        private bool[] pMouseOldState = new bool[nMouseButtons];
        private HWButton[] pMouseState = new HWButton[nMouseButtons];
        private VectorI2d vMousePosCache = new VectorI2d(0, 0);
        private VectorI2d vMousePos = new VectorI2d(0, 0);
        private Int32 nMouseWheelDeltaCache = 0;
        private Int32 nMouseWheelDelta = 0;
        private bool bHasInputFocus = false;
        private bool bHasMouseFocus = false;

        public Input () {
            for (int i = 0; i<pKeyboardState.Length; i++) pKeyboardState[i] = new HWButton();
            for (int i = 0; i<pMouseState.Length; i++) pMouseState[i] = new HWButton();
        }

        public HWButton GetKey(Key k)
        { return pKeyboardState[(int)k]; }

        public HWButton GetMouse(UInt32 b)
        { return pMouseState[b]; }

        public bool IsFocused()
        { return bHasInputFocus; }

        public Int32 GetMouseX()
        { return vMousePos.x; }

        public Int32 GetMouseY()
        { return vMousePos.y; }

        public Int32 GetMouseWheel()
        { return nMouseWheelDelta; }

        public void UpdateMouseWheel(Int32 delta)
        { nMouseWheelDeltaCache += delta; }

        public void UpdateMouse(Int32 x, Int32 y)
        {
            vMousePosCache.x = x;
            vMousePosCache.y = y;
        }

        public void UpdateMouseState(Int32 button, bool state)
        { pMouseNewState[button] = state; }

        public void UpdateKeyState(Int32 key, bool state)
        { pKeyNewState[key] = state; }

        public void UpdateMouseFocus(bool state)
        { bHasMouseFocus = state; }

        public void UpdateKeyFocus(bool state)
        { bHasInputFocus = state; }

        public void UpdateInput(VectorI2d vViewPos, VectorI2d vWindowSize, VectorI2d vScreenSize)
        {
            Action<HWButton[], bool[], bool[], UInt32> ScanHardware = (pKeys, pStateOld, pStateNew, nKeyCount) =>
            {
                for (UInt32 i = 0; i < nKeyCount; i++)
                {
                    pKeys[i].bPressed = false;
                    pKeys[i].bReleased = false;
                    if (pStateNew[i] != pStateOld[i])
                    {
                        if (pStateNew[i])
                        {
                            pKeys[i].bPressed = !pKeys[i].bHeld;
                            pKeys[i].bHeld = true;
                        }
                        else
                        {
                            pKeys[i].bReleased = true;
                            pKeys[i].bHeld = false;
                        }
                    }
                    pStateOld[i] = pStateNew[i];
                }
            };

            ScanHardware(pKeyboardState, pKeyOldState, pKeyNewState, 256);
            ScanHardware(pMouseState, pMouseOldState, pMouseNewState, nMouseButtons);

            // Mouse coords come in screen space
            // But leave in pixel space

            // Full Screen mode may have a weird viewport we must clamp to
            int x = vMousePosCache.x - vViewPos.x;
            int y = vMousePosCache.y - vViewPos.y;
            vMousePosCache.x = (Int32)(((float)x / (float)(vWindowSize.x - (vViewPos.x * 2)) * (float)vScreenSize.x));
            vMousePosCache.y = (Int32)(((float)y / (float)(vWindowSize.y - (vViewPos.y * 2)) * (float)vScreenSize.y));
            if (vMousePosCache.x >= (Int32)vScreenSize.x) vMousePosCache.x = vScreenSize.x - 1;
            if (vMousePosCache.y >= (Int32)vScreenSize.y) vMousePosCache.y = vScreenSize.y - 1;
            if (vMousePosCache.x < 0) vMousePosCache.x = 0;
            if (vMousePosCache.y < 0) vMousePosCache.y = 0;

            // Cache mouse coordinates so they remain consistent during frame
            vMousePos.assign(vMousePosCache);
            nMouseWheelDelta = nMouseWheelDeltaCache;
            nMouseWheelDeltaCache = 0;
        }
    }

    public enum Key
    {
        NONE,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        K0, K1, K2, K3, K4, K5, K6, K7, K8, K9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        UP, DOWN, LEFT, RIGHT,
        SPACE, TAB, SHIFT, CTRL, INS, DEL, HOME, END, PGUP, PGDN,
        BACK, ESCAPE, RETURN, ENTER, PAUSE, SCROLL,
        NP0, NP1, NP2, NP3, NP4, NP5, NP6, NP7, NP8, NP9,
        NP_MUL, NP_DIV, NP_ADD, NP_SUB, NP_DECIMAL, PERIOD
    };

    public class HWButton
    {
        public bool bPressed = false;    // Set once during the frame the event occurs
        public bool bReleased = false;    // Set once during the frame the event occurs
        public bool bHeld = false;        // Set true for all frames between pressed and released events
    };
}
