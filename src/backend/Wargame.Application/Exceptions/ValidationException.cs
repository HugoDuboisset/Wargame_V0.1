using FluentValidation.Results;

namespace Wargame.Application.Exceptions;

/// <summary>
/// Exception personnalisée levée lorsqu'une commande ou requête échoue
/// à la validation du pipeline MediatR via FluentValidation.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException() : base("Un ou plusieurs échecs de validation se sont produits.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
