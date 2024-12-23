using IpaHosting.Components;

internal class Program
{
#pragma warning disable IDE1006 // Naming Styles
    private const string STORAGE_DIR = nameof(STORAGE_DIR);
    private const string SERVER_BASE_URL = nameof(SERVER_BASE_URL);
    private const string TOKEN = nameof(TOKEN);
#pragma warning restore IDE1006 // Naming Styles

    public static string StorageDir = Environment.GetEnvironmentVariable(STORAGE_DIR) ?? throw new Exception();
    public static string BaseAddress = Environment.GetEnvironmentVariable(SERVER_BASE_URL) ?? throw new Exception();
    public static string Token = Environment.GetEnvironmentVariable(TOKEN) ?? throw new Exception();

    public static long UploadMaxLength { get; } = 100 * 1024 * 1024;

    private static void Main(string[] args)
    {
        if (BaseAddress.EndsWith("/"))
        {
            throw new Exception($"{SERVER_BASE_URL} must not end with a '/'");
        }

        Console.WriteLine($"using {TOKEN} '{Token}'");

        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = UploadMaxLength;
        });

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