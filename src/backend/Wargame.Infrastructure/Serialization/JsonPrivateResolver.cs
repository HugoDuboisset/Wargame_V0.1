using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Wargame.Infrastructure.Serialization;

/// <summary>
/// Permet à System.Text.Json de désérialiser des objets avec des constructeurs
/// sans paramètres privés et des propriétés à setters privés.
/// </summary>
public static class JsonPrivateResolver
{
    public static void SetPrivateSettersAndConstructors(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        // Permet l'instanciation via un constructeur privé sans paramètres
        if (jsonTypeInfo.CreateObject is null)
        {
            var ctor = jsonTypeInfo.Type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (ctor != null)
            {
                jsonTypeInfo.CreateObject = () => ctor.Invoke(null);
            }
        }

        // Permet l'affectation sur des setters privés
        foreach (var property in jsonTypeInfo.Properties)
        {
            if (property.Set is null && property.AttributeProvider is PropertyInfo propInfo)
            {
                var setter = propInfo.GetSetMethod(nonPublic: true);
                if (setter != null)
                {
                    property.Set = (obj, value) => setter.Invoke(obj, [value]);
                }
            }
        }
    }
}
