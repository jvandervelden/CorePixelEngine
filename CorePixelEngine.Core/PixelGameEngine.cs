using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CorePixelEngine
{
    public abstract class PixelGameEngine
    {
        public static PixelGameEngine Instance { get; private set; } = null;

        protected abstract string sAppName { get; }

        private Sprite pDrawTarget = null;
        private Pixel.Mode nPixelMode = Pixel.Mode.NORMAL;
        private float fBlendFactor = 1.0f;
        private VectorI2d vScreenSize = new VectorI2d(256, 240);
        private VectorF2d vInvScreenSize = new VectorF2d(1.0f / 256.0f, 1.0f / 240.0f);
        private VectorI2d vPixelSize = new VectorI2d(4, 4);
        private VectorI2d vWindowSize = new VectorI2d(0, 0);
        private VectorI2d vViewPos = new VectorI2d(0, 0);
        private VectorI2d vViewSize = new VectorI2d(0, 0);
        private bool bFullScreen = false;
        private VectorF2d vPixel = new VectorF2d(1.0f, 1.0f);
        private bool bEnableVSYNC = false;
        private float fFrameTimer = 1.0f;
        private int nFrameCount = 0;
        private Sprite fontSprite = null;
        private Decal fontDecal = null;
        private IList<LayerDesc> vLayers = new List<LayerDesc>(1);
        private byte nTargetLayer = 0;
        private UInt32 nLastFPS = 0;
        private Func<int, int, Pixel, Pixel, Pixel> funcPixelMode;
        private DateTime m_tp1, m_tp2;
        public Input Input { get; } = new Input();
        public Renderer Renderer { get; private set; }
        public Platform Platform { get; private set; }

        // If anything sets this flag to false, the engine
        // "should" shut down gracefully
        private static object bAtomActiveLock = new object();
        private static bool m_AtomActive = false;
        public static bool bAtomActive
        { 
            get { return m_AtomActive; }
            set { lock (bAtomActiveLock) { m_AtomActive = value; } }
        }
        
        public PixelGameEngine()
        {
            if (PixelGameEngine.Instance != null)
                throw new InvalidOperationException("Cannot instantiate more than 1 instance of the PixelGameEngine.");

            PGEX.pge = this;
            Instance = this;
            ConfigureExtensions();

            Platform.SetPixelGameEngine(this);
            Renderer.SetPixelGameEngine(this);
        }

        private void ConfigureExtensions()
        {
            List<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            string[] loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();
            string[] referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            List<string> toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();

            toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

            loadedAssemblies.ForEach(assembly =>
            {
                foreach (TypeInfo typeInfo in assembly.DefinedTypes)
                {
                    if (!typeInfo.IsInterface)
                    {
                        if (typeof(Renderer).IsAssignableFrom(typeInfo))
                        {
                            if (Renderer != null)
                                throw new InvalidOperationException("Found multiple renderers in the loaded assemblies");

                            Renderer = (Renderer)Activator.CreateInstance(typeInfo.AsType());
                        }
                        if (typeof(Platform).IsAssignableFrom(typeInfo))
                        {
                            if (Platform != null)
                                throw new InvalidOperationException("Found multiple platforms in the loaded assemblies");

                            Platform = (Platform)Activator.CreateInstance(typeInfo.AsType());
                        }
                    }
                }
            });

            if (Renderer == null) throw new InvalidOperationException("No renderer found in the loaded assemblies");
            if (Platform == null) throw new InvalidOperationException("No renderer found in the loaded assemblies");
        }

        public RCode Construct(Int32 screen_w, Int32 screen_h, Int32 pixel_w, Int32 pixel_h, bool full_screen, bool vsync)
        {
            vScreenSize = new VectorI2d(screen_w, screen_h);
            vInvScreenSize = new VectorF2d(1.0f / (float)screen_w, 1.0f / (float)screen_h);
            vPixelSize = new VectorI2d(pixel_w, pixel_h);
            vWindowSize = vScreenSize * vPixelSize;
            bFullScreen = full_screen;
            bEnableVSYNC = vsync;
            vPixel = 2.0f / vScreenSize;

            if (vPixelSize.x <= 0 || vPixelSize.y <= 0 || vScreenSize.x <= 0 || vScreenSize.y <= 0)
                return RCode.FAIL;


            return RCode.OK;
        }

        public void SetScreenSize(int w, int h)
        {
            vScreenSize = new VectorI2d(w, h);
            foreach(LayerDesc layer in vLayers)
            {
                layer.pDrawTarget = null; // Erase existing layer sprites
                layer.pDrawTarget = new Sprite(vScreenSize.x, vScreenSize.y);
                layer.bUpdate = true;
            }
            
            SetDrawTarget(null);

            Renderer.ClearBuffer(Pixel.BLACK, true);
            Renderer.DisplayFrame();
            Renderer.ClearBuffer(Pixel.BLACK, true);
            Renderer.UpdateViewport(vViewPos, vViewSize);
        }

        public virtual RCode Start()
        {
            if (Platform.ApplicationStartUp() != RCode.OK) return RCode.FAIL;

            // Construct the window
            if (Platform.CreateWindowPane(new VectorI2d(30, 30), vWindowSize, bFullScreen) != RCode.OK) return RCode.FAIL;
            UpdateWindowSize(vWindowSize.x, vWindowSize.y);

            // Start the thread
            bAtomActive = true;
            Thread t = new Thread(this.EngineThread);
            t.Start(this);

            Platform.StartSystemEventLoop();

            // Wait for thread to be exited
            t.Join();

            if (Platform.ApplicationCleanUp() != RCode.OK) return RCode.FAIL;

            return RCode.OK;
        }

        public void SetDrawTarget(Sprite target)
        {
            if (target != null)
            {
                pDrawTarget = target;
            }
            else
            {
                nTargetLayer = 0;
                pDrawTarget = vLayers[0].pDrawTarget;
            }
        }

        public void SetDrawTarget(byte layer)
        {
            if (layer < vLayers.Count)
            {
                pDrawTarget = vLayers[layer].pDrawTarget;
                vLayers[layer].bUpdate = true;
                nTargetLayer = layer;
            }
        }

        public void EnableLayer(byte layer, bool b)
        { if (layer < vLayers.Count) vLayers[layer].bShow = b; }

        public void SetLayerOffset(byte layer, VectorF2d offset)
        { SetLayerOffset(layer, offset.x, offset.y); }

        public void SetLayerOffset(byte layer, float x, float y)
        { if (layer < vLayers.Count) vLayers[layer].vOffset = new VectorF2d(x, y); }

        public void SetLayerScale(byte layer, VectorF2d scale)
        { SetLayerScale(layer, scale.x, scale.y); }

        public void SetLayerScale(byte layer, float x, float y)
        { if (layer < vLayers.Count) vLayers[layer].vScale = new VectorF2d(x, y); }

        public void SetLayerTint(byte layer, Pixel tint)
        { if (layer < vLayers.Count) vLayers[layer].tint = tint; }

        public void SetLayerCustomRenderFunction(byte layer, Action f)
        { if (layer<vLayers.Count) vLayers[layer].funcHook = f; }

        public IList<LayerDesc> GetLayers()
        { return vLayers; }

        public UInt32 CreateLayer()
        {
            LayerDesc ld = new LayerDesc();
            ld.pDrawTarget = new Sprite(vScreenSize.x, vScreenSize.y);
            ld.nResID = Renderer.CreateTexture((uint)vScreenSize.x, (uint)vScreenSize.y);
            Renderer.UpdateTexture(ld.nResID, ld.pDrawTarget);
            vLayers.Add(ld);
            return (UInt32)vLayers.Count - 1;
        }

        public Sprite GetDrawTarget()
        { return pDrawTarget; }

        public Int32 GetDrawTargetWidth()
        {
            if (pDrawTarget != null)
                return pDrawTarget.width;
            else
                return 0;
        }

        public Int32 GetDrawTargetHeight()
        {
            if (pDrawTarget != null)
                return pDrawTarget.height;
            else
                return 0;
        }

        public UInt32 GetFPS()
        { return nLastFPS; }

        public Int32 ScreenWidth()
        { return vScreenSize.x; }

        public Int32 ScreenHeight()
        { return vScreenSize.y; }

        public bool Draw(VectorI2d pos, Pixel p)
        { return Draw(pos.x, pos.y, p); }

        public bool Draw(Int32 x, Int32 y, Pixel p)
        { return Draw(pDrawTarget, x, y, p); }

        // This is it, the critical function that plots a pixel
        public bool Draw(Sprite pDrawTarget, Int32 x, Int32 y, Pixel p)
        {
            if (pDrawTarget == null) return false;

            if (nPixelMode == Pixel.Mode.NORMAL)
            {
                return pDrawTarget.SetPixel(x, y, p);
            }

            if (nPixelMode == Pixel.Mode.MASK)
            {
                if (p.a == 255)
                    return pDrawTarget.SetPixel(x, y, p);
            }

            if (nPixelMode == Pixel.Mode.ALPHA)
            {
                Pixel d = pDrawTarget.GetPixel(x, y);
                float a = (float)(p.a / 255.0f) * fBlendFactor;
                float c = 1.0f - a;
                float r = a * (float)p.r + c * (float)d.r;
                float g = a * (float)p.g + c * (float)d.g;
                float b = a * (float)p.b + c * (float)d.b;
                return pDrawTarget.SetPixel(x, y, new Pixel((byte)r, (byte)g, (byte)b/*, (byte)(p.a * fBlendFactor)*/));
            }

            if (nPixelMode == Pixel.Mode.CUSTOM)
            {
                return pDrawTarget.SetPixel(x, y, funcPixelMode(x, y, p, pDrawTarget.GetPixel(x, y)));
            }

            return false;
        }

        public void SetSubPixelOffset(float ox, float oy)
        {
            //vSubPixelOffset.x = ox * vPixel.x;
            //vSubPixelOffset.y = oy * vPixel.y;
        }

        public void DrawLine(VectorI2d pos1, VectorI2d pos2, Pixel p, UInt32 pattern)
        { DrawLine(pos1.x, pos1.y, pos2.x, pos2.y, p, pattern); }

        private void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public void DrawLine(Int32 x1, Int32 y1, Int32 x2, Int32 y2, Pixel p, UInt32 pattern = 0xFFFFFFFF)
        {
            int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
            dx = x2 - x1; dy = y2 - y1;

            Func<bool> rol = () => { pattern = (pattern << 1) | (pattern >> 31); return (pattern & 1) > 0; };

            // straight lines idea by gurkanctn
            if (dx == 0) // Line is vertical
            {
                if (y2 < y1)  Swap(ref y1, ref y2);
                for (y = y1; y <= y2; y++) if (rol()) Draw(x1, y, p);
                return;
            }

            if (dy == 0) // Line is horizontal
            {
                if (x2 < x1) Swap(ref x1, ref x2);
                for (x = x1; x <= x2; x++) if (rol()) Draw(x, y1, p);
                return;
            }

            // Line is Funk-aye
            dx1 = Math.Abs(dx); dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
            if (dy1 <= dx1)
            {
                if (dx >= 0)
                { x = x1; y = y1; xe = x2; }
                else
                { x = x2; y = y2; xe = x1; }

                if (rol()) Draw(x, y, p);

                for (i = 0; x < xe; i++)
                {
                    x = x + 1;
                    if (px < 0)
                        px = px + 2 * dy1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) y = y + 1; else y = y - 1;
                        px = px + 2 * (dy1 - dx1);
                    }
                    if (rol()) Draw(x, y, p);
                }
            }
            else
            {
                if (dy >= 0)
                { x = x1; y = y1; ye = y2; }
                else
                { x = x2; y = y2; ye = y1; }

                if (rol()) Draw(x, y, p);

                for (i = 0; y < ye; i++)
                {
                    y = y + 1;
                    if (py <= 0)
                        py = py + 2 * dx1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) x = x + 1; else x = x - 1;
                        py = py + 2 * (dx1 - dy1);
                    }
                    if (rol()) Draw(x, y, p);
                }
            }
        }

        public void DrawCircle(VectorI2d pos, Int32 radius, Pixel p, byte mask)
        { DrawCircle(pos.x, pos.y, radius, p, mask); }

        public void DrawCircle(Int32 x, Int32 y, Int32 radius, Pixel p, byte mask)
        {
            int x0 = 0;
            int y0 = radius;
            int d = 3 - 2 * radius;
            if (radius == 0) return;

            while (y0 >= x0) // only formulate 1/8 of circle
            {
                if ((mask & 0x01) > 0) Draw(x + x0, y - y0, p);
                if ((mask & 0x02) > 0) Draw(x + y0, y - x0, p);
                if ((mask & 0x04) > 0) Draw(x + y0, y + x0, p);
                if ((mask & 0x08) > 0) Draw(x + x0, y + y0, p);
                if ((mask & 0x10) > 0) Draw(x - x0, y + y0, p);
                if ((mask & 0x20) > 0) Draw(x - y0, y + x0, p);
                if ((mask & 0x40) > 0) Draw(x - y0, y - x0, p);
                if ((mask & 0x80) > 0) Draw(x - x0, y - y0, p);
                if (d < 0) d += 4 * x0++ + 6;
                else d += 4 * (x0++ - y0--) + 10;
            }
        }

        public void FillCircle(VectorI2d pos, Int32 radius, Pixel p)
        { FillCircle(pos.x, pos.y, radius, p); }

        public void FillCircle(Int32 x, Int32 y, Int32 radius, Pixel p)
        {
            // Taken from wikipedia
            int x0 = 0;
            int y0 = radius;
            int d = 3 - 2 * radius;
            if (radius == 0) return;

            Action<int, int, int> drawline = (sx, ex, ny) => 
            {
                for (int i = sx; i <= ex; i++)
                    Draw(i, ny, p);
            };

            while (y0 >= x0)
            {
                // Modified to draw scan-lines instead of edges
                drawline(x - x0, x + x0, y - y0);
                drawline(x - y0, x + y0, y - x0);
                drawline(x - x0, x + x0, y + y0);
                drawline(x - y0, x + y0, y + x0);
                if (d < 0) d += 4 * x0++ + 6;
                else d += 4 * (x0++ - y0--) + 10;
            }
        }

        public void DrawRect(VectorI2d pos, VectorI2d size, Pixel p)
        { DrawRect(pos.x, pos.y, size.x, size.y, p); }

        public void DrawRect(Int32 x, Int32 y, Int32 w, Int32 h, Pixel p)
        {
            DrawLine(x, y, x + w, y, p);
            DrawLine(x + w, y, x + w, y + h, p);
            DrawLine(x + w, y + h, x, y + h, p);
            DrawLine(x, y + h, x, y, p);
        }

        public void Clear(Pixel p)
        {
            int pixels = GetDrawTargetWidth() * GetDrawTargetHeight();
            Pixel[] m = GetDrawTarget().GetData();
            for (int i = 0; i < pixels; i++) m[i] = p;
        }

        public void ClearBuffer(Pixel p, bool bDepth)
        {
            Renderer.ClearBuffer(p, bDepth);
        }

        public void FillRect(VectorI2d pos, VectorI2d size, Pixel p)
        { FillRect(pos.x, pos.y, size.x, size.y, p); }

        public void FillRect(Int32 x, Int32 y, Int32 w, Int32 h, Pixel p)
        {
            Int32 x2 = x + w;
            Int32 y2 = y + h;

            if (x < 0) x = 0;
            if (x >= (Int32)GetDrawTargetWidth()) x = (Int32)GetDrawTargetWidth();
            if (y < 0) y = 0;
            if (y >= (Int32)GetDrawTargetHeight()) y = (Int32)GetDrawTargetHeight();

            if (x2 < 0) x2 = 0;
            if (x2 >= (Int32)GetDrawTargetWidth()) x2 = (Int32)GetDrawTargetWidth();
            if (y2 < 0) y2 = 0;
            if (y2 >= (Int32)GetDrawTargetHeight()) y2 = (Int32)GetDrawTargetHeight();

            for (int i = x; i < x2; i++)
                for (int j = y; j < y2; j++)
                    Draw(i, j, p);
        }

        public void DrawTriangle(VectorI2d pos1, VectorI2d pos2, VectorI2d pos3, Pixel p)
        { DrawTriangle(pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, p); }

        public void DrawTriangle(Int32 x1, Int32 y1, Int32 x2, Int32 y2, Int32 x3, Int32 y3, Pixel p)
        {
            DrawLine(x1, y1, x2, y2, p);
            DrawLine(x2, y2, x3, y3, p);
            DrawLine(x3, y3, x1, y1, p);
        }

        public void FillTriangle(VectorI2d pos1, VectorI2d pos2, VectorI2d pos3, Pixel p)
        { FillTriangle(pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, p); }

        // https://www.avrfreaks.net/sites/default/files/triangles.c
        public void FillTriangle(Int32 x1, Int32 y1, Int32 x2, Int32 y2, Int32 x3, Int32 y3, Pixel p)
        {
            Action<int, int, int> drawline = (sx, ex, ny) => { for (int i = sx; i <= ex; i++) Draw(i, ny, p); };

            int t1x, t2x, y, minx, maxx, t1xp, t2xp;
            bool changed1 = false;
            bool changed2 = false;
            int signx1, signx2, dx1, dy1, dx2, dy2;
            int e1, e2;
            // Sort vertices
            if (y1 > y2) { Swap(ref y1, ref y2); Swap(ref x1, ref x2); }
            if (y1 > y3) { Swap(ref y1, ref y3); Swap(ref x1, ref x3); }
            if (y2 > y3) { Swap(ref y2, ref y3); Swap(ref x2, ref x3); }

            t1x = t2x = x1; y = y1;   // Starting points
            dx1 = (int)(x2 - x1);
            if (dx1 < 0) { dx1 = -dx1; signx1 = -1; } else signx1 = 1;
            dy1 = (int)(y2 - y1);

            dx2 = (int)(x3 - x1);
            if (dx2 < 0) { dx2 = -dx2; signx2 = -1; } else signx2 = 1;
            dy2 = (int)(y3 - y1);

            if (dy1 > dx1) { Swap(ref dx1, ref dy1); changed1 = true; }
            if (dy2 > dx2) { Swap(ref dy2, ref dx2); changed2 = true; }

            e2 = (int)(dx2 >> 1);
            // Flat top, just process the second half
            if (y1 == y2) goto next;
            e1 = (int)(dx1 >> 1);

            for (int i = 0; i < dx1;)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x) { minx = t1x; maxx = t2x; }
                else { minx = t2x; maxx = t1x; }
                // process first line until y value is about to change
                while (i < dx1)
                {
                    i++;
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1) t1xp = signx1;//t1x += signx1;
                        else goto next1;
                    }
                    if (changed1) break;
                    else t1x += signx1;
                }
            // Move line
            next1:
                // process second line until y value is about to change
                while (true)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2) t2xp = signx2;//t2x += signx2;
                        else goto next2;
                    }
                    if (changed2) break;
                    else t2x += signx2;
                }
            next2:
                if (minx > t1x) minx = t1x;
                if (minx > t2x) minx = t2x;
                if (maxx < t1x) maxx = t1x;
                if (maxx < t2x) maxx = t2x;
                drawline(minx, maxx, y);    // Draw line from min to max points found on the y
                                            // Now increase y
                if (!changed1) t1x += signx1;
                t1x += t1xp;
                if (!changed2) t2x += signx2;
                t2x += t2xp;
                y += 1;
                if (y == y2) break;

            }
        next:
            // Second half
            dx1 = (int)(x3 - x2); if (dx1 < 0) { dx1 = -dx1; signx1 = -1; }
            else signx1 = 1;
            dy1 = (int)(y3 - y2);
            t1x = x2;

            if (dy1 > dx1)
            {   // swap values
                Swap(ref dy1, ref dx1);
                changed1 = true;
            }
            else changed1 = false;

            e1 = (int)(dx1 >> 1);

            for (int i = 0; i <= dx1; i++)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x) { minx = t1x; maxx = t2x; }
                else { minx = t2x; maxx = t1x; }
                // process first line until y value is about to change
                while (i < dx1)
                {
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1) { t1xp = signx1; break; }//t1x += signx1;
                        else goto next3;
                    }
                    if (changed1) break;
                    else t1x += signx1;
                    if (i < dx1) i++;
                }
            next3:
                // process second line until y value is about to change
                while (t2x != x3)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2) t2xp = signx2;
                        else goto next4;
                    }
                    if (changed2) break;
                    else t2x += signx2;
                }
            next4:

                if (minx > t1x) minx = t1x;
                if (minx > t2x) minx = t2x;
                if (maxx < t1x) maxx = t1x;
                if (maxx < t2x) maxx = t2x;
                drawline(minx, maxx, y);
                if (!changed1) t1x += signx1;
                t1x += t1xp;
                if (!changed2) t2x += signx2;
                t2x += t2xp;
                y += 1;
                if (y > y3) return;
            }
        }

        public void DrawSprite(VectorI2d pos, Sprite sprite, UInt32 scale, byte flip)
        { DrawSprite(pos.x, pos.y, sprite, scale, flip); }

        public void DrawSprite(Int32 x, Int32 y, Sprite sprite, UInt32 scale, byte flip)
        {
            if (sprite == null)
                return;

            Int32 fxs = 0, fxm = 1, fx = 0;
            Int32 fys = 0, fym = 1, fy = 0;
            if (flip == (byte)Sprite.Flip.HORIZ) { fxs = sprite.width - 1; fxm = -1; }
            if (flip == (byte)Sprite.Flip.VERT) { fys = sprite.height - 1; fym = -1; }

            if (scale > 1)
            {
                fx = fxs;
                for (Int32 i = 0; i < sprite.width; i++, fx += fxm)
                {
                    fy = fys;
                    for (Int32 j = 0; j < sprite.height; j++, fy += fym)
                        for (UInt32 _is = 0; _is < scale; _is++)
                            for (UInt32 js = 0; js < scale; js++)
                                Draw((int)(x + (i * scale) + _is), (int)(y + (j * scale) + js), sprite.GetPixel(fx, fy));
                }
            }
            else
            {
                fx = fxs;
                for (Int32 i = 0; i < sprite.width; i++, fx += fxm)
                {
                    fy = fys;
                    for (Int32 j = 0; j < sprite.height; j++, fy += fym)
                        Draw(x + i, y + j, sprite.GetPixel(fx, fy));
                }
            }
        }

        public void DrawPartialSprite(VectorI2d pos, Sprite sprite, VectorI2d sourcepos, VectorI2d size, UInt32 scale, Sprite.Flip flip)
        { DrawPartialSprite(pos.x, pos.y, sprite, sourcepos.x, sourcepos.y, size.x, size.y, scale, flip); }

        public void DrawPartialSprite(Int32 x, Int32 y, Sprite sprite, Int32 ox, Int32 oy, Int32 w, Int32 h, UInt32 scale, Sprite.Flip flip)
        {
            if (sprite == null)
                return;

            Int32 fxs = 0, fxm = 1, fx = 0;
            Int32 fys = 0, fym = 1, fy = 0;
            if (((byte)flip & (byte)Sprite.Flip.HORIZ) > 0) { fxs = w - 1; fxm = -1; }
            if (((byte)flip & (byte)Sprite.Flip.VERT) > 0) { fys = h - 1; fym = -1; }

            if (scale > 1)
            {
                fx = fxs;
                for (Int32 i = 0; i < w; i++, fx += fxm)
                {
                    fy = fys;
                    for (Int32 j = 0; j < h; j++, fy += fym)
                        for (UInt32 _is = 0; _is < scale; _is++)
                            for (UInt32 js = 0; js < scale; js++)
                                Draw((int)(x + (i * scale) + _is), (int)(y + (j * scale) + js), sprite.GetPixel(fx + ox, fy + oy));
                }
            }
            else
            {
                fx = fxs;
                for (Int32 i = 0; i < w; i++, fx += fxm)
                {
                    fy = fys;
                    for (Int32 j = 0; j < h; j++, fy += fym)
                        Draw(x + i, y + j, sprite.GetPixel(fx + ox, fy + oy));
                }
            }
        }

        public void DrawPartialDecal(VectorF2d pos, Decal decal, VectorF2d source_pos, VectorF2d source_size, VectorF2d scale, Pixel tint)
        {
            VectorF2d vScreenSpacePos = new VectorF2d(
                (pos.x * vInvScreenSize.x) * 2.0f - 1.0f,
                ((pos.y * vInvScreenSize.y) * 2.0f - 1.0f) * -1.0f
            );

            VectorF2d vScreenSpaceDim = new VectorF2d(
                vScreenSpacePos.x + (2.0f * source_size.x * vInvScreenSize.x) * scale.x,
                vScreenSpacePos.y - (2.0f * source_size.y * vInvScreenSize.y) * scale.y
            );

            DecalInstance di = new DecalInstance();
            di.decal = decal; di.tint = tint;

            di.pos[0] = new VectorF2d(vScreenSpacePos.x, vScreenSpacePos.y);
            di.pos[1] = new VectorF2d(vScreenSpacePos.x, vScreenSpaceDim.y);
            di.pos[2] = new VectorF2d(vScreenSpaceDim.x, vScreenSpaceDim.y);
            di.pos[3] = new VectorF2d(vScreenSpaceDim.x, vScreenSpacePos.y);

            VectorF2d uvtl = new VectorF2d(source_pos.x * decal.vUVScale.x, source_pos.y * decal.vUVScale.y);
            VectorF2d uvbr = new VectorF2d(uvtl.x + (source_size.x * decal.vUVScale.x), uvtl.y + (source_size.y * decal.vUVScale.y));
            di.uv[0] = new VectorF2d(uvtl.x, uvtl.y); di.uv[1] = new VectorF2d(uvtl.x, uvbr.y);
            di.uv[2] = new VectorF2d(uvbr.x, uvbr.y); di.uv[3] = new VectorF2d(uvbr.x, uvtl.y);
            vLayers[nTargetLayer].vecDecalInstance.Add(di);
        }

        public void DrawDecal(VectorF2d pos, Decal decal, VectorF2d scale, Pixel tint)
        {
            VectorF2d vScreenSpacePos = new VectorF2d(
                (pos.x * vInvScreenSize.x) * 2.0f - 1.0f,
                ((pos.y * vInvScreenSize.y) * 2.0f - 1.0f) * -1.0f
            );

            VectorF2d vScreenSpaceDim = new VectorF2d(
                vScreenSpacePos.x + (2.0f * ((float)(decal.sprite.width) * vInvScreenSize.x)) * scale.x,
                vScreenSpacePos.y - (2.0f * ((float)(decal.sprite.height) * vInvScreenSize.y)) * scale.y
            );

            DecalInstance di = new DecalInstance();
            di.decal = decal;
            di.tint = tint;
            di.pos[0] = new VectorF2d(vScreenSpacePos.x, vScreenSpacePos.y);
            di.pos[1] = new VectorF2d(vScreenSpacePos.x, vScreenSpaceDim.y);
            di.pos[2] = new VectorF2d(vScreenSpaceDim.x, vScreenSpaceDim.y);
            di.pos[3] = new VectorF2d(vScreenSpaceDim.x, vScreenSpacePos.y);
            vLayers[nTargetLayer].vecDecalInstance.Add(di);
        }

        public void DrawRotatedDecal(VectorF2d pos, Decal decal, float fAngle, VectorF2d center, VectorF2d scale, Pixel tint)
        {
            DecalInstance di = new DecalInstance();
            di.decal = decal;
            di.tint = tint;
            di.pos[0] = (new VectorF2d(0.0f, 0.0f) - center) * scale;
            di.pos[1] = (new VectorF2d(0.0f, decal.sprite.height) - center) * scale;
            di.pos[2] = (new VectorF2d(decal.sprite.width, decal.sprite.height) - center) * scale;
            di.pos[3] = (new VectorF2d(decal.sprite.width, 0.0f) - center) * scale;
            float c = (float)Math.Sin(fAngle), s = (float)Math.Cos(fAngle);
            for (int i = 0; i < 4; i++)
            {
                di.pos[i] = pos + new VectorF2d(di.pos[i].x * c - di.pos[i].y * s, di.pos[i].x * s + di.pos[i].y * c);
                di.pos[i].x = di.pos[i].x * vInvScreenSize.x * 2.0f - 1.0f;
                di.pos[i].y = (di.pos[i].y * vInvScreenSize.y * 2.0f - 1.0f) * -1.0f;
            }
            vLayers[nTargetLayer].vecDecalInstance.Add(di);
        }

        public void DrawPartialRotatedDecal(VectorF2d pos, Decal decal, float fAngle, VectorF2d center, VectorF2d source_pos, VectorF2d source_size, VectorF2d scale, Pixel tint)
        {
            DecalInstance di = new DecalInstance();
            di.decal = decal;
            di.tint = tint;
            di.pos[0] = (new VectorF2d(0.0f, 0.0f) - center) * scale;
            di.pos[1] = (new VectorF2d(0.0f, source_size.y) - center) * scale;
            di.pos[2] = (new VectorF2d(source_size.x, source_size.y) - center) * scale;
            di.pos[3] = (new VectorF2d(source_size.x, 0.0f) - center) * scale;
            float c = (float)Math.Cos(fAngle), s = (float)Math.Sin(fAngle);
            for (int i = 0; i < 4; i++)
            {
                di.pos[i] = pos + new VectorF2d(di.pos[i].x * c - di.pos[i].y * s, di.pos[i].x * s + di.pos[i].y * c);
                di.pos[i].x = di.pos[i].x * vInvScreenSize.x * 2.0f - 1.0f;
                di.pos[i].y = (di.pos[i].y * vInvScreenSize.y * 2.0f - 1.0f) * -1.0f;
            }

            VectorF2d uvtl = source_pos * decal.vUVScale;
            VectorF2d uvbr = uvtl + (source_size * decal.vUVScale);
            di.uv[0] = new VectorF2d(uvtl.x, uvtl.y); di.uv[1] = new VectorF2d(uvtl.x, uvbr.y);
            di.uv[2] = new VectorF2d(uvbr.x, uvbr.y); di.uv[3] = new VectorF2d(uvbr.x, uvtl.y);

            vLayers[nTargetLayer].vecDecalInstance.Add(di);
        }

        public void DrawPartialWarpedDecal(Decal decal, VectorF2d[] pos, VectorF2d source_pos, VectorF2d source_size, Pixel tint)
        {
            DecalInstance di = new DecalInstance();
            di.decal = decal;
            di.tint = tint;
            VectorF2d center = new VectorF2d(0.0f, 0.0f);

            float rd = ((pos[2].x - pos[0].x) * (pos[3].y - pos[1].y) - (pos[3].x - pos[1].x) * (pos[2].y - pos[0].y));
            if (rd != 0)
            {
                VectorF2d uvtl = source_pos * decal.vUVScale;
                VectorF2d uvbr = uvtl + (source_size * decal.vUVScale);
                di.uv[0] = new VectorF2d(uvtl.x, uvtl.y); di.uv[1] = new VectorF2d(uvtl.x, uvbr.y);
                di.uv[2] = new VectorF2d(uvbr.x, uvbr.y); di.uv[3] = new VectorF2d(uvbr.x, uvtl.y);

                rd = 1.0f / rd;
                float rn = ((pos[3].x - pos[1].x) * (pos[0].y - pos[1].y) - (pos[3].y - pos[1].y) * (pos[0].x - pos[1].x)) * rd;
                float sn = ((pos[2].x - pos[0].x) * (pos[0].y - pos[1].y) - (pos[2].y - pos[0].y) * (pos[0].x - pos[1].x)) * rd;
                if (!(rn < 0.0f || rn > 1.0f || sn < 0.0f || sn > 1.0f)) center = pos[0] + rn * (pos[2] - pos[0]);
                float[] d = new float[4]; for (int i = 0; i < 4; i++) d[i] = (float)(pos[i] - center).mag();
                for (int i = 0; i < 4; i++)
                {
                    float q = d[i] == 0.0f ? 1.0f : (d[i] + d[(i + 2) & 3]) / d[(i + 2) & 3];
                    di.uv[i] *= q; di.w[i] *= q;
                    di.pos[i] = new VectorF2d((pos[i].x * vInvScreenSize.x) * 2.0f - 1.0f, ((pos[i].y * vInvScreenSize.y) * 2.0f - 1.0f) * -1.0f);
                }
                vLayers[nTargetLayer].vecDecalInstance.Add(di);
            }
        }

        public void DrawWarpedDecal(Decal decal, VectorF2d[] pos, Pixel tint)
        {
            // Thanks Nathan Reed, a brilliant article explaining whats going on here
            // http://www.reedbeta.com/blog/quadrilateral-interpolation-part-1/
            DecalInstance di = new DecalInstance();
            di.decal = decal;
            di.tint = tint;
            VectorF2d center = new VectorF2d(0.0f, 0.0f);
            float rd = ((pos[2].x - pos[0].x) * (pos[3].y - pos[1].y) - (pos[3].x - pos[1].x) * (pos[2].y - pos[0].y));
            if (rd != 0)
            {
                rd = 1.0f / rd;
                float rn = ((pos[3].x - pos[1].x) * (pos[0].y - pos[1].y) - (pos[3].y - pos[1].y) * (pos[0].x - pos[1].x)) * rd;
                float sn = ((pos[2].x - pos[0].x) * (pos[0].y - pos[1].y) - (pos[2].y - pos[0].y) * (pos[0].x - pos[1].x)) * rd;
                if (!(rn < 0.0f || rn > 1.0f || sn < 0.0f || sn > 1.0f)) center = pos[0] + rn * (pos[2] - pos[0]);
                float[] d = new float[4]; for (int i = 0; i < 4; i++) d[i] = (float)(pos[i] - center).mag();
                for (int i = 0; i < 4; i++)
                {
                    float q = d[i] == 0.0f ? 1.0f : (d[i] + d[(i + 2) & 3]) / d[(i + 2) & 3];
                    di.uv[i] *= q; di.w[i] *= q;
                    di.pos[i] = new VectorF2d((pos[i].x * vInvScreenSize.x) * 2.0f - 1.0f, ((pos[i].y * vInvScreenSize.y) * 2.0f - 1.0f) * -1.0f);
                }
                vLayers[nTargetLayer].vecDecalInstance.Add(di);
            }
        }

        public void DrawWarpedDecal(Decal decal, IList<VectorF2d> pos, Pixel tint) => 
            DrawWarpedDecal(decal, (VectorF2d[])ArrayList.Adapter((IList)pos).ToArray(), tint);

        public void DrawPartialWarpedDecal(Decal decal, IList<VectorF2d> pos, VectorF2d source_pos, VectorF2d source_size, Pixel tint) =>
            DrawPartialWarpedDecal(decal, (VectorF2d[])ArrayList.Adapter((IList)pos).ToArray(), source_pos, source_size, tint);

        public void DrawStringDecal(VectorF2d pos, string sText, Pixel col, VectorF2d scale)
        {
            VectorF2d spos = new VectorF2d(0.0f, 0.0f);
            foreach (Char c in sText)
            {
                if (c == '\n')
                {
                    spos.x = 0; spos.y += 8.0f * scale.y;
                }
                else
                {
                    Int32 ox = (c - 32) % 16;
                    Int32 oy = (c - 32) / 16;
                    DrawPartialDecal(pos + spos, fontDecal, new VectorF2d(ox * 8.0f, oy * 8.0f), new VectorF2d(8.0f, 8.0f), scale, col);
                    spos.x += 8.0f * scale.x;
                }
            }
        }

        public void DrawString(VectorI2d pos, string sText, Pixel col, UInt32 scale)
        { DrawString(pos.x, pos.y, sText, col, scale); }

        public void DrawString(Int32 x, Int32 y, string sText, Pixel col, UInt32 scale)
        {
            Int32 sx = 0;
            Int32 sy = 0;
            Pixel.Mode m = nPixelMode;
            // Thanks @tucna, spotted bug with col.ALPHA :P
            if (col.a != 255) SetPixelMode(Pixel.Mode.ALPHA);
            else SetPixelMode(Pixel.Mode.MASK);
            foreach (Char c in sText)
            {
                if (c == '\n')
                {
                    sx = 0; sy += 8 * (Int32)scale;
                }
                else
                {
                    Int32 ox = (c - 32) % 16;
                    Int32 oy = (c - 32) / 16;

                    if (scale > 1)
                    {
                        for (UInt32 i = 0; i < 8; i++)
                            for (UInt32 j = 0; j < 8; j++)
                                if (fontSprite.GetPixel((int)(i + ox * 8), (int)(j + oy * 8)).r > 0)
                                    for (UInt32 _is = 0; _is < scale; _is++)
                                        for (UInt32 js = 0; js < scale; js++)
                                            Draw((int)(x + sx + (i * scale) + _is), (int)(y + sy + (j * scale) + js), col);
                    }
                    else
                    {
                        for (UInt32 i = 0; i < 8; i++)
                            for (UInt32 j = 0; j < 8; j++)
                                if (fontSprite.GetPixel((int)(i + ox * 8), (int)(j + oy * 8)).r > 0)
                                    Draw((int)(x + sx + i), (int)(y + sy + j), col);
                    }
                    sx += 8 * (Int32)scale;
                }
            }
            SetPixelMode(m);
        }

        public void SetPixelMode(Pixel.Mode m)
        { nPixelMode = m; }

        public Pixel.Mode GetPixelMode()
        { return nPixelMode; }

        public void SetPixelMode(Func<int, int, Pixel, Pixel, Pixel> pixelMode)
        {
            funcPixelMode = pixelMode;
            nPixelMode = Pixel.Mode.CUSTOM;
        }

        public void SetPixelBlend(float fBlend)
        {
            fBlendFactor = fBlend;
            if (fBlendFactor < 0.0f) fBlendFactor = 0.0f;
            if (fBlendFactor > 1.0f) fBlendFactor = 1.0f;
        }

        // User must override these functions as required. I have not made
        // them Math.Abstract because I do need a default behaviour to occur if
        // they are not overwritten

        public abstract bool OnUserCreate();

        public abstract bool OnUserUpdate(float fElapsedTime);

        public virtual bool OnUserDestroy()
        { return true; }
        //////////////////////////////////////////////////////////////////

        public void UpdateViewport()
        {
            Int32 ww = vScreenSize.x * vPixelSize.x;
            Int32 wh = vScreenSize.y * vPixelSize.y;
            float wasp = (float)ww / (float)wh;

            vViewSize.x = (Int32)vWindowSize.x;
            vViewSize.y = (Int32)((float)vViewSize.x / wasp);

            if (vViewSize.y > vWindowSize.y)
            {
                vViewSize.y = vWindowSize.y;
                vViewSize.x = (Int32)((float)vViewSize.y * wasp);
            }

            vViewPos = (vWindowSize - vViewSize) / 2;
        }

        public void UpdateWindowSize(Int32 x, Int32 y)
        {
            vWindowSize = new VectorI2d(x, y);
            UpdateViewport();
        }

        public void Terminate()
        { 
            bAtomActive = false;
        }

        public void EngineThread(object data)
        {
            // Allow platform to do stuff here if needed, since its now in the
            // context of this thread
            if (Platform.ThreadStartUp() == RCode.FAIL) return;

            // Do engine context specific initialisation
            PrepareEngine();

            // Create user resources as part of this thread
            if (!OnUserCreate()) bAtomActive = false;

            while (bAtomActive)
            {
                // Run as fast as possible
                while (bAtomActive) { CoreUpdate(); }

                // Allow the user to free resources if they have overrided the destroy function
                if (!OnUserDestroy())
                {
                    // User denied destroy for some reason, so continue running
                    bAtomActive = true;
                }
            }

            Platform.ThreadCleanUp();
        }

        public void PrepareEngine()
        {
            // Start OpenGL, the context is owned by the game thread
            if (Platform.CreateGraphics(bFullScreen, bEnableVSYNC, vViewPos, vViewSize) == RCode.FAIL) return;

            // Construct default font sheet
            ConstructFontSheet();

            // Create Primary Layer "0"
            CreateLayer();
            vLayers[0].bUpdate = true;
            vLayers[0].bShow = true;
            SetDrawTarget(null);

            m_tp1 = DateTime.UtcNow;
            m_tp2 = DateTime.UtcNow;
        }


        public void CoreUpdate()
        {
            // Handle Timing
            m_tp2 = DateTime.UtcNow;
            TimeSpan elapsedTime = m_tp2 - m_tp1;
            m_tp1 = m_tp2;

            // Our time per frame coefficient
            float fElapsedTime = (float)elapsedTime.Ticks / (float)TimeSpan.TicksPerSecond;

            // Some platforms will need to check for events
            Platform.HandleSystemEvent();

            // Run input frame update.
            Input.UpdateInput(vViewPos, vWindowSize, vScreenSize);

            Renderer.ClearBuffer(Pixel.BLACK, true);

            // Handle Frame Update
            if (!OnUserUpdate(fElapsedTime))
                bAtomActive = false;

            // Display Frame
            Renderer.UpdateViewport(vViewPos, vViewSize);
            Renderer.ClearBuffer(Pixel.BLACK, true);

            // Layer 0 must always exist
            vLayers[0].bUpdate = true;
            vLayers[0].bShow = true;
            Renderer.PrepareDrawing();

            foreach (LayerDesc layer in vLayers)
            {
                if (layer.bShow)
                {
                    if (layer.funcHook == null)
                    {
                        Renderer.ApplyTexture(layer.nResID);
                        if (layer.bUpdate)
                        {
                            Renderer.UpdateTexture(layer.nResID, layer.pDrawTarget);
                            layer.bUpdate = false;
                        }

                        Renderer.DrawLayerQuad(layer.vOffset, layer.vScale, layer.tint);

                        // Display Decals in order for this layer
                        foreach (DecalInstance decal in layer.vecDecalInstance)
                            Renderer.DrawDecalQuad(decal);
                        layer.vecDecalInstance.Clear();
                    }
                    else
                    {
                        // Mwa ha ha.... Have Fun!!!
                        layer.funcHook();
                    }
                }
            }

            // Present Graphics to screen
            Renderer.DisplayFrame();

            // Update Title Bar
            fFrameTimer += fElapsedTime;
            nFrameCount++;
            if (fFrameTimer >= 1.0f)
            {
                nLastFPS = (uint)nFrameCount;
                fFrameTimer -= 1.0f;
                string sTitle = "OneLoneCoder.com - Pixel Game Engine - " + sAppName + " - FPS: " + nFrameCount;
                Platform.SetWindowTitle(sTitle);
                nFrameCount = 0;
            }
        }

        public void ConstructFontSheet()
        {
            string data = "";
            data += "?Q`0001oOch0o01o@F40o0<AGD4090LAGD<090@A7ch0?00O7Q`0600>00000000";
            data += "O000000nOT0063Qo4d8>?7a14Gno94AA4gno94AaOT0>o3`oO400o7QN00000400";
            data += "Of80001oOg<7O7moBGT7O7lABET024@aBEd714AiOdl717a_=TH013Q>00000000";
            data += "720D000V?V5oB3Q_HdUoE7a9@DdDE4A9@DmoE4A;Hg]oM4Aj8S4D84@`00000000";
            data += "OaPT1000Oa`^13P1@AI[?g`1@A=[OdAoHgljA4Ao?WlBA7l1710007l100000000";
            data += "ObM6000oOfMV?3QoBDD`O7a0BDDH@5A0BDD<@5A0BGeVO5ao@CQR?5Po00000000";
            data += "Oc``000?Ogij70PO2D]??0Ph2DUM@7i`2DTg@7lh2GUj?0TO0C1870T?00000000";
            data += "70<4001o?P<7?1QoHg43O;`h@GT0@:@LB@d0>:@hN@L0@?aoN@<0O7ao0000?000";
            data += "OcH0001SOglLA7mg24TnK7ln24US>0PL24U140PnOgl0>7QgOcH0K71S0000A000";
            data += "00H00000@Dm1S007@DUSg00?OdTnH7YhOfTL<7Yh@Cl0700?@Ah0300700000000";
            data += "<008001QL00ZA41a@6HnI<1i@FHLM81M@@0LG81?O`0nC?Y7?`0ZA7Y300080000";
            data += "O`082000Oh0827mo6>Hn?Wmo?6HnMb11MP08@C11H`08@FP0@@0004@000000000";
            data += "00P00001Oab00003OcKP0006@6=PMgl<@440MglH@000000`@000001P00000000";
            data += "Ob@8@@00Ob@8@Ga13R@8Mga172@8?PAo3R@827QoOb@820@0O`0007`0000007P0";
            data += "O`000P08Od400g`<3V=P0G`673IP0`@3>1`00P@6O`P00g`<O`000GP800000000";
            data += "?P9PL020O`<`N3R0@E4HC7b0@ET<ATB0@@l6C4B0O`H3N7b0?P01L3R000000020";

            fontSprite = new Sprite(128, 48);
            int px = 0, py = 0;
            for (int b = 0; b < 1024; b += 4)
            {
                UInt32 sym1 = (UInt32)data[b + 0] - 48;
                UInt32 sym2 = (UInt32)data[b + 1] - 48;
                UInt32 sym3 = (UInt32)data[b + 2] - 48;
                UInt32 sym4 = (UInt32)data[b + 3] - 48;
                UInt32 r = sym1 << 18 | sym2 << 12 | sym3 << 6 | sym4;

                for (int i = 0; i < 24; i++)
                {
                    int k = (r & (1 << i)) > 0 ? 255 : 0;
                    fontSprite.SetPixel(px, py, new Pixel(k, k, k, k));
                    if (++py == 48) { px++; py = 0; }
                }
            }

            fontDecal = new Decal(fontSprite);
        }
    }
}
