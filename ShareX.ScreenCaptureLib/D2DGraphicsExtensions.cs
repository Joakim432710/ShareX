using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using unvell.D2DLib;

namespace ShareX.ScreenCaptureLib
{
    public static class D2DGraphicsExtensions
    {

        public static void DrawRectangleProper(this D2DGraphics g, D2DColor color, D2DRect rect)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                g.DrawRectangle(rect, color);
            }
        }

        public static D2DPathGeometry GetPathGeometry(this GraphicsPath gdiPath, D2DDevice device)
        {
            var geometry = device.CreatePathGeometry();
            
            var index = -1;
            var currentShapePoints = new List<PointF>();
            while (++index < gdiPath.PointCount)
            {
                var currentPoint = gdiPath.PathPoints[index];
                var pointData = gdiPath.PathTypes[index];

                void closeBezier()
                {
                    if (currentShapePoints.Count > 3)
                    {
                        geometry.SetStartPoint(currentShapePoints[0]);
                        var beziers = new List<D2DBezierSegment>();
                        for (var iii = 1; iii < currentShapePoints.Count - 2; iii += 3)
                        {
                            var segment = new D2DBezierSegment
                            {
                                point1 = currentShapePoints[iii],
                                point2 = currentShapePoints[iii+1],
                                point3 = currentShapePoints[iii+2]
                            };
                            beziers.Add(segment);
                        }
                        geometry.AddBeziers(beziers.ToArray());
                    }
                }

                void closeLines()
                {
                    geometry.AddLines(currentShapePoints.Select(s => (D2DPoint)s).ToArray());
                }

                //Indicates that the point is the start of a figure
                if (pointData == (int)PathPointType.Start)
                {
                    currentShapePoints.Clear();
                    currentShapePoints.Add(currentPoint);
                }
                else if ((pointData & (int) PathPointType.CloseSubpath) == (int) PathPointType.CloseSubpath)
                {
                    currentShapePoints.Add(currentPoint);

                    var prevData = gdiPath.PathTypes[index];
                    if ((prevData & (~(int)PathPointType.CloseSubpath)) == (int) PathPointType.Line)
                    {
                        closeLines();
                    }
                    else if ((prevData & (int) PathPointType.Bezier) == (int) PathPointType.Bezier)
                    {
                        closeBezier();
                    }
                }
                //Indicates that the point is one of the two endpoints of a line.
                else if ((pointData & (int)PathPointType.Line) == (int)PathPointType.Line)
                {
                    currentShapePoints.Add(currentPoint);
                }
                else if ((pointData & (int) PathPointType.Bezier) == (int) PathPointType.Bezier)
                {
                    currentShapePoints.Add(currentPoint);
                }
            }
            geometry.ClosePath();

            return geometry;
        }

        public static void DrawRectangleProper(this D2DGraphics g, D2DPen pen, D2DRect rect)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                g.DrawRectangle(rect, pen);
            }
        }

        public static void DrawTextWithShadow(this D2DGraphics g, string text, PointF position, Font font, D2DColor textColor, D2DColor shadowColor)
        {
            DrawTextWithShadow(g, text, position, font, textColor, shadowColor, new Point(1, 1));
        }

        public static void DrawTextWithShadow(this D2DGraphics g, string text, PointF position, Font font, D2DColor textColor, D2DColor shadowColor, Point shadowOffset)
        {
            g.DrawText(text, shadowColor, font.Name, font.Size, new D2DRect(position.X + shadowOffset.X, position.Y + shadowOffset.Y, 1000, 1000));
            g.DrawText(text, textColor, font.Name, font.Size, new D2DRect(position.X, position.Y, 1000, 1000));
        }

        public static void DrawCross(this D2DGraphics g, D2DColor color, Point center, int crossSize)
        {
            if (crossSize > 0)
            {
                // Horizontal
                g.DrawLine(center.X - crossSize, center.Y, center.X + crossSize, center.Y, color);

                // Vertical
                g.DrawLine(center.X, center.Y - crossSize, center.X, center.Y + crossSize, color);
            }
        }

        public static void DrawCornerLines(this D2DGraphics g, Rectangle rect, D2DColor color, int lineSize)
        {
            if (rect.Width <= lineSize * 2)
            {
                g.DrawLine(rect.X, rect.Y, rect.Right - 1, rect.Y, color);
                g.DrawLine(rect.X, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1, color);
            }
            else
            {
                // Top left
                g.DrawLine(rect.X, rect.Y, rect.X + lineSize, rect.Y, color);

                // Top right
                g.DrawLine(rect.Right - 1, rect.Y, rect.Right - 1 - lineSize, rect.Y, color);

                // Bottom left
                g.DrawLine(rect.X, rect.Bottom - 1, rect.X + lineSize, rect.Bottom - 1, color);

                // Bottom right
                g.DrawLine(rect.Right - 1, rect.Bottom - 1, rect.Right - 1 - lineSize, rect.Bottom - 1, color);
            }

            if (rect.Height <= lineSize * 2)
            {
                g.DrawLine(rect.X, rect.Y, rect.X, rect.Bottom - 1, color);
                g.DrawLine(rect.Right - 1, rect.Y, rect.Right - 1, rect.Bottom - 1, color);
            }
            else
            {
                // Top left
                g.DrawLine(rect.X, rect.Y, rect.X, rect.Y + lineSize, color);

                // Top right
                g.DrawLine(rect.Right - 1, rect.Y, rect.Right - 1, rect.Y + lineSize, color);

                // Bottom left
                g.DrawLine(rect.X, rect.Bottom - 1, rect.X, rect.Bottom - 1 - lineSize, color);

                // Bottom right
                g.DrawLine(rect.Right - 1, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1 - lineSize, color);
            }
        }

        public static D2DDashStyle ToD2DDashStyle(this BorderStyle borderStyle) => (D2DDashStyle) borderStyle;

        public static Color ToGDIColor(this D2DColor color) => Color.FromArgb((int)(color.a * 255f),(int)(color.r * 255f),(int)(color.g * 255f),(int)(color.b * 255f));

        public static D2DColor ToD2DColor(this Color color) => new D2DColor(color.A / 255f, color.R / 255f, color.G / 255f, color.B / 255f);
    }
}
