namespace Homologacion.Api.Contracts;

public sealed record ValidarRequisitosResponse(bool Cumple, IReadOnlyList<string> Incumplimientos);
