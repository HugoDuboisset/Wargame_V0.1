using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry.Bases;

namespace Wargame.Domain.Factories;

/// <summary>
/// Builder fluent pour construire des objets Unit complexes avec leurs Figures.
/// </summary>
public class UnitBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Unnamed Unit";
    private UnitType _type = UnitType.Infantry;
    private UnitProfile _baseProfile = new UnitProfile(0, 0, 0, 0, 0, ArmorClass.Unarmored);
    private readonly List<Figure> _figures = new();

    public UnitBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UnitBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UnitBuilder WithType(UnitType type)
    {
        _type = type;
        return this;
    }

    public UnitBuilder WithBaseProfile(int movement, int shooting, int combat, int initiative, int morale, ArmorClass armorClass)
    {
        _baseProfile = new UnitProfile(movement, shooting, combat, initiative, morale, armorClass);
        return this;
    }

    public UnitBuilder WithBaseProfile(UnitProfile profile)
    {
        _baseProfile = profile;
        return this;
    }

    /// <summary>
    /// Ajoute une figurine à l'unité.
    /// </summary>
    public UnitBuilder AddFigure(Figure figure)
    {
        _figures.Add(figure);
        return this;
    }

    /// <summary>
    /// Crée et ajoute de multiples figurines basées sur le même modèle.
    /// Le constructeur de Figure prend typiquement l'ID, la santé (liée à la classe), et les armes.
    /// Pour simplifier, cette méthode construit des figures basiques qui pourront être équipées ensuite,
    /// ou passées directement équipées.
    /// </summary>
    public UnitBuilder AddFigures(int count, int hitPoints = 1, IBaseShape? baseShape = null)
    {
        var shape = baseShape ?? new CircularBase(12.5); // Par défaut : socle 25mm
        for (int i = 0; i < count; i++)
        {
            _figures.Add(new Figure(Guid.NewGuid(), hitPoints, shape, new Position(0, 0)));
        }
        return this;
    }

    /// <summary>
    /// Équipe toutes les figurines actuellement dans le builder avec l'arme à distance spécifiée.
    /// </summary>
    public UnitBuilder EquipAllWithRangedWeapon(Weapon weapon)
    {
        foreach (var figure in _figures)
        {
            figure.AddRangedWeapon(weapon);
        }
        return this;
    }

    /// <summary>
    /// Équipe toutes les figurines actuellement dans le builder avec l'arme de mêlée spécifiée.
    /// </summary>
    public UnitBuilder EquipAllWithMeleeWeapon(Weapon weapon)
    {
        foreach (var figure in _figures)
        {
            figure.AddMeleeWeapon(weapon);
        }
        return this;
    }

    /// <summary>
    /// Instancie la classe Unit finale avec la configuration actuelle.
    /// </summary>
    public Unit Build()
    {
        if (_figures.Count == 0)
        {
            throw new InvalidOperationException("Cannot build a unit without any figures.");
        }

        return new Unit(_id, _name, _type, _baseProfile, _figures);
    }
}
