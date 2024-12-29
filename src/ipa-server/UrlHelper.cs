namespace IpaHosting;

public static class MyUrlHelper
{
    public static Uri AppIconUrl(Sha256 id)
    {
        return new Uri($"/{MyRoutes.download}/{id.Value}.display-image.png", UriKind.Relative);
    }
}
