using Auth.Application.Abstractions;
using Auth.Application.Login;
using Auth.Domain.Entities;
using Xunit;

namespace Auth.Application.Tests;

public sealed class LoginServiceTests
{
    private readonly UserRepositoryStub users = new();
    private readonly PasswordServiceStub passwords = new();
    private readonly TokenIssuerStub tokens = new();
    private LoginService Service() => new(users, passwords, tokens);

    [Fact]
    public async Task Login_NormalizaNombreConservaPasswordYDevuelveTokenDelPuerto()
    {
        var result = await Service().LoginAsync(new LoginCommand(" ADMIN ", " clave con espacios "));
        Assert.Same(tokens.Result, result);
        Assert.Equal("ADMIN", users.LastName);
        Assert.Equal(" clave con espacios ", passwords.LastPassword);
        Assert.Same(users.User, passwords.LastUser);
        Assert.Same(users.User, tokens.LastUser);
        Assert.Equal(0, users.Updates);
    }

    [Fact]
    public async Task UsuarioInexistente_VerificaHashSinteticoSinEmitirToken()
    {
        users.User = null;
        Assert.Null(await Service().LoginAsync(new LoginCommand("desconocido", "clave")));
        Assert.Equal(1, passwords.Verifications);
        Assert.Null(passwords.LastUser);
        Assert.Equal(0, tokens.Calls);
        Assert.Equal(0, users.Updates);
    }

    [Theory]
    [InlineData(PasswordVerification.Failed, true)]
    [InlineData(PasswordVerification.Success, false)]
    [InlineData(PasswordVerification.SuccessRehashNeeded, false)]
    public async Task PasswordInvalidoOUsuarioInactivo_NoActualizaHashNiEmiteToken(PasswordVerification verification, bool active)
    {
        passwords.Verification = verification;
        users.User!.Activo = active;
        Assert.Null(await Service().LoginAsync(new LoginCommand("admin", "clave")));
        Assert.Equal(1, passwords.Verifications);
        Assert.Equal(0, passwords.Hashes);
        Assert.Equal(0, users.Updates);
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task HashAntiguo_SePersisteAntesDeEmitirToken()
    {
        passwords.Verification = PasswordVerification.SuccessRehashNeeded;
        Assert.NotNull(await Service().LoginAsync(new LoginCommand("admin", "clave")));
        Assert.Equal(1, passwords.Hashes);
        Assert.Equal(1, users.Updates);
        Assert.Equal("hash-actualizado", tokens.HashAtIssuance);
    }

    public static TheoryData<string?, string?> InvalidCredentials => new()
    {
        { null, "clave" }, { "admin", null }, { " ", "clave" }, { "admin", " " },
        { new string('a', 257), "clave" }, { "admin", new string('a', 1025) }
    };

    [Theory]
    [MemberData(nameof(InvalidCredentials))]
    public async Task EntradaInvalida_NoConsultaUsuariosNiInvocaSeguridad(string? name, string? password)
    {
        Assert.Null(await Service().LoginAsync(new LoginCommand(name!, password!)));
        Assert.Null(users.LastName);
        Assert.Equal(0, passwords.Verifications);
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task Cancelacion_SePropagaAlRepositorioEnConsultaYActualizacion()
    {
        using var cancellation = new CancellationTokenSource();
        passwords.Verification = PasswordVerification.SuccessRehashNeeded;
        await Service().LoginAsync(new LoginCommand("admin", "clave"), cancellation.Token);
        Assert.Equal(cancellation.Token, users.QueryToken);
        Assert.Equal(cancellation.Token, users.UpdateToken);
    }

    private sealed class UserRepositoryStub : IUserRepository
    {
        public Usuario? User { get; set; } = new()
        { NombreUsuario = "admin", NombreNormalizado = "ADMIN", PasswordHash = "hash-anterior", Rol = new Rol { Nombre = "Administrador" } };
        public string? LastName { get; private set; }
        public int Updates { get; private set; }
        public CancellationToken QueryToken { get; private set; }
        public CancellationToken UpdateToken { get; private set; }
        public Task<Usuario?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            LastName = normalizedName;
            QueryToken = cancellationToken;
            return Task.FromResult(User);
        }
        public Task UpdatePasswordHashAsync(Usuario user, string passwordHash, CancellationToken cancellationToken = default)
        {
            user.PasswordHash = passwordHash;
            Updates++;
            UpdateToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class PasswordServiceStub : IPasswordService
    {
        public PasswordVerification Verification { get; set; } = PasswordVerification.Success;
        public int Verifications { get; private set; }
        public int Hashes { get; private set; }
        public Usuario? LastUser { get; private set; }
        public string? LastPassword { get; private set; }
        public PasswordVerification Verify(Usuario? usuario, string password)
        {
            Verifications++;
            LastUser = usuario;
            LastPassword = password;
            return Verification;
        }
        public string Hash(Usuario usuario, string password) { Hashes++; return "hash-actualizado"; }
    }

    private sealed class TokenIssuerStub : ITokenIssuer
    {
        public LoginResult Result { get; } = new("token-sintetico", "Bearer", DateTimeOffset.UnixEpoch);
        public int Calls { get; private set; }
        public Usuario? LastUser { get; private set; }
        public string? HashAtIssuance { get; private set; }
        public LoginResult Create(Usuario usuario)
        {
            Calls++;
            LastUser = usuario;
            HashAtIssuance = usuario.PasswordHash;
            return Result;
        }
    }
}
