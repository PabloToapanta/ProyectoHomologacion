using Homologacion.Domain.Auditoria;
using Xunit;

namespace Homologacion.Domain.Tests;

/// <summary>HU-25: eventos del informe (generado, remitido, finalizado) con fecha y usuario, en orden cronológico.</summary>
public class HistorialInformeTests
{
    private static readonly Guid Solicitud = Guid.Parse("25000000-0000-0000-0000-000000000001");
    private static readonly Guid Usuario = Guid.Parse("25000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset T0 = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    private static EventoInforme Evento(TipoEventoInforme tipo, DateTimeOffset fecha, string rol = "Docente") =>
        HistorialInforme.Crear(Solicitud, tipo, fecha, Usuario, "docente.ana", rol);

    [Fact]
    public void Crea_el_evento_con_fecha_usuario_y_rol()
    {
        var e = HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, T0, Usuario, "  docente.ana  ", "Docente");
        Assert.NotEqual(Guid.Empty, e.Id);
        Assert.Equal((Solicitud, TipoEventoInforme.Generado, T0, Usuario, "docente.ana", "Docente"),
            (e.SolicitudId, e.Tipo, e.Fecha, e.UsuarioId, e.Usuario, e.Rol));
    }

    [Fact]
    public void Sin_nombre_usa_el_identificador_y_un_nombre_largo_se_recorta()
    {
        Assert.Equal(Usuario.ToString(), HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, T0, Usuario, "  ", "Docente").Usuario);
        Assert.Equal(HistorialInforme.MaxUsuario,
            HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, T0, Usuario, new string('x', 300), "Docente").Usuario.Length);
    }

    [Fact]
    public void Datos_obligatorios_ausentes_o_tipo_desconocido_se_rechazan()
    {
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.Crear(Guid.Empty, TipoEventoInforme.Generado, T0, Usuario, "a", "Docente"));
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, T0, Guid.Empty, "a", "Docente"));
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.Crear(Solicitud, (TipoEventoInforme)9, T0, Usuario, "a", "Docente"));
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, T0, Usuario, "a", " "));
    }

    [Fact]
    public void Ordena_cronologicamente_y_a_igual_fecha_sigue_el_flujo()
    {
        var finalizado = Evento(TipoEventoInforme.Finalizado, T0.AddHours(2), "Coordinador");
        var remitido = Evento(TipoEventoInforme.Remitido, T0);
        var generado = Evento(TipoEventoInforme.Generado, T0);
        Assert.Equal([TipoEventoInforme.Generado, TipoEventoInforme.Remitido, TipoEventoInforme.Finalizado],
            HistorialInforme.Ordenar([finalizado, remitido, generado]).Select(e => e.Tipo));
    }

    [Fact]
    public void Cada_evento_ocurre_una_vez_y_en_el_orden_del_flujo()
    {
        HistorialInforme.ValidarSiguiente([], TipoEventoInforme.Generado);
        var generado = Evento(TipoEventoInforme.Generado, T0);
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.ValidarSiguiente([], TipoEventoInforme.Remitido));
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.ValidarSiguiente([generado], TipoEventoInforme.Generado));
        Assert.Throws<EventoInformeInvalidoException>(() => HistorialInforme.ValidarSiguiente([generado], TipoEventoInforme.Finalizado));
        HistorialInforme.ValidarSiguiente([generado], TipoEventoInforme.Remitido);
        var remitido = Evento(TipoEventoInforme.Remitido, T0.AddHours(1));
        HistorialInforme.ValidarSiguiente([generado, remitido], TipoEventoInforme.Finalizado);
        Assert.Throws<EventoInformeInvalidoException>(() =>
            HistorialInforme.ValidarSiguiente([generado, remitido, Evento(TipoEventoInforme.Finalizado, T0.AddHours(2))], TipoEventoInforme.Finalizado));
    }
}
