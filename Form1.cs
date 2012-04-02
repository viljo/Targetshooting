using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace Target_shooter
{
    public partial class Form1 : Form
    { 
        
        delegate void SafeRefreshCallback();
        
        BB_Target targetdata;
        List<Circle> Solution_history = new List<Circle>();
        float C_size = (float)(1.3);       // Circle division factor
        thread_com tc;

        List<Circle> targetboard = new List<Circle>();

        public Form1(thread_com t, BB_Target target)
        {
            //Circle c;
            //PointF p = new PointF(target.X_offset,target.Y_offset);

            for (int i = 0; i < 10; i++)
            {
                targetboard.Add(new Circle(target.X_offset, target.Y_offset, i * 10 * 4));     
            }

            
            targetdata = target;
            tc = t;
            InitializeComponent();
            
            //tc.Changed += new EventHandler(UpdateScreenHandler);
            tc.RedrawForm  += new EventHandler(UpdateScreenHandler);
        }

        // CheckBoxes that control which circles are displayed.
        // private CheckBox[] CheckBoxes;


        private void UpdateScreenHandler(object sender, EventArgs e)
        {
            this.SafeRefresh();
            //MessageBox.Show("Test");
        
        }

        private void SafeRefresh()
        {
            // InvokeRequired required compares the thread ID of the

            // calling thread to the thread ID of the creating thread.

            // If these threads are different, it returns true.

            if (this.InvokeRequired)
            {
                
                SafeRefreshCallback d = new SafeRefreshCallback( SafeRefresh );
 
                //SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d);
            }
            else
            {
                this.Refresh();
            }
        }

     

        private Pen[] SolutionPens =
        {
            Pens.Red, Pens.Green, Pens.Blue, Pens.Orange,
            Pens.Lime, Pens.DeepSkyBlue, Pens.Pink, Pens.Purple,
        };

        // Solve an example problem and display the result.
        private void Form1_Load(object sender, EventArgs e)
        {
            DoubleBuffered = true;

            this.comboBox1.Items.AddRange(
            (from qP in System.IO.Ports.SerialPort.GetPortNames()
             orderby System.Text.RegularExpressions.Regex.Replace(qP, "~\\d",
                      string.Empty).PadLeft(6, '0')
             select qP).ToArray());

            this.comboBox1.SelectedIndex=0;

        }

        // Draw the circles.
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (Circle c in targetboard) // draw target board
            {
                c.Draw(e.Graphics, SolutionPens[1]);
            }

            if (targetdata.count() < 3) return;  // check if theres anything to calculate & draw

            Circle solution = new Circle(ShrinkCircle(targetdata.ResultCircleYinv(), C_size));
            solution.radius = 10;

            if (Solution_history.Count == 0)
            {
                Solution_history.Add(solution);
            }
            else
            {
                bool unique = true;

                foreach (Circle c in Solution_history)
                {
                    if (c == solution)
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique == true)
                {
                    Solution_history.Add(solution);
                }
            }

            // Draw the solution circles.
            foreach (Circle c in Solution_history)
            {
                if (chkFilled.Checked)
                {
                    Color clr = Color.FromArgb(128,
                        SolutionPens[0].Color.R,
                        SolutionPens[0].Color.G,
                        SolutionPens[0].Color.B);
                    using (Brush circle_brush = new SolidBrush(clr))
                    {
                        c.Draw(e.Graphics, circle_brush);
                    }
                }
            }

            ShrinkCircle(targetdata.YinvCircle(targetdata.ResultCircle()), C_size).Draw(e.Graphics, SolutionPens[0]);

/*
            foreach( PointF in targetdata.res
            Color clr = Color.FromArgb(128,
                    SolutionPens[0].Color.R,
                    SolutionPens[0].Color.G,
                    SolutionPens[0].Color.B);
            using (Brush circle_brush = new SolidBrush(clr))
            {
                Circle c = new Circle(ShrinkCircle(targetdata.ResultCircleYinv(), C_size));
                c.radius = 10;
                c.Draw(e.Graphics, circle_brush);
            }
*/

            // Draw the given circles.
            foreach (Circle given_circle in targetdata.Largest_3_Circles())
            {
                Color clr = Color.FromArgb(0, 0, 0, 0);
                using (Brush brush = new SolidBrush(clr))
                {
                    ShrinkCircle(targetdata.YinvCircle(given_circle), C_size).Draw(e.Graphics, brush);
                }
                ShrinkCircle(given_circle, C_size).Draw(e.Graphics, Pens.Black);
            }
        }

        private Circle ShrinkCircle(Circle c, float div)
        {
            Circle smallCircle = new Circle();
            smallCircle.Radius = c.Radius / div;
            smallCircle.Center.X = ((c.Center.X - targetdata.X_offset) / div) + targetdata.X_offset;
            smallCircle.Center.Y = ((c.Center.Y - targetdata.Y_offset) / div) + targetdata.Y_offset;

            return smallCircle;
        }

        // Find the circles that touch each of the three input circles.
        private Circle[] FindApollonianCircles(Circle[] given_circles)
        {
            // Make a list for results.
            List<Circle> solution_circles = new List<Circle>();

            solution_circles.Add(targetdata.ResultCircle());

            return solution_circles.ToArray();
        }

        

        // Refresh.
        private void chk_CheckedChanged(object sender, EventArgs e)
        {
            if (!IgnoreRefresh) Refresh();
        }

        public Stopwatch MyTimer { get; set; }

        // Check or uncheck all CheckBoxes.
        private bool IgnoreRefresh = false;
        private void btnAllOrNone_Click(object sender, EventArgs e)
        {
            MyTimer = new Stopwatch();
            MyTimer.Start();
            IgnoreRefresh = true;

            Button btn = sender as Button;
            bool is_checked = (btn.Text == "All");
/*            foreach (CheckBox chk in CheckBoxes)
            {
                chk.Checked = is_checked;
            }
*/
            IgnoreRefresh = false;
            Refresh();
            MyTimer.Stop();
            //MessageBox.Show(MyTimer.Elapsed.TotalMilliseconds.ToString());

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            tc.Serial_port_name = comboBox1.SelectedItem.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Solution_history.Clear();
            Refresh();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                C_size = (float)System.Convert.ToDouble(textBox1.Text);
                textBox1.BackColor = Color.White;
            }
            catch
            {
                textBox1.BackColor = Color.Red;
            }
            Solution_history.Clear();
            Refresh();
        }
    }
}
