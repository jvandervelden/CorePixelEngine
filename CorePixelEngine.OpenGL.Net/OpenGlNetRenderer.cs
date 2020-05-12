using Khronos;
using OpenGL;
using System;
using System.Collections.Generic;

namespace CorePixelEngine.OpenGL.Net
{
    public class OpenGlNetRenderer : Renderer
    {
        DeviceContext deviceContext = null;
        IntPtr windowHandle = IntPtr.Zero;
        IntPtr renderContext = IntPtr.Zero;

        public void ApplyTexture(uint id)
        {
            Gl.BindTexture(TextureTarget.Texture2d, id);
        }

        public void ClearBuffer(Pixel p, bool bDepth)
        {
            Gl.ClearColor((float)p.r / 255.0f, (float)p.g / 255.0f, (float)p.b / 255.0f, (float)p.a / 255.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            if (bDepth) Gl.Clear(ClearBufferMask.DepthBufferBit);
        }

        public RCode CreateDevice(IList<object> parameters, bool bFullScreen, bool bVSYNC)
        {
            windowHandle = (IntPtr)parameters[0];

            if (parameters.Count == 1)
            {
                deviceContext = DeviceContext.Create(IntPtr.Zero, windowHandle);
                deviceContext.ChoosePixelFormat(new DevicePixelFormat()
                {
                    DoubleBuffer = true
                });

                renderContext = deviceContext.CreateContext(IntPtr.Zero);

                if (renderContext == IntPtr.Zero) return RCode.FAIL;
                if (!deviceContext.MakeCurrent(renderContext)) return RCode.FAIL;

                deviceContext.SwapInterval(0);
            } 
            else if (parameters.Count > 1)
            {
                renderContext = (IntPtr)parameters[1];
                //deviceContext = DeviceContext.Create(renderContext, windowHandle);
                Gl.Initialize();
                Gl.BindAPI(KhronosVersion.Parse("1.1"), null);
            }

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Enable(EnableCap.Texture2d);
            //deviceContext.SwapInterval(0);

            return RCode.OK;
        }

        public uint CreateTexture(uint width, uint height)
        {
            uint id = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, id);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Nearest);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMagFilter.Nearest);
            //Gl.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (IntPtr)TextureEnvMode.Modulate);
            return id;
        }

        public uint DeleteTexture(uint id)
        {
            Gl.DeleteTextures(new uint[] { id });
            return id;
        }

        public RCode DestroyDevice()
        {
            deviceContext?.DeleteContext(renderContext);
            deviceContext?.Dispose();
            return RCode.OK;
        }

        public void DisplayFrame()
        {
            deviceContext?.SwapBuffers();
        }

        public void DrawDecalQuad(DecalInstance decal)
        {
            Gl.BindTexture(TextureTarget.Texture2d, (uint)decal.decal.id);
            Gl.Begin(PrimitiveType.Quads);
            Gl.Color4(decal.tint.r, decal.tint.g, decal.tint.b, decal.tint.a);
            Gl.TexCoord4(decal.uv[0].x, decal.uv[0].y, 0.0f, decal.w[0]); Gl.Vertex2(decal.pos[0].x, decal.pos[0].y);
            Gl.TexCoord4(decal.uv[1].x, decal.uv[1].y, 0.0f, decal.w[1]); Gl.Vertex2(decal.pos[1].x, decal.pos[1].y);
            Gl.TexCoord4(decal.uv[2].x, decal.uv[2].y, 0.0f, decal.w[2]); Gl.Vertex2(decal.pos[2].x, decal.pos[2].y);
            Gl.TexCoord4(decal.uv[3].x, decal.uv[3].y, 0.0f, decal.w[3]); Gl.Vertex2(decal.pos[3].x, decal.pos[3].y);
            Gl.End();
        }

        public void DrawLayerQuad(VectorF2d offset, VectorF2d scale, Pixel tint)
        {
            Gl.Begin(PrimitiveType.Quads);
            Gl.Color4(tint.r, tint.g, tint.b, tint.a);
            Gl.TexCoord2(0.0f * scale.x + offset.x, 1.0f * scale.y + offset.y);
            Gl.Vertex3(-1.0f /*+ vSubPixelOffset.x*/, -1.0f /*+ vSubPixelOffset.y*/, 0.0f);
            Gl.TexCoord2(0.0f * scale.x + offset.x, 0.0f * scale.y + offset.y);
            Gl.Vertex3(-1.0f /*+ vSubPixelOffset.x*/, 1.0f /*+ vSubPixelOffset.y*/, 0.0f);
            Gl.TexCoord2(1.0f * scale.x + offset.x, 0.0f * scale.y + offset.y);
            Gl.Vertex3(1.0f /*+ vSubPixelOffset.x*/, 1.0f /*+ vSubPixelOffset.y*/, 0.0f);
            Gl.TexCoord2(1.0f * scale.x + offset.x, 1.0f * scale.y + offset.y);
            Gl.Vertex3(1.0f /*+ vSubPixelOffset.x*/, -1.0f /*+ vSubPixelOffset.y*/, 0.0f);
            Gl.End();
        }

        public void PrepareDevice()
        {
        }

        public void PrepareDrawing()
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void SetPixelGameEngine(PixelGameEngine pge)
        {
        }

        public void UpdateTexture(uint id, Sprite spr)
        {
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, spr.width, spr.height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, spr.GetPackedData());
        }

        public void UpdateViewport(VectorI2d pos, VectorI2d size)
        {
            Gl.Viewport(pos.x, pos.y, size.x, size.y);
        }
    }
}
