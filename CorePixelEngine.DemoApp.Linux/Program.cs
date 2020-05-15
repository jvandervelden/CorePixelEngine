using CorePixelEngine;
using System;

namespace DemoApp
{
    public class Demo : PixelGameEngine
    {
        private Random rand = new Random();

        protected override string sAppName => "Demo App";

        public override bool OnUserCreate()
        {
            return true;
        }

        public override bool OnUserUpdate(float fElapsedTime)
        {
            if (!Input.GetKey(Key.SPACE).bHeld)
            {
                for (int x = 0; x < ScreenWidth(); x++)
                    for (int y = 0; y < ScreenHeight(); y++)
                        Draw(x, y, new Pixel(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)));
            }

            return true;
        }
    }

    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Demo game = new Demo();

            if (game.Construct(256, 256, 2, 2, false, false) == RCode.OK)
            {
                game.Start();
            }
        }
    }
}