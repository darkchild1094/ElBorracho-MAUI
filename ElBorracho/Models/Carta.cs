namespace ElBorracho.Models;

public class Carta
{
    public int Numero { get; init; }
    public string Imagen { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;

    public Carta(int numero, string imagen, string nombre)
    {
        Numero = numero;
        Imagen = imagen;
        Nombre = nombre;
    }
}
