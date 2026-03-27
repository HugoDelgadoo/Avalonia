using System;
using System.Collections.Generic;
using System.Linq;
using Biblioteca.Modelo;
using Biblioteca.Utilidades;

namespace Biblioteca.Controlador;

public class ControladorBiblioteca
{
    private readonly GestorBd _gestorBd;
    // Lista en memoria sincronizada con la BD
    public List<Articulo> Catalogo { get; private set; } = new();

    public ControladorBiblioteca(string rutaBd = "biblioteca.db")
    {
        _gestorBd = new GestorBd(rutaBd);
        CargarCatalogo();
    }

    // Carga todos los artículos desde la BD
    public void CargarCatalogo()
    {
        Catalogo = _gestorBd.ObtenerTodos();
    }

    // Añade un artículo y lo persiste en la BD
    public void AnadirArticulo(Articulo articulo)
    {
        _gestorBd.Insertar(articulo);
        Catalogo.Add(articulo);
    }

    // Edita un artículo existente en BD y en memoria
    public void EditarArticulo(Articulo articulo)
    {
        _gestorBd.Actualizar(articulo);
        var idx = Catalogo.FindIndex(a => a.Id == articulo.Id);
        if (idx >= 0) Catalogo[idx] = articulo;
    }

    // Elimina un artículo por Id
    public void EliminarArticulo(int id)
    {
        _gestorBd.Eliminar(id);
        Catalogo.RemoveAll(a => a.Id == id);
    }

    // Busca artículos por títulos y/o filtros opcionales
    public List<Articulo> BuscarItems(
        IEnumerable<string>? titulos = null,
        Dictionary<string, string>? filtros = null)
    {
        var resultado = Catalogo.AsEnumerable();

        // Filtrar por títulos (cualquiera que coincida)
        if (titulos != null && titulos.Any())
        {
            var listaTitulos = titulos.Select(t => t.ToLower()).ToList();
            resultado = resultado.Where(a =>
                listaTitulos.Any(t => a.Titulo.ToLower().Contains(t)));
        }

        // Aplicar filtros clave-valor
        if (filtros != null)
        {
            foreach (var (clave, valor) in filtros)
            {
                resultado = clave.ToLower() switch
                {
                    "tipo" => resultado.Where(a => a.TipoArticulo.Equals(valor, StringComparison.OrdinalIgnoreCase)),
                    "anio" => int.TryParse(valor, out var anio) ? resultado.Where(a => a.Anio == anio) : resultado,
                    "disponible" => resultado.Where(a => a is Audiolibro au && au.EstaDisponible == (valor == "true")),
                    "prestado" => resultado.Where(a => a is Libro l && l.Prestado == (valor == "true")),
                    _ => resultado
                };
            }
        }

        return resultado.ToList();
    }

    // Añade una valoración a un artículo valorable
    public void AnadirValoracion(int articuloId, int puntuacion, string usuarioId,
        string? comentario = null, string? palabrasClave = null)
    {
        var articulo = Catalogo.FirstOrDefault(a => a.Id == articuloId)
            ?? throw new Exception("Artículo no encontrado.");

        var valoracion = new Valoracion(puntuacion, usuarioId, comentario, palabrasClave);

        if (articulo is Libro libro)
            libro.Valoraciones.Add(valoracion);
        else if (articulo is Audiolibro audio)
            audio.Valoraciones.Add(valoracion);
        else
            throw new Exception("El artículo no es valorable.");

        _gestorBd.InsertarValoracion(articuloId, valoracion);
    }

    // Exporta el catálogo completo a CSV
    public void ExportarCsv(string ruta)
    {
        CsvManejador.ExportarCatalogo(Catalogo, ruta);
    }

    // Importa artículos desde CSV (solo añade los que no existan por título)
    public void ImportarCsv(string ruta)
    {
        var importados = CsvManejador.ImportarCatalogo(ruta);
        foreach (var art in importados)
        {
            if (!Catalogo.Any(a => a.Titulo == art.Titulo && a.TipoArticulo == art.TipoArticulo))
                AnadirArticulo(art);
        }
    }
}
