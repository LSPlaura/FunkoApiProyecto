using FunkoApi.Back.Models;

namespace FunkoApi.Tests;

internal static class TestData
{
    public static DateTime Created => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Categoria Categoria(int id = 1, string nombre = "Marvel")
    {
        return new Categoria(id, nombre, false, Created, Created);
    }

    public static Proveedor Proveedor(int id = 1, string nombre = "Funko Corp")
    {
        return new Proveedor(id, nombre, false, Created, Created);
    }

    public static Funko Funko(int id = 1, string nombre = "Iron Man", Categoria? categoria = null)
    {
        return new(id, nombre, 14.99m, 10, categoria?.Id ?? 1, categoria ?? Categoria(), Proveedor(), false, Created,
            Created);
    }
}