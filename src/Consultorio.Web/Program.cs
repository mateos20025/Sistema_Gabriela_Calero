using System.Globalization;
using Consultorio.Application;
using Consultorio.Infrastructure;
using Consultorio.Infrastructure.Identidad;
using Consultorio.Infrastructure.Persistencia;
using Consultorio.Web.Components;
using Consultorio.Web.Seguridad;
using MudBlazor.Services;
using Serilog;

// Logger mínimo para registrar errores que ocurran incluso antes de leer la configuración.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    // Fechas, números y textos en formato de Ecuador.
    var culturaEcuador = CultureInfo.GetCultureInfo("es-EC");
    CultureInfo.DefaultThreadCurrentCulture = culturaEcuador;
    CultureInfo.DefaultThreadCurrentUICulture = culturaEcuador;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((servicios, configuracion) => configuracion
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(servicios));

    // No anunciar qué servidor web usamos.
    builder.WebHost.ConfigureKestrel(opciones => opciones.AddServerHeader = false);

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices(opciones =>
        opciones.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight);

    builder.Services.AddAplicacion();
    builder.Services.AddInfraestructura(builder.Configuration);
    builder.Services.AddSeguridadWeb(builder.Configuration);

    var app = builder.Build();

    await app.Services.InicializarBaseDatosAsync();

    // Comando de emergencia (desde la consola del servidor, no desde la web):
    //   dotnet Consultorio.Web.dll restablecer-password correo@ejemplo.com
    if (args is ["restablecer-password", var correo])
    {
        var passwordTemporal = await app.Services.RestablecerPasswordDesdeConsolaAsync(correo);
        Console.WriteLine($"Contraseña temporal para {correo}: {passwordTemporal}");
        Console.WriteLine("Se pedirá cambiarla en el primer inicio de sesión.");
        return;
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/no-encontrado", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseCabecerasDeSeguridad();
    app.UseSerilogRequestLogging();

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        // Blazor agrega su propia cabecera "frame-ancestors"; se alinea con la nuestra (nadie puede incrustar el sitio).
        .AddInteractiveServerRenderMode(opciones => opciones.ContentSecurityFrameAncestorsPolicy = "'none'");
    app.MapEndpointsCuenta();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException es normal: la lanza "dotnet ef" al crear migraciones.
    Log.Fatal(ex, "La aplicación se detuvo por un error inesperado");
}
finally
{
    await Log.CloseAndFlushAsync();
}
