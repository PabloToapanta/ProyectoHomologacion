using Auth.Application.Login;
using Auth.Domain.Entities;

namespace Auth.Application.Abstractions;

public interface ITokenIssuer
{
    LoginResult Create(Usuario usuario);
}
