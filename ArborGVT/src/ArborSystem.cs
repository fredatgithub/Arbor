/*
 *  ArborGVT - a graph vizualization toolkit
 *
 *  Physics code derived from springy.js, copyright (c) 2010 Dennis Hotson
 *  JavaScript library, copyright (c) 2011 Samizdat Drafting Co.
 *
 *  Fork and C# implementation, copyright (c) 2012,2016 by Serg V. Zhdanovskih.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArborGVT
{
  internal class PSBounds
  {
    public ArborPoint LeftTop = ArborPoint.Null;
    public ArborPoint RightBottom = ArborPoint.Null;

    public PSBounds(ArborPoint leftTop, ArborPoint rightBottom)
    {
      LeftTop = leftTop;
      RightBottom = rightBottom;
    }
  }

  public abstract class ArborSystem : IDisposable
  {
    private static readonly int DEBUG_PROFILER_LIMIT = 0;//2000;

    private static readonly Random _random = new Random();

    private readonly int[] margins = new int[4] { 20, 20, 20, 20 };
    private const double Mag = 0.04;

    private bool fAutoStop;
    private bool fBusy;
    private bool fDisposed;
    private readonly List<ArborEdge> fEdges;
    private PSBounds fGraphBounds;
    private int fIterationsCounter;
    private readonly Hashtable fNames;
    private readonly List<ArborNode> fNodes;
    private EventHandler fOnStart;
    private EventHandler fOnStop;
    private readonly IArborRenderer fRenderer;
    private DateTime fPrevTime;
    private int fScreenHeight;
    private int fScreenWidth;
    private double fStopThreshold;
    private PSBounds fViewBounds;

    public double EnergySum = 0;
    public double EnergyMax = 0;
    public double EnergyMean = 0;

    public double ParamRepulsion = 1000; // отражение, отвращение, отталкивание
    public double ParamStiffness = 600; // церемонность, тугоподвижность
    public double ParamFriction = 0.5; // трение
    public double ParamDt = 0.01; // 0.02;
    public bool ParamGravity = false;
    public double ParamPrecision = 0.6;
    public double ParamTimeout = 1000.0f / 100.0f;
    public double ParamTheta = 0.4;

    #region Properties

    public bool AutoStop
    {
      get { return fAutoStop; }
      set { fAutoStop = value; }
    }

    public List<ArborNode> Nodes
    {
      get { return fNodes; }
    }

    public List<ArborEdge> Edges
    {
      get { return fEdges; }
    }

    public event EventHandler OnStart
    {
      add
      {
        fOnStart = value;
      }
      remove
      {
        if (fOnStart == value)
        {
          fOnStart = null;
        }
      }
    }

    public event EventHandler OnStop
    {
      add
      {
        fOnStop = value;
      }
      remove
      {
        if (fOnStop == value)
        {
          fOnStop = null;
        }
      }
    }

    public double StopThreshold
    {
      get { return fStopThreshold; }
      set { fStopThreshold = value; }
    }

    #endregion

    public ArborSystem(double repulsion, double stiffness, double friction, IArborRenderer renderer)
    {
      fAutoStop = true;
      fBusy = false;
      fNames = new Hashtable();
      fNodes = new List<ArborNode>();
      fEdges = new List<ArborEdge>();
      fRenderer = renderer;
      fPrevTime = DateTime.FromBinary(0);
      fStopThreshold = /*0.05*/ 0.7;

      ParamRepulsion = repulsion;
      ParamStiffness = stiffness;
      ParamFriction = friction;
    }

    public void Dispose()
    {
      if (!fDisposed)
      {
        Stop();
        fDisposed = true;
      }
    }

    protected abstract void StartTimer();

    protected abstract void StopTimer();

    public void Start()
    {
      if (fOnStart != null) fOnStart(this, new EventArgs());

      /*if (fTimer != null)
      {
          return;
      }*/
      fPrevTime = DateTime.FromBinary(0);

      fIterationsCounter = 0;

      StartTimer();
    }

    public void Stop()
    {
      StopTimer();
      //if (fOnStop != null) fOnStop(this, new EventArgs());
      fOnStop?.Invoke(this, new EventArgs());
    }

    protected virtual ArborNode CreateNode(string sign)
    {
      return new ArborNode(sign);
    }

    protected virtual ArborEdge CreateEdge(ArborNode src, ArborNode tgt, double len, double stiffness, bool directed = false)
    {
      return new ArborEdge(src, tgt, len, stiffness, directed);
    }

    public ArborNode AddNode(string sign, double x, double y)
    {
      ArborNode node = getNode(sign);
      if (node != null) return node;

      node = CreateNode(sign);
      node.Pt = new ArborPoint(x, y);

      fNames.Add(sign, node);
      fNodes.Add(node);

      return node;
    }

    public ArborNode AddNode(string sign)
    {
      ArborPoint lt = fGraphBounds.LeftTop;
      ArborPoint rb = fGraphBounds.RightBottom;
      double xx = lt.X + (rb.X - lt.X) * ArborSystem.NextRndDouble();
      double yy = lt.Y + (rb.Y - lt.Y) * ArborSystem.NextRndDouble();

      return AddNode(sign, xx, yy);
    }

    public ArborNode getNode(string sign)
    {
      return (ArborNode)fNames[sign];
    }

    public ArborEdge AddEdge(string srcSign, string tgtSign, double len = 1.0)
    {
      ArborNode src = getNode(srcSign);
      src = (src != null) ? src : AddNode(srcSign);

      ArborNode tgt = getNode(tgtSign);
      tgt = (tgt != null) ? tgt : AddNode(tgtSign);

      ArborEdge x = null;
      if (src != null && tgt != null)
      {
        foreach (ArborEdge edge in fEdges)
        {
          if (edge.Source == src && edge.Target == tgt)
          {
            x = edge;
            break;
          }
        }
      }

      if (x == null)
      {
        x = CreateEdge(src, tgt, len, ParamStiffness);
        fEdges.Add(x);
      }

      return x;
    }

    public void SetScreenSize(int width, int height)
    {
      fScreenWidth = width;
      fScreenHeight = height;
      UpdateViewBounds();
    }

    public ArborPoint ToScreen(ArborPoint pt)
    {
      if (fViewBounds == null) return ArborPoint.Null;

      ArborPoint vd = fViewBounds.RightBottom.Sub(fViewBounds.LeftTop);
      double sx = margins[3] + pt.Sub(fViewBounds.LeftTop).Div(vd.X).X * (fScreenWidth - (margins[1] + margins[3]));
      double sy = margins[0] + pt.Sub(fViewBounds.LeftTop).Div(vd.Y).Y * (fScreenHeight - (margins[0] + margins[2]));
      return new ArborPoint(sx, sy);
    }

    public ArborPoint FromScreen(double sx, double sy)
    {
      if (fViewBounds == null) return ArborPoint.Null;

      ArborPoint vd = fViewBounds.RightBottom.Sub(fViewBounds.LeftTop);
      double x = (sx - margins[3]) / (fScreenWidth - (margins[1] + margins[3])) * vd.X + fViewBounds.LeftTop.X;
      double y = (sy - margins[0]) / (fScreenHeight - (margins[0] + margins[2])) * vd.Y + fViewBounds.LeftTop.Y;
      return new ArborPoint(x, y);
    }

    public ArborNode Nearest(int sx, int sy)
    {
      ArborPoint x = FromScreen(sx, sy);

      ArborNode resNode = null;
      double minDist = +1.0;

      foreach (ArborNode node in fNodes)
      {
        ArborPoint z = node.Pt;
        if (z.Exploded())
        {
          continue;
        }

        double dist = z.Sub(x).Magnitude();
        if (dist < minDist)
        {
          resNode = node;
          minDist = dist;
        }
      }

      //minDist = this.toScreen(resNode.Pt).sub(this.toScreen(x)).magnitude();
      return resNode;
    }

    private void UpdateGraphBounds()
    {
      ArborPoint lt = new ArborPoint(-1, -1);
      ArborPoint rb = new ArborPoint(1, 1);

      foreach (ArborNode node in fNodes)
      {
        ArborPoint pt = node.Pt;
        if (pt.Exploded()) continue;

        if (pt.X < lt.X) lt.X = pt.X;
        if (pt.Y < lt.Y) lt.Y = pt.Y;
        if (pt.X > rb.X) rb.X = pt.X;
        if (pt.Y > rb.Y) rb.Y = pt.Y;
      }

      lt.X -= 1.2;
      lt.Y -= 1.2;
      rb.X += 1.2;
      rb.Y += 1.2;

      ArborPoint sz = rb.Sub(lt);
      ArborPoint cent = lt.Add(sz.Div(2));
      ArborPoint d = new ArborPoint(Math.Max(sz.X, 4.0), Math.Max(sz.Y, 4.0)).Div(2);

      fGraphBounds = new PSBounds(cent.Sub(d), cent.Add(d));
    }

    private void UpdateViewBounds()
    {
      try
      {
        UpdateGraphBounds();

        if (fViewBounds == null)
        {
          fViewBounds = fGraphBounds;
          return;
        }

        ArborPoint vLT = fGraphBounds.LeftTop.Sub(fViewBounds.LeftTop).Mul(Mag);
        ArborPoint vRB = fGraphBounds.RightBottom.Sub(fViewBounds.RightBottom).Mul(Mag);

        double aX = vLT.Magnitude() * fScreenWidth;
        double aY = vRB.Magnitude() * fScreenHeight;

        if (aX > 1 || aY > 1)
        {
          ArborPoint nbLT = fViewBounds.LeftTop.Add(vLT);
          ArborPoint nbRB = fViewBounds.RightBottom.Add(vRB);

          fViewBounds = new PSBounds(nbLT, nbRB);
        }
      }
      catch (Exception exception)
      {
        Debug.WriteLine("ArborSystem.updateViewBounds(): " + exception.Message);
      }
    }

    protected void TickTimer()
    {
      if (DEBUG_PROFILER_LIMIT > 0)
      {
        if (fIterationsCounter >= DEBUG_PROFILER_LIMIT)
        {
          return;
        }
        else
        {
          fIterationsCounter++;
        }
      }

      if (fBusy) return;
      fBusy = true;
      try
      {
        UpdatePhysics();
        UpdateViewBounds();

        if (fRenderer != null)
        {
          fRenderer.Invalidate();
        }

        if (fAutoStop)
        {
          if (EnergyMean <= fStopThreshold)
          {
            if (fPrevTime == DateTime.FromBinary(0))
            {
              fPrevTime = DateTime.Now;
            }
            TimeSpan ts = DateTime.Now - fPrevTime;
            if (ts.TotalMilliseconds > 1000)
            {
              Stop();
            }
          }
          else
          {
            fPrevTime = DateTime.FromBinary(0);
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine("ArborSystem.tickTimer(): " + ex.Message);
      }
      fBusy = false;
    }

    private void UpdatePhysics()
    {
      try
      {
        // tend particles
        foreach (ArborNode p in fNodes)
        {
          p.V.X = 0;
          p.V.Y = 0;
        }

        if (ParamStiffness > 0)
        {
          ApplySprings();
        }

        // euler integrator
        if (ParamRepulsion > 0)
        {
          ApplyBarnesHutRepulsion();
        }

        UpdateVelocityAndPosition(ParamDt);
      }
      catch (Exception exception)
      {
        Debug.WriteLine("ArborSystem.updatePhysics(): " + exception.Message);
      }
    }

    private void ApplyBarnesHutRepulsion()
    {
      BarnesHutTree bht = new BarnesHutTree(fGraphBounds.LeftTop, fGraphBounds.RightBottom, ParamTheta);

      foreach (ArborNode node in fNodes)
      {
        bht.Insert(node);
      }

      foreach (ArborNode node in fNodes)
      {
        bht.ApplyForces(node, ParamRepulsion);
      }
    }

    private void ApplySprings()
    {
      foreach (ArborEdge edge in fEdges)
      {
        ArborPoint s = edge.Target.Pt.Sub(edge.Source.Pt);
        double sMag = s.Magnitude();

        ArborPoint r = ((sMag > 0) ? s : ArborPoint.NewRnd(1)).Normalize();
        double q = edge.Stiffness * (edge.Length - sMag);

        edge.Source.ApplyForce(r.Mul(q * -0.5));
        edge.Target.ApplyForce(r.Mul(q * 0.5));
      }
    }

    private void UpdateVelocityAndPosition(double dt)
    {
      int size = fNodes.Count;
      if (size == 0)
      {
        EnergySum = 0;
        EnergyMax = 0;
        EnergyMean = 0;
        return;
      }

      double eSum = 0;
      double eMax = 0;

      // calc center drift
      ArborPoint rr = new ArborPoint(0, 0);
      foreach (ArborNode node in fNodes)
      {
        rr = rr.Sub(node.Pt);
      }
      ArborPoint drift = rr.Div(size);

      // main updates loop
      foreach (ArborNode node in fNodes)
      {
        // apply center drift
        node.ApplyForce(drift);

        // apply center gravity
        if (ParamGravity)
        {
          ArborPoint q = node.Pt.Mul(-1);
          node.ApplyForce(q.Mul(ParamRepulsion / 100));
        }

        // update velocities
        if (node.Fixed)
        {
          node.V = new ArborPoint(0, 0);
        }
        else
        {
          node.V = node.V.Add(node.F.Mul(dt));
          node.V = node.V.Mul(1 - ParamFriction);

          double r = node.V.MagnitudeSquare();
          if (r > 1000000)
          {
            node.V = node.V.Div(r);
          }
        }

        node.F.X = node.F.Y = 0;

        // update positions
        node.Pt = node.Pt.Add(node.V.Mul(dt));

        // update energy
        double z = node.V.MagnitudeSquare();
        eSum += z;
        eMax = Math.Max(z, eMax);
      }

      EnergySum = eSum;
      EnergyMax = eMax;
      EnergyMean = eSum / size;
    }

    internal static double NextRndDouble()
    {
      return _random.NextDouble();
    }
  }
}
