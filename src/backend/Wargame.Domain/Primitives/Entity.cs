namespace Wargame.Domain.Primitives;

/// <summary>
/// Classe de base abstraite pour toutes les entités du domaine.
/// Fournit un identifiant unique (Guid) stable sur toute la durée de vie de l'objet.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; }

    protected Entity() : this(Guid.NewGuid()) { }

    protected Entity(Guid id)
    {
        Id = id;
    }

    // L'égalité entre entités est basée sur l'identité (Id), pas sur les valeurs.
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
