﻿using OpenTK.Graphics.OpenGL;
using System;

namespace AOG
{
    public class CFont
    {
        private readonly FormGPS mf;

        public const int GlyphsPerLine = 16;
        public const int GlyphLineCount = 16;
        public const int GlyphWidth = 16;
        public const int GlyphHeight = 32;
        public const int CharXSpacing = 16;

        //int FontTextureID;
        public const int textureWidth = 256;

        public const int textureHeight = 256;

        public bool isFontOn;

        public CFont(FormGPS _f)
        {
            //constructor
            mf = _f;
            isFontOn = true;
        }

        public void DrawText3D(double x1, double y1, string text, bool changeWithCam, double sizeF = 1.0)
        {
            double x = 0, y = 0;
            double size = 1;
            GL.PushMatrix();

            GL.Translate(x1, y1, 0);

            if (changeWithCam)
            {
                if (Settings.User.setDisplay_camPitch < -45)
                {
                    GL.Rotate(90, 1, 0, 0);
                    if (mf.camera.camFollowing) GL.Rotate(-mf.camHeading, 0, 1, 0);
                    size = -mf.camera.camSetDistance;
                    size = Math.Pow(size, 0.8);
                    size /= 800;
                }
                else
                {
                    if (mf.camera.camFollowing) GL.Rotate(-mf.camHeading, 0, 0, 1);
                    size = -mf.camera.camSetDistance;
                    size = Math.Pow(size, 0.85);
                    size /= 1000;
                }
            }

            size *= sizeF;

            GL.BindTexture(TextureTarget.Texture2D, mf.texture[(int)FormGPS.textures.Font]);
            GL.Enable(EnableCap.Texture2D);
            GL.Begin(PrimitiveType.TriangleStrip);

            double u_step = GlyphWidth / (double)textureWidth;
            double v_step = GlyphHeight / (double)textureHeight;

            for (int n = 0; n < text.Length; n++)
            {
                char idx = text[n];
                double u = idx % GlyphsPerLine * u_step;
                double v = idx / GlyphsPerLine * v_step;

                GL.TexCoord2(u, v);
                GL.Vertex2(x, y + GlyphHeight * size);

                GL.TexCoord2(u, v + v_step);
                GL.Vertex2(x, y);

                GL.TexCoord2(u + u_step, v);
                GL.Vertex2(x + GlyphWidth * size, y + GlyphHeight * size);

                GL.TexCoord2(u + u_step, v + v_step);
                GL.Vertex2(x + GlyphWidth * size, y);

                x += CharXSpacing * size;
            }

            GL.End();
            GL.Disable(EnableCap.Texture2D);

            GL.PopMatrix();
        }

        public void DrawText(double x, double y, string text, double size = 1.0)
        {
            GL.BindTexture(TextureTarget.Texture2D, mf.texture[(int)FormGPS.textures.Font]);
            // Select Our Texture
            //GL.Color3(0.95f, 0.95f, 0.40f);
            GL.Enable(EnableCap.Texture2D);
            GL.Begin(PrimitiveType.TriangleStrip);

            double u_step = GlyphWidth / (double)textureWidth;
            double v_step = GlyphHeight / (double)textureHeight;

            for (int n = 0; n < text.Length; n++)
            {
                char idx = text[n];
                double u = idx % GlyphsPerLine * u_step;
                double v = idx / GlyphsPerLine * v_step;

                GL.TexCoord2(u, v);
                GL.Vertex2(x, y);

                GL.TexCoord2(u, v + v_step);
                GL.Vertex2(x, y + GlyphHeight * size);

                GL.TexCoord2(u + u_step, v);
                GL.Vertex2(x + GlyphWidth * size, y);

                GL.TexCoord2(u + u_step, v + v_step);
                GL.Vertex2(x + GlyphWidth * size, y + GlyphHeight * size);

                x += CharXSpacing * size;
            }
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }
    }
}