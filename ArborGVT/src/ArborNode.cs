/*
 *  ArborGVT - a graph vizualization toolkit
 *
 *  Physics code derived from springy.js, copyright (c) 2010 Dennis Hotson
 *  JavaScript library, copyright (c) 2011 Samizdat Drafting Co.
 *
 *  Fork and C# implementation, copyright (c) 2012,2016 by Serg V. Zhdanovskih.
 */

namespace ArborGVT
{
  public class ArborNode
  {
    public string Sign;
    public object Data;

    public bool Fixed;
    public double Mass;
    public ArborPoint Pt;

    internal ArborPoint V;
    internal ArborPoint F;

    public ArborNode(string sign)
    {
      Sign = sign;

      Fixed = false;
      Mass = 1;
      Pt = ArborPoint.Null;

      V = new ArborPoint(0, 0);
      F = new ArborPoint(0, 0);
    }

    internal void applyForce(ArborPoint a)
    {
      F = F.add(a.div(Mass));
    }
  }
}
