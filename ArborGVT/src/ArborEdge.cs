﻿/*
 *  ArborGVT - a graph vizualization toolkit
 *
 *  Physics code derived from springy.js, copyright (c) 2010 Dennis Hotson
 *  JavaScript library, copyright (c) 2011 Samizdat Drafting Co.
 *
 *  Fork and C# implementation, copyright (c) 2012,2016 by Serg V. Zhdanovskih.
 */

namespace ArborGVT
{
  public class ArborEdge
  {
    public ArborNode Source;
    public ArborNode Target;

    public double Length;
    public double Stiffness;
    public bool Directed;

    public ArborEdge(ArborNode src, ArborNode tgt, double len, double stiffness)
        : this(src, tgt, len, stiffness, false)
    {
    }

    public ArborEdge(ArborNode src, ArborNode tgt, double len, double stiffness, bool directed)
    {
      Source = src;
      Target = tgt;
      Length = len;
      Stiffness = stiffness;
      Directed = directed;
    }
  }
}
