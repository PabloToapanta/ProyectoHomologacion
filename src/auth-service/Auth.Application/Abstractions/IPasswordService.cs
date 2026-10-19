using Auth.Domain.Entities;

namespace Auth.Application.Abstractions;

public enum PasswordVerification { Failed, Success, SuccessRehashNeeded }

public interface IPasswordService
{
    // El adaptador también verifica un hash sintético si no existe el usuario.
    PasswordVerification Verify(Usuario? usuario, string password);
    string Hash(Usuario usuario, string password);
}
