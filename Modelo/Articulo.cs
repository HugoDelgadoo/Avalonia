using System;
using System.Collections.Generic;
using System.Linq;

namespace Biblioteca.Modelo;

// Clase abstracta base para todos los artículos
public abstract class Articulo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public int Anio { get; set; }
    public DateTime FechaAdquisicion { get; set; }
    public string TipoArticulo => GetType().Name;

    protected Articulo() { }

    protected Articulo(string titulo, int anio, DateTime fechaAdquisicion)
    {
        Titulo = FormatearTitulo(titulo);
        Anio = anio;
        FechaAdquisicion = fechaAdquisicion;
    }

    // Formatea título: primera letra mayúscula y sin espacios al inicio/fin
    public static string FormatearTitulo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        texto = texto.Trim();
        return char.ToUpper(texto[0]) + texto[1..];
    }

    // Valida que el año esté entre 1500 y el año actual
    public static bool ValidarAnio(int anio) =>
        anio >= 1500 && anio <= DateTime.Now.Year;
}

// Interfaz para artículos que se pueden valorar
public interface IValorable
{
    List<Valoracion> Valoraciones { get; }
    double ValoracionMedia => Valoraciones.Count == 0 ? 0 : Valoraciones.Average(v => v.Nota);
    void AnadirValoracion(int puntuacion, string usuarioId, string? comentario = null, string? palabrasClave = null);
}

// Interfaz para artículos prestables
public interface IPrestable
{
    static int DiasMaximoPrestamo => 31; // Atributo compartido por todos los prestables
    bool Prestado { get; set; }
}

// Valoración de un artículo
public class Valoracion
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public int Nota { get; set; }           // Obligatoria, entre 0 y 10
    public string? Comentario { get; set; } // Opcional
    public string? PalabrasClave { get; set; } // Opcional
    public string UsuarioId { get; set; } = "";

    public Valoracion() { }

    public Valoracion(int nota, string usuarioId, string? comentario = null, string? palabrasClave = null)
    {
        if (nota < 0 || nota > 10)
            throw new ArgumentException("La nota debe estar entre 0 y 10.");
        Nota = nota;
        UsuarioId = usuarioId;
        Comentario = comentario;
        PalabrasClave = palabrasClave;
    }
}

// Libro: prestable y valorable
public class Libro : Articulo, IPrestable, IValorable
{
    public string Isbn { get; set; } = "";
    public bool Prestado { get; set; }
    public List<Valoracion> Valoraciones { get; set; } = new();

    public Libro() { }

    public Libro(string titulo, int anio, DateTime fechaAdquisicion, string isbn)
        : base(titulo, anio, fechaAdquisicion)
    {
        if (!ValidarIsbn(isbn))
            throw new ArgumentException("ISBN-10 inválido.");
        Isbn = isbn;
    }

    public void AnadirValoracion(int puntuacion, string usuarioId, string? comentario = null, string? palabrasClave = null)
    {
        Valoraciones.Add(new Valoracion(puntuacion, usuarioId, comentario, palabrasClave));
    }

    // Valida ISBN-10: suma de dígito*peso (10..1) debe ser múltiplo de 11
    public static bool ValidarIsbn(string isbn)
    {
        if (isbn.Length != 10 || !isbn.All(char.IsDigit)) return false;
        int suma = isbn.Select((c, i) => (c - '0') * (10 - i)).Sum();
        return suma % 11 == 0;
    }

    public double ValoracionMedia => ((IValorable)this).ValoracionMedia;
}

// Audiolibro: valorable, no prestable, con fechas de disponibilidad
public class Audiolibro : Articulo, IValorable
{
    public DateTime FechaInicioDisponibilidad { get; set; }
    public DateTime FechaFinDisponibilidad { get; set; }
    public List<Valoracion> Valoraciones { get; set; } = new();

    // Disponible si la fecha actual está en el rango
    public bool EstaDisponible =>
        DateTime.Today >= FechaInicioDisponibilidad && DateTime.Today <= FechaFinDisponibilidad;

    public Audiolibro() { }

    public Audiolibro(string titulo, int anio, DateTime fechaAdquisicion,
        DateTime fechaInicio, DateTime fechaFin)
        : base(titulo, anio, fechaAdquisicion)
    {
        FechaInicioDisponibilidad = fechaInicio;
        FechaFinDisponibilidad = fechaFin;
    }

    public void AnadirValoracion(int puntuacion, string usuarioId, string? comentario = null, string? palabrasClave = null)
    {
        Valoraciones.Add(new Valoracion(puntuacion, usuarioId, comentario, palabrasClave));
    }

    public double ValoracionMedia => ((IValorable)this).ValoracionMedia;
}
