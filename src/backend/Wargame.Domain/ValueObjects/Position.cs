namespace Wargame.Domain.ValueObjects;

// L'utilisation d'un 'record' en C# rend l'objet immuable par défaut
// et gère automatiquement l'égalité (deux positions avec les mêmes X et Y sont égales).
public record Position(double X, double Y)
{
    //calcul entre deux positions
    public double DistanceTo(Position target)
    {
        var dx = X - target.X;
        var dy = Y - target.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}