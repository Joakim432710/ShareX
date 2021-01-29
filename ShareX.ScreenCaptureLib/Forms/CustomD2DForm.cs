/*
* MIT License
*
* Copyright (c) 2009-2018 Jingwood, unvell.com. All right reserved.
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Windows.Sdk;
using Timer = System.Windows.Forms.Timer;

namespace unvell.D2DLib.WinForm
{
	public class CustomD2DForm : Form
    {
        public CustomD2DForm()
        {
            renderInvokeThreadCancellationTokenSource = new CancellationTokenSource();
            renderInvokeThread = new Thread(() => TickFrame(renderInvokeThreadCancellationTokenSource.Token));
            renderInvokeThread.Start();
            MouseMove += OnMouseMove;
            fpsFont = new Font(Font.FontFamily, 25, Font.Style);
        }

        public Rectangle RenderTargetArea
        {
            get
            {
                if (PInvoke.GetClientRect(new HWND(Handle), out var r))
                    return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
                return ClientRectangle;
            }
        }

        private IntPtr wndHandle;
		private D2DDevice device;
        private Font fpsFont;
		public D2DDevice Device
		{
			get
            {
                var hwnd = Handle;
                if (device == null || hwnd != wndHandle)
                {
                    wndHandle = hwnd;
                    device = D2DDevice.FromHwnd(wndHandle);
                    graphics = new D2DGraphics(device);
                }
                return device;
            }
		}

		private D2DBitmap backgroundImage;

		public new D2DBitmap BackgroundImage
		{
			get => backgroundImage;
            set
			{
				if (backgroundImage != value)
				{
					if (backgroundImage != null)
					{
						backgroundImage.Dispose();
					}
					backgroundImage = value;
					Invalidate();
				}
			}
		}

		private D2DGraphics graphics;

		private int currentFps;
		private int lastFps;
		public bool ShowFPS { get; set; }
		private DateTime lastFpsUpdate = DateTime.Now;

        private Thread renderInvokeThread;
        private CancellationTokenSource renderInvokeThreadCancellationTokenSource;
        private const int RenderMilliseconds = 6;
		public bool EscapeKeyToClose { get; set; } = true;

        public bool AnimationDraw { get; set; }

        protected bool SceneChanged { get; set; }

		protected override void CreateHandle()
		{
			base.CreateHandle();

			DoubleBuffered = false;

			if (device == null || wndHandle != Handle)
			{
                wndHandle = Handle;
				device = D2DDevice.FromHwnd(wndHandle);
                graphics = new D2DGraphics(device);
            }

			graphics = new D2DGraphics(device);
			graphics.SetDPI(96, 96);
		}

        private void TickFrame(CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            sw.Start();
            long lastFrame = -RenderMilliseconds;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(SceneChanged)
                    Draw();
                else if (AnimationDraw)
                {
                    var elapsed = (sw.ElapsedMilliseconds - lastFrame);
                    if(elapsed >= RenderMilliseconds)
                        Draw();
                }

                void Draw()
                {
                    lastFrame = sw.ElapsedMilliseconds;
                    OnFrame();
                    Invalidate();
                    SceneChanged = false;
                }
            }
        }

		protected override void OnPaintBackground(PaintEventArgs e) { }
        
		protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode)
			{
				e.Graphics.Clear(Color.Black);
				e.Graphics.DrawString("D2DLib windows form cannot render in design time.", Font, Brushes.White, 10, 10);
			}
			else
			{
				if (backgroundImage != null)
				{
					graphics.BeginRender(backgroundImage);
				}
				else
				{
					graphics.BeginRender(D2DColor.FromGDIColor(BackColor));
				}

				OnRender(graphics);

				if (ShowFPS)
				{
					if (lastFpsUpdate.Second != DateTime.Now.Second)
					{
						lastFps = currentFps;
						currentFps = 0;
						lastFpsUpdate = DateTime.Now;
					}
					else
					{
						currentFps++;
					}

					var fpsInfo = $"{lastFps} fps";
					var size = e.Graphics.MeasureString(fpsInfo, fpsFont, Width);
                    Debug.WriteLine($"{lastFps} FPS");
					graphics.DrawText(fpsInfo, D2DColor.Yellow, fpsFont,
						new PointF((ClientRectangle.Width / 2.0f) - (size.Width / 2.0f), 100));
				}

				graphics.EndRender();
			}
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case (int)Win32.WMessages.WM_ERASEBKGND:
					break;

				case (int)Win32.WMessages.WM_SIZE:
					base.WndProc(ref m);
					if (device != null)
					{
						device.Resize();
						Invalidate(false);
					}
					break;

				case (int)Win32.WMessages.WM_DESTROY:
					if (backgroundImage != null) backgroundImage.Dispose();
					if (device != null) device.Dispose();
					base.WndProc(ref m);
					break;

				default:
					base.WndProc(ref m);
					break;
			}
		}

		protected virtual void OnRender(D2DGraphics g) { }

		protected virtual void OnFrame() { }

		public new void Invalidate()
		{
			base.Invalidate(false);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			switch (e.KeyCode)
			{
				case Keys.Escape:
					if (EscapeKeyToClose) Close();
					break;
			}
		}

        private Point lastMousePoint = new Point();
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            lastMousePoint = e.Location;
        }
	}
}