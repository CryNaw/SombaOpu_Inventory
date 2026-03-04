using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SombaOpu
{
    internal static class MouseCursorHelper
    {
        private static DispatcherTimer _idleTimer = new DispatcherTimer();
        private static Window window = new Window();
        private static Grid grid = new Grid();

        private static Point lastMousePosition;

        public static void Initialized(Window _window, Grid _grid)
        {            
            window = _window;
            grid = _grid;

            _idleTimer.Interval = TimeSpan.FromSeconds(2); // Set your timeout here
            _idleTimer.Tick += (s, e) => HideCursor();
            _idleTimer.Start();

            // Listen for any movement to show the cursor again
            window.PreviewMouseMove += MainWindow_MouseMove;
        }

        private static void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPos = e.GetPosition(window);
            if (currentPos == lastMousePosition) return;
            lastMousePosition = currentPos;

            ShowCursor();
            _idleTimer.Stop();
            _idleTimer.Start();            
        }

        private static void HideCursor()
        {
            window.Cursor = Cursors.None;
            grid.IsHitTestVisible = false;
        }

        private static void ShowCursor()
        {
            window.Cursor = Cursors.Arrow;
            grid.IsHitTestVisible = true;
        }
    }
}
