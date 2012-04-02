using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Diagnostics;

namespace Target_shooter
{
    // Represents a circle.
    public class Circle
    {
        public PointF Center;
        public float Radius;
        public Circle()
            : this(0, 0, 0)
        {
        }

        public float radius{
            set { Radius = value; }
            get { return Radius; }
        }

        public Circle(float new_x, float new_y, float new_radius)
        {
            Center = new PointF(new_x, new_y);
            Radius = Math.Abs(new_radius);
        }
        public Circle(Circle c)
        {
            Center = c.Center;
            Radius = c.Radius;
        }

        // Return the circle's bounds.
        public RectangleF GetBounds()
        {
            return new RectangleF(
                Center.X - Radius, Center.Y - Radius,
                2 * Radius, 2 * Radius);
        }

        // Draw the circle.
        public void Draw(Graphics gr, Pen pen)
        {
            if (Radius > 0) gr.DrawEllipse(pen, GetBounds());
        }
        public void Draw(Graphics gr, Brush brush)
        {
            if (Radius > 0) gr.FillEllipse(brush, GetBounds());
        }
        public void Draw(Graphics gr, Brush brush, Pen pen)
        {
            Draw(gr, brush);
            Draw(gr, pen);
        }
    }
}
