using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SombaOpu
{
    internal static class FullScreenHelper
    {
        public static void EnableFullscreen(Window window)
        {
            window.WindowStyle = WindowStyle.None;
            window.WindowState = WindowState.Maximized;
            window.ResizeMode = ResizeMode.NoResize;
        }

        public static void ToggleFullscreen(Window window, ref bool isFullscreen)
        {
            if (isFullscreen)
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.WindowState = WindowState.Normal;
                window.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                EnableFullscreen(window);
            }
            isFullscreen = !isFullscreen;
        }
    }
}
