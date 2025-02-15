﻿/*
 *  ArborGVT - a graph vizualization toolkit
 *
 *  Physics code derived from springy.js, copyright (c) 2010 Dennis Hotson
 *  JavaScript library, copyright (c) 2011 Samizdat Drafting Co.
 *
 *  Fork and C# implementation, copyright (c) 2012,2016 by Serg V. Zhdanovskih.
 */

using System;

namespace ArborGVT
{
  public struct ArborPoint
  {
    public static readonly ArborPoint Null = new ArborPoint(double.NaN, double.NaN);

    public double X;
    public double Y;

    public ArborPoint(double x, double y)
    {
      X = x;
      Y = y;
    }

    public bool IsNull()
    {
      return (double.IsNaN(X) && double.IsNaN(Y));
    }

    public static ArborPoint NewRnd(double a = 5)
    {
      return new ArborPoint(2 * a * (ArborSystem.NextRndDouble() - 0.5), 2 * a * (ArborSystem.NextRndDouble() - 0.5));
    }

    public bool Exploded()
    {
      return (double.IsNaN(X) || double.IsNaN(Y));
    }

    public ArborPoint Add(ArborPoint a)
    {
      return new ArborPoint(X + a.X, Y + a.Y);
    }

    public ArborPoint Sub(ArborPoint a)
    {
      return new ArborPoint(X - a.X, Y - a.Y);
    }

    public ArborPoint Mul(double a)
    {
      return new ArborPoint(X * a, Y * a);
    }

    public ArborPoint Div(double a)
    {
      return new ArborPoint(X / a, Y / a);
    }

    public double Magnitude()
    {
      return Math.Sqrt(X * X + Y * Y);
    }

    public double MagnitudeSquare()
    {
      return X * X + Y * Y;
    }

    public ArborPoint Normalize()
    {
      return Div(Magnitude());
    }
  }
}
