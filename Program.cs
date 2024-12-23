using IpaHosting.Components;

internal class Program
{
#pragma warning disable IDE1006 // Naming Styles
    private const string SERVER_BASE_URL = nameof(SERVER_BASE_URL);
#pragma warning restore IDE1006 // Naming Styles

    public static string StorageDir = Environment.GetEnvironmentVariable("STORAGE_DIR") ?? throw new Exception();
    public static string BaseAddress = Environment.GetEnvironmentVariable("SERVER_BASE_URL") ?? throw new Exception();

    public const string FileExtension = "sha256";

    private static void Main(string[] args)
    {
        if (BaseAddress.EndsWith("/"))
        {
            throw new Exception($"{SERVER_BASE_URL} must not end with a '/'");
        }

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