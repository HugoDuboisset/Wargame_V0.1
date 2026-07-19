using System;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry;

public class Rectangle : IShape
{
    public Position Center { get; }
    public double Width { get; }
    public double Height { get; }
    
    /// <summary>
    /// Rotation en radians (sens anti-horaire).
    /// </summary>
    public double Rotation { get; }

    public Rectangle(Position center, double width, double height, double rotation = 0)
    {
        if (width <= 0) throw new ArgumentException("Width must be greater than zero.", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be greater than zero.", nameof(height));

        Center = center;
        Width = width;
        Height = height;
        Rotation = rotation;
    }

    public bool Contains(Position position)
    {
        // Translate point to origin
        double px = position.X - Center.X;
        double py = position.Y - Center.Y;

        // Rotate point in the opposite direction
        double cos = Math.Cos(-Rotation);
        double sin = Math.Sin(-Rotation);

        double rx = px * cos - py * sin;
        double ry = px * sin + py * cos;

        // Check if the rotated point is within the axis-aligned bounding box
        return Math.Abs(rx) <= Width / 2 && Math.Abs(ry) <= Height / 2;
    }

    public bool Intersects(Position start, Position end)
    {
        // Translate segment to origin
        double sx = start.X - Center.X;
        double sy = start.Y - Center.Y;
        
        double ex = end.X - Center.X;
        double ey = end.Y - Center.Y;

        // Rotate segment in the opposite direction
        double cos = Math.Cos(-Rotation);
        double sin = Math.Sin(-Rotation);

        double rsx = sx * cos - sy * sin;
        double rsy = sx * sin + sy * cos;

        double rex = ex * cos - ey * sin;
        double rey = ex * sin + ey * cos;

        // Now we just check if the line segment (rsx, rsy) to (rex, rey) intersects the AABB
        // AABB boundaries:
        double minX = -Width / 2;
        double maxX = Width / 2;
        double minY = -Height / 2;
        double maxY = Height / 2;

        return LineIntersectsAABB(rsx, rsy, rex, rey, minX, maxX, minY, maxY);
    }

    private bool LineIntersectsAABB(double x1, double y1, double x2, double y2, double minX, double maxX, double minY, double maxY)
    {
        // 1. Check if either point is inside the AABB
        if (x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY) return true;
        if (x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY) return true;

        // 2. Line intersection checks with the 4 borders
        bool IntersectsLine(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            // Returns true if line segments AB and CD intersect
            double denominator = ((bx - ax) * (dy - cy)) - ((by - ay) * (dx - cx));
            if (denominator == 0) return false;

            double numerator1 = ((ay - cy) * (dx - cx)) - ((ax - cx) * (dy - cy));
            double numerator2 = ((ay - cy) * (bx - ax)) - ((ax - cx) * (by - ay));

            // Fractional distance
            double r = numerator1 / denominator;
            double s = numerator2 / denominator;

            return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
        }

        // Top edge
        if (IntersectsLine(x1, y1, x2, y2, minX, maxY, maxX, maxY)) return true;
        // Bottom edge
        if (IntersectsLine(x1, y1, x2, y2, minX, minY, maxX, minY)) return true;
        // Left edge
        if (IntersectsLine(x1, y1, x2, y2, minX, minY, minX, maxY)) return true;
        // Right edge
        if (IntersectsLine(x1, y1, x2, y2, maxX, minY, maxX, maxY)) return true;

        return false;
    }
}
