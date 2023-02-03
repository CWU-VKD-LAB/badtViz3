using System;
using Godot;

public class TreeNodeLine : Node2D
{
    private Vector2 destPos = new Vector2(0.0f, 0.0f);
    private Color color = Color.Color8(0, 0, 0, 255);
    private float lineThickness = 8.0f;
    private bool drawRangeTicks = true;
    private float rangeTicksPadding = 40.0f;
    private float rangeTickLength = 15.0f;
    private Vector2 startTickPos = Vector2.Zero;
    private Vector2 endTickPos = Vector2.Zero;

    public override void _Ready()
    {
    }

    public Vector2 Destination
    {
        get
        {
            return destPos;
        }
        set
        {
            destPos = value;
            updateRangePos();
        }
    }

    public float TickPadding
    {
        get
        {
            return rangeTicksPadding;
        }
        set
        {
            rangeTicksPadding = value;
            updateRangePos();
        }
    }

    public override void _Draw()
    {
        DrawLine(Position, destPos, color, lineThickness, true);
        if (drawRangeTicks)
        {
            Vector2 dir = Position.DirectionTo(destPos);
            Vector2 cross = dir.Rotated(Mathf.Pi / 2) * rangeTickLength;
            DrawLine(startTickPos - cross, startTickPos + cross, color, lineThickness / 2, true);
            DrawLine(endTickPos - cross, endTickPos + cross, color, lineThickness / 2, true);
        }
    }

    private void updateRangePos()
    {
        Vector2 dir = Position.DirectionTo(destPos);
        float length = Position.DistanceTo(destPos);
        startTickPos = Position + (dir * rangeTicksPadding);
        endTickPos = Position + (dir * (length - rangeTicksPadding));
    }
}
