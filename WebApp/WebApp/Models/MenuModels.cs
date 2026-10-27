namespace WebApp.Models;

public record MenuItem(string Name, decimal Price, string Description);

public static class Menu
{
    public static readonly IReadOnlyList<MenuItem> Items = new List<MenuItem>
    {
        new("Classic Beans", 4.50m, "Slow-cooked pinto beans, house seasoning."),
        new("Spicy Beans", 5.00m, "Classic Beans with a jalapeno kick."),
        new("Bean Burrito", 6.50m, "Beans, rice, and cheese wrapped up."),
        new("Bean Chili", 5.50m, "Hearty three-bean chili."),
        new("Rotten Beans", 0.50m, "Do not order — deterministically fails payment (demo/testing item, architecture plan Q8)."),
    };
}
