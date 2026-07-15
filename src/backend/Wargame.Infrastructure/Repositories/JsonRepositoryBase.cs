using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Wargame.Domain.Primitives;
using Wargame.Infrastructure.Configuration;
using Wargame.Infrastructure.Serialization;

namespace Wargame.Infrastructure.Repositories;

/// <summary>
/// Classe de base abstraite pour les repositories basés sur JSON.
/// Gère la lecture et l'écriture de listes complètes d'entités avec System.Text.Json.
/// </summary>
/// <typeparam name="T">Le type d'entité, héritant de Wargame.Domain.Primitives.Entity</typeparam>
public abstract class JsonRepositoryBase<T> where T : Entity
{
    protected readonly string FilePath;
    protected readonly JsonSerializerOptions SerializerOptions;

    protected JsonRepositoryBase(IOptions<JsonRepositoryOptions> options, string fileName)
    {
        var dataDir = options.Value.DataDirectory;
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        FilePath = Path.Combine(dataDir, fileName);

        SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { JsonPrivateResolver.SetPrivateSettersAndConstructors }
            }
        };
    }

    protected async Task<List<T>> LoadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        using var stream = File.OpenRead(FilePath);
        try
        {
            var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, cancellationToken);
            return items ?? [];
        }
        catch (JsonException)
        {
            // Retourne une liste vide en cas de fichier malformé ou vide
            return [];
        }
    }

    protected async Task SaveAllAsync(IEnumerable<T> items, CancellationToken cancellationToken)
    {
        using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken);
    }
}
