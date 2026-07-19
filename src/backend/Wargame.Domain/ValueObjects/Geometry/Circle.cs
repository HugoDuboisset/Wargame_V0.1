using System;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry;

public class Circle : IShape
{
    public Position Center { get; }
    public double Radius { get; }

    public Circle(Position center, double radius)
    {
        if (radius <= 0)
            throw new ArgumentException("Radius must be greater than zero.", nameof(radius));

        Center = center;
        Radius = radius;
    }

    public bool Contains(Position position)
    {
        double dx = position.X - Center.X;
        double dy = position.Y - Center.Y;
        return (dx * dx + dy * dy) <= (Radius * Radius);
    }

    public bool Intersects(Position start, Position end)
    {
        // Vector from start to end
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        
        // Vector from start to circle center
        double fx = start.X - Center.X;
        double fy = start.Y - Center.Y;

        double a = dx * dx + dy * dy;
        double b = 2 * (fx * dx + fy * dy);
        double c = (fx * fx + fy * fy) - (Radius * Radius);

        double discriminant = b * b - 4 * a * c;

        // No real roots -> no intersection
        if (discriminant < 0)
            return false;

        // Ray intersection check (we need to check if the intersection falls within the segment [0, 1])
        discriminant = Math.Sqrt(discriminant);

        double t1 = (-b - discriminant) / (2 * a);
        double t2 = (-b + discriminant) / (2 * a);

        // Check if either t1 or t2 is within the [0, 1] segment
        if (t1 >= 0 && t1 <= 1) return true;
        if (t2 >= 0 && t2 <= 1) return true;

        // Check if the circle completely contains the line segment
        // Wait, if it contains the line segment, does it "intersect" the area?
        // Yes, if start or end is inside the circle, it intersects the area.
        if (Contains(start) || Contains(end)) return true;

        return false;
    }
}
