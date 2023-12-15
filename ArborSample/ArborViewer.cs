/*
 *  ArborGVT - a graph vizualization toolkit
 *
 *  Physics code derived from springy.js, copyright (c) 2010 Dennis Hotson
 *  JavaScript library, copyright (c) 2011 Samizdat Drafting Co.
 *
 *  Fork and C# implementation, copyright (c) 2012,2016 by Serg V. Zhdanovskih.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Timers;
using System.Windows.Forms;

using ArborGVT;

namespace ArborSample
{
    public sealed class ArborNodeEx : ArborNode
    {
        public Color Color;
        public RectangleF Box;

        public ArborNodeEx(string sign) : base(sign)
        {
            Color = Color.Gray;
        }
    }

    public sealed class ArborSystemEx : ArborSystem
    {
        private System.Timers.Timer fTimer;

        public ArborSystemEx(double repulsion, double stiffness, double friction, IArborRenderer renderer)
            : base(repulsion, stiffness, friction, renderer)
        {
            fTimer = null;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            TickTimer();
        }

        protected override void StartTimer()
        {
            fTimer = new System.Timers.Timer();
            fTimer.AutoReset = true;
            fTimer.Interval = ParamTimeout;
            fTimer.Elapsed += TimerElapsed;
            fTimer.Start();
        }

        protected override void StopTimer()
        {
            if (fTimer != null)
            {
                fTimer.Stop();
                fTimer.Dispose();
                fTimer = null;
            }
        }

        protected override ArborNode CreateNode(string sign)
        {
            return new ArborNodeEx(sign);
        }

        protected override ArborEdge CreateEdge(ArborNode src, ArborNode tgt, double len, double stiffness, bool directed = false)
        {
            return new ArborEdge(src, tgt, len, stiffness, directed);
        }
    }

    public sealed class ArborViewer : Panel, IArborRenderer
    {
        private bool fEnergyDebug;
        private ArborNode fDragged;
        private readonly Font fDrawFont;
        private bool fNodesDragging;
        private readonly StringFormat fStrFormat;
        private readonly ArborSystemEx fSys;
        private readonly SolidBrush fBlackBrush;
        private readonly SolidBrush fWhiteBrush;

        public bool EnergyDebug
        {
            get { return fEnergyDebug; }
            set { fEnergyDebug = value; }
        }

        public bool NodesDragging
        {
            get { return fNodesDragging; }
            set { fNodesDragging = value; }
        }

        public ArborSystemEx Sys
        {
            get { return fSys; }
        }

        public ArborViewer()
        {
            base.BorderStyle = BorderStyle.Fixed3D;
            base.TabStop = true;
            base.BackColor = Color.White;

            base.DoubleBuffered = true;
            base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            base.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // repulsion - отталкивание, stiffness - тугоподвижность, friction - сила трения
            fSys = new ArborSystemEx(10000, 500/*1000*/, 0.1, this);
            fSys.SetScreenSize(Width, Height);
            fSys.AutoStop = false;

            fEnergyDebug = false;
            fDrawFont = new Font("Calibri", 9);

            fStrFormat = new StringFormat();
            fStrFormat.Alignment = StringAlignment.Center;
            fStrFormat.LineAlignment = StringAlignment.Center;

            fBlackBrush = new SolidBrush(Color.Black);
            fWhiteBrush = new SolidBrush(Color.White);
            fDragged = null;
            fNodesDragging = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fSys.Dispose();
                fDrawFont.Dispose();
                fWhiteBrush.Dispose();
                fBlackBrush.Dispose();
                fStrFormat.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            fSys.SetScreenSize(Width, Height);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics gfx = pe.Graphics;

            try
            {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;

                foreach (ArborNode node in fSys.Nodes)
                {
                    var xnode = node as ArborNodeEx;

                    xnode.Box = getNodeRect(gfx, node);
                    gfx.FillRectangle(new SolidBrush(xnode.Color), xnode.Box);
                    gfx.DrawString(node.Sign, fDrawFont, fWhiteBrush, xnode.Box, fStrFormat);
                }

                using (Pen grayPen = new Pen(Color.Gray, 1))
                {
                    grayPen.StartCap = LineCap.NoAnchor;
                    grayPen.EndCap = LineCap.ArrowAnchor;

                    foreach (ArborEdge edge in fSys.Edges)
                    {
                        var srcNode = edge.Source as ArborNodeEx;
                        var tgtNode = edge.Target as ArborNodeEx;

                        ArborPoint pt1 = fSys.ToScreen(srcNode.Pt);
                        ArborPoint pt2 = fSys.ToScreen(tgtNode.Pt);

                        ArborPoint tail = intersect_line_box(pt1, pt2, srcNode.Box);
                        ArborPoint head = (tail.IsNull()) ? ArborPoint.Null : intersect_line_box(tail, pt2, tgtNode.Box);

                        if (!head.IsNull() && !tail.IsNull())
                        {
                            gfx.DrawLine(grayPen, (int)tail.X, (int)tail.Y, (int)head.X, (int)head.Y);
                        }
                    }
                }

                if (fEnergyDebug)
                {
                    string energy = "max=" + fSys.EnergyMax.ToString("0.00000") + ", mean=" + fSys.EnergyMean.ToString("0.00000");
                    gfx.DrawString(energy, fDrawFont, fBlackBrush, 10, 10);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ArborViewer.OnPaint(): " + ex.Message);
            }
        }

        public static ArborPoint intersect_line_line(ArborPoint p1, ArborPoint p2, ArborPoint p3, ArborPoint p4)
        {
            double denom = ((p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y));
            if (denom == 0.0f) return ArborPoint.Null; // lines are parallel

            double ua = ((p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X)) / denom;
            double ub = ((p2.X - p1.X) * (p1.Y - p3.Y) - (p2.Y - p1.Y) * (p1.X - p3.X)) / denom;

            if (ua < 0 || ua > 1 || ub < 0 || ub > 1) return ArborPoint.Null;

            return new ArborPoint(p1.X + ua * (p2.X - p1.X), p1.Y + ua * (p2.Y - p1.Y));
        }

        public ArborPoint intersect_line_box(ArborPoint p1, ArborPoint p2, RectangleF boxTuple)
        {
            double bx = boxTuple.X;
            double by = boxTuple.Y;
            double bw = boxTuple.Width;
            double bh = boxTuple.Height;

            ArborPoint tl = new ArborPoint(bx, by);
            ArborPoint tr = new ArborPoint(bx + bw, by);
            ArborPoint bl = new ArborPoint(bx, by + bh);
            ArborPoint br = new ArborPoint(bx + bw, by + bh);

            ArborPoint pt;

            pt = intersect_line_line(p1, p2, tl, tr);
            if (!pt.IsNull()) return pt;

            pt = intersect_line_line(p1, p2, tr, br);
            if (!pt.IsNull()) return pt;

            pt = intersect_line_line(p1, p2, br, bl);
            if (!pt.IsNull()) return pt;

            pt = intersect_line_line(p1, p2, bl, tl);
            if (!pt.IsNull()) return pt;

            return ArborPoint.Null;
        }

        public void start()
        {
            fSys.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Focused) base.Focus();

            if (fNodesDragging)
            {
                fDragged = fSys.Nearest(e.X, e.Y);

                if (fDragged != null)
                {
                    fDragged.Fixed = true;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (fNodesDragging && fDragged != null)
            {
                fDragged.Fixed = false;
                //this.fDragged.Mass = 1000;
                fDragged = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (fNodesDragging && fDragged != null)
            {
                fDragged.Pt = fSys.FromScreen(e.X, e.Y);
            }
        }

        public RectangleF getNodeRect(Graphics gfx, ArborNode node)
        {
            SizeF tsz = gfx.MeasureString(node.Sign, fDrawFont);
            float w = tsz.Width + 10;
            float h = tsz.Height + 4;
            ArborPoint pt = fSys.ToScreen(node.Pt);
            pt.X = Math.Floor(pt.X);
            pt.Y = Math.Floor(pt.Y);

            return new RectangleF((float)pt.X - w / 2, (float)pt.Y - h / 2, w, h);
        }

        public ArborNode getNodeByCoord(int x, int y)
        {
            return fSys.Nearest(x, y);

            /*foreach (ArborNode node in fSys.Nodes)
            {
                if (node.Box.Contains(x, y)) {
                    return node;
                }
            }
            return null;*/
        }

        public void doSample()
        {
            fSys.AddNode("1")/*.Mass = 50*/;

            fSys.AddEdge("1", "4"/*, 10*/);
            fSys.AddEdge("1", "12"/*, 10*/);
            fSys.AddEdge("4", "21");
            fSys.AddEdge("4", "23"/*, 20*/);
            fSys.AddEdge("7", "34");
            fSys.AddEdge("7", "13");
            fSys.AddEdge("7", "44");
            fSys.AddEdge("12", "25");
            fSys.AddEdge("12", "24");
            fSys.AddEdge("23", "50");
            fSys.AddEdge("23", "53");
            fSys.AddEdge("24", "6");
            fSys.AddEdge("24", "42"/*, 20*/);
            fSys.AddEdge("25", "94");
            fSys.AddEdge("25", "66");
            fSys.AddEdge("32", "47");
            fSys.AddEdge("32", "84");
            fSys.AddEdge("42", "32");
            fSys.AddEdge("42", "7");
            fSys.AddEdge("50", "72");
            fSys.AddEdge("50", "65");
            fSys.AddEdge("53", "67");
            fSys.AddEdge("53", "68");
            fSys.AddEdge("66", "79");
            fSys.AddEdge("66", "80");
            fSys.AddEdge("67", "88");
            fSys.AddEdge("67", "83");
            fSys.AddEdge("68", "77");
            fSys.AddEdge("68", "91");
            fSys.AddEdge("80", "99");
            fSys.AddEdge("80", "97");
            fSys.AddEdge("88", "110");
            fSys.AddEdge("88", "104");
            fSys.AddEdge("91", "106");
            fSys.AddEdge("91", "100");
        }
    }
}
