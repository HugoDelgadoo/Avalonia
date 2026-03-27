using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Biblioteca.Modelo;

namespace Biblioteca.Utilidades;

public static class CsvManejador
{
    private const string Cabecera = "Tipo,Titulo,Anio,FechaAdquisicion,Isbn,Prestado,FechaInicio,FechaFin";

    // Exporta la lista de artículos a un archivo CSV
    public static void ExportarCatalogo(IEnumerable<Articulo> catalogo, string ruta)
    {
        using var writer = new StreamWriter(ruta, false, System.Text.Encoding.UTF8);
        writer.WriteLine(Cabecera);

        foreach (var art in catalogo)
        {
            string linea = art switch
            {
                Libro l => $"Libro,\"{l.Titulo}\",{l.Anio},{l.FechaAdquisicion:yyyy-MM-dd},{l.Isbn},{l.Prestado},,",
                Audiolibro a => $"Audiolibro,\"{a.Titulo}\",{a.Anio},{a.FechaAdquisicion:yyyy-MM-dd},,," +
                                $"{a.FechaInicioDisponibilidad:yyyy-MM-dd},{a.FechaFinDisponibilidad:yyyy-MM-dd}",
                _ => ""
            };
            if (!string.IsNullOrEmpty(linea))
                writer.WriteLine(linea);
        }
    }

    // Importa artículos desde un archivo CSV
    public static List<Articulo> ImportarCatalogo(string ruta)
    {
        var lista = new List<Articulo>();
        if (!File.Exists(ruta)) return lista;

        var lineas = File.ReadAllLines(ruta, System.Text.Encoding.UTF8);

        for (int i = 1; i < lineas.Length; i++) // Salta la cabecera
        {
            try
            {
                var campos = ParsearLinea(lineas[i]);
                if (campos.Length < 8) continue;

                var tipo = campos[0].Trim();
                var titulo = campos[1].Trim();
                var anio = int.Parse(campos[2]);
                var fechaAdq = DateTime.Parse(campos[3]);

                if (tipo == "Libro")
                {
                    lista.Add(new Libro
                    {
                        Titulo = Articulo.FormatearTitulo(titulo),
                        Anio = anio,
                        FechaAdquisicion = fechaAdq,
                        Isbn = campos[4].Trim(),
                        Prestado = bool.TryParse(campos[5], out var prest) && prest
                    });
                }
                else if (tipo == "Audiolibro")
                {
                    lista.Add(new Audiolibro
                    {
                        Titulo = Articulo.FormatearTitulo(titulo),
                        Anio = anio,
                        FechaAdquisicion = fechaAdq,
                        FechaInicioDisponibilidad = DateTime.TryParse(campos[6], out var ini) ? ini : DateTime.Today,
                        FechaFinDisponibilidad = DateTime.TryParse(campos[7], out var fin) ? fin : DateTime.Today
                    });
                }
            }
            catch { /* Ignorar filas malformadas */ }
        }

        return lista;
    }

    // Divide una línea CSV respetando comillas
    private static string[] ParsearLinea(string linea)
    {
        var campos = new List<string>();
        bool enComillas = false;
        var campo = new System.Text.StringBuilder();

        foreach (char c in linea)
        {
            if (c == '"') { enComillas = !enComillas; continue; }
            if (c == ',' && !enComillas) { campos.Add(campo.ToString()); campo.Clear(); continue; }
            campo.Append(c);
        }
        campos.Add(campo.ToString());
        return campos.ToArray();
    }
}
