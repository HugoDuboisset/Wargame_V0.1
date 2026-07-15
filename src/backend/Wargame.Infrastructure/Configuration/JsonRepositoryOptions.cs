namespace Wargame.Infrastructure.Configuration;

/// <summary>
/// Configuration pour les repositories basés sur des fichiers JSON.
/// </summary>
public class JsonRepositoryOptions
{
    /// <summary>
    /// Chemin du dossier contenant les fichiers JSON (ex: "data").
    /// </summary>
    public string DataDirectory { get; set; } = "data";
}
