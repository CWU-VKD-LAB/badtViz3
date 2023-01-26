using System;
using Godot;

public class SampleSpline : Path2D
{
    private float splineLen = 25.0f;
    private float splineLenFactor = 0.1f;
    private Color splineColor = Color.Color8(0, 0, 0, 255);

    public SampleSpline()
    {
        this.Curve = new Curve2D();
    }

    public Color Color
    {
        get
        {
            return splineColor;
        }
        set
        {
            splineColor = value;
            this.Update();
        }
    }

    public void Straighten()
    {
        for (int i = 0; i < Curve.GetPointCount(); i++)
        {
            Curve.SetPointIn(i, new Vector2());
            Curve.SetPointOut(i, new Vector2());
        }
        this.Update();
    }

    public void Smooth(bool evenOnly = false)
    {
        int pointCount = Curve.GetPointCount();
        for (int i = 0; i < pointCount; i++)
        {
            if (i == 0 || i == pointCount - 1 || (evenOnly && i % 2 == 0))
            {
                continue;
            }

            Vector2 spline = getSpline(i);
            Curve.SetPointIn(i, -spline);
            Curve.SetPointOut(i, spline);
        }
        this.Update();
    }

    public override void _Draw()
    {
        Vector2[] points = Curve.GetBakedPoints();

        if (points != null && points.Length > 1)
        {
            this.DrawPolyline(points, splineColor, 2, true);
        }
    }

    private Vector2 getSpline(int i)
    {
        Vector2 prevPoint = getPoint(i - 1);
        Vector2 nextPoint = getPoint(i + 1);
        return prevPoint.DirectionTo(nextPoint) *
            splineLenFactor * prevPoint.DistanceTo(nextPoint);
    }

    private Vector2 getPoint(int i)
    {
        return Curve.GetPointPosition(Godot.Mathf.Wrap(i, 0, Curve.GetPointCount()));
    }
}
