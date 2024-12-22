using IpaHosting.Components;

internal class Program
{
    public static string PackagesDir => 
        OperatingSystem.IsWindows() ? "c:/temp/ipa" :
        OperatingSystem.IsLinux() ? "/var/www/ipa-server/ipa" :
        throw new NotSupportedException();

    public static string BaseAddress = Environment.GetEnvironmentVariable("SERVER_BASE_URL") ?? throw new Exception();

    public const string FileExtension = "sha256";

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();


        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapControllers();

        app.Run();
    }
}