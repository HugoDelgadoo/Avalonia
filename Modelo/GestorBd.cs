using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Biblioteca.Modelo;

namespace Biblioteca.Modelo;

public class GestorBd
{
    private readonly string _rutaBd;

    public GestorBd(string rutaBd = "biblioteca.db")
    {
        _rutaBd = rutaBd;
        var conexion = CrearConexionBd(_rutaBd);
        InicializarBd(conexion);
        conexion.Close();
    }

    // Crea y retorna una conexión a la base de datos
    public static SqliteConnection CrearConexionBd(string ruta)
    {
        var conexion = new SqliteConnection($"Data Source={ruta}");
        conexion.Open();
        return conexion;
    }

    // Crea las tablas si no existen
    public static void InicializarBd(SqliteConnection conexion)
    {
        var cmd = conexion.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Articulos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Tipo TEXT NOT NULL,
                Titulo TEXT NOT NULL,
                Anio INTEGER NOT NULL,
                FechaAdquisicion TEXT NOT NULL,
                Isbn TEXT,
                Prestado INTEGER,
                FechaInicioDisponibilidad TEXT,
                FechaFinDisponibilidad TEXT
            );
            CREATE TABLE IF NOT EXISTS Valoraciones (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ArticuloId INTEGER NOT NULL,
                Nota INTEGER NOT NULL,
                UsuarioId TEXT NOT NULL,
                Comentario TEXT,
                PalabrasClave TEXT,
                FOREIGN KEY(ArticuloId) REFERENCES Articulos(Id)
            );";
        cmd.ExecuteNonQuery();
    }

    // Obtiene todos los artículos con sus valoraciones
    public List<Articulo> ObtenerTodos()
    {
        var lista = new List<Articulo>();
        using var con = CrearConexionBd(_rutaBd);

        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM Articulos";
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var articulo = LeerArticulo(reader);
            if (articulo != null) lista.Add(articulo);
        }

        // Cargar valoraciones para cada artículo
        foreach (var art in lista)
            CargarValoraciones(con, art);

        return lista;
    }

    // Inserta un artículo nuevo y devuelve su Id
    public int Insertar(Articulo articulo)
    {
        using var con = CrearConexionBd(_rutaBd);
        var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Articulos (Tipo, Titulo, Anio, FechaAdquisicion, Isbn, Prestado, FechaInicioDisponibilidad, FechaFinDisponibilidad)
            VALUES ($tipo, $titulo, $anio, $fecha, $isbn, $prestado, $inicio, $fin);
            SELECT last_insert_rowid();";
        AsignarParametros(cmd, articulo);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        articulo.Id = id;
        return id;
    }

    // Actualiza un artículo existente
    public void Actualizar(Articulo articulo)
    {
        using var con = CrearConexionBd(_rutaBd);
        var cmd = con.CreateCommand();
        cmd.CommandText = @"
            UPDATE Articulos SET Titulo=$titulo, Anio=$anio, FechaAdquisicion=$fecha,
            Isbn=$isbn, Prestado=$prestado, FechaInicioDisponibilidad=$inicio, FechaFinDisponibilidad=$fin
            WHERE Id=$id";
        AsignarParametros(cmd, articulo);
        cmd.Parameters.AddWithValue("$id", articulo.Id);
        cmd.ExecuteNonQuery();
    }

    // Elimina un artículo y sus valoraciones
    public void Eliminar(int id)
    {
        using var con = CrearConexionBd(_rutaBd);
        var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM Valoraciones WHERE ArticuloId=$id; DELETE FROM Articulos WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // Inserta una valoración para un artículo
    public void InsertarValoracion(int articuloId, Valoracion v)
    {
        using var con = CrearConexionBd(_rutaBd);
        var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Valoraciones (ArticuloId, Nota, UsuarioId, Comentario, PalabrasClave)
            VALUES ($artId, $nota, $usuario, $comentario, $palabras);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$artId", articuloId);
        cmd.Parameters.AddWithValue("$nota", v.Nota);
        cmd.Parameters.AddWithValue("$usuario", v.UsuarioId);
        cmd.Parameters.AddWithValue("$comentario", (object?)v.Comentario ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$palabras", (object?)v.PalabrasClave ?? DBNull.Value);
        v.Id = Convert.ToInt32(cmd.ExecuteScalar());
        v.ArticuloId = articuloId;
    }

    // Lee las valoraciones de un artículo desde la BD
    private static void CargarValoraciones(SqliteConnection con, Articulo art)
    {
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM Valoraciones WHERE ArticuloId=$id";
        cmd.Parameters.AddWithValue("$id", art.Id);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var val = new Valoracion
            {
                Id = r.GetInt32(0),
                ArticuloId = r.GetInt32(1),
                Nota = r.GetInt32(2),
                UsuarioId = r.GetString(3),
                Comentario = r.IsDBNull(4) ? null : r.GetString(4),
                PalabrasClave = r.IsDBNull(5) ? null : r.GetString(5)
            };
            if (art is Libro l) l.Valoraciones.Add(val);
            else if (art is Audiolibro a) a.Valoraciones.Add(val);
        }
    }

    // Convierte una fila del reader en un objeto Articulo
    private static Articulo? LeerArticulo(SqliteDataReader r)
    {
        var tipo = r.GetString(1);
        var id = r.GetInt32(0);
        var titulo = r.GetString(2);
        var anio = r.GetInt32(3);
        var fecha = DateTime.Parse(r.GetString(4));

        if (tipo == "Libro")
        {
            return new Libro
            {
                Id = id, Titulo = titulo, Anio = anio, FechaAdquisicion = fecha,
                Isbn = r.IsDBNull(5) ? "" : r.GetString(5),
                Prestado = !r.IsDBNull(6) && r.GetInt32(6) == 1
            };
        }
        else if (tipo == "Audiolibro")
        {
            return new Audiolibro
            {
                Id = id, Titulo = titulo, Anio = anio, FechaAdquisicion = fecha,
                FechaInicioDisponibilidad = r.IsDBNull(7) ? DateTime.Today : DateTime.Parse(r.GetString(7)),
                FechaFinDisponibilidad = r.IsDBNull(8) ? DateTime.Today : DateTime.Parse(r.GetString(8))
            };
        }
        return null;
    }

    // Asigna los parámetros comunes al comando SQL
    private static void AsignarParametros(SqliteCommand cmd, Articulo articulo)
    {
        cmd.Parameters.AddWithValue("$tipo", articulo.TipoArticulo);
        cmd.Parameters.AddWithValue("$titulo", articulo.Titulo);
        cmd.Parameters.AddWithValue("$anio", articulo.Anio);
        cmd.Parameters.AddWithValue("$fecha", articulo.FechaAdquisicion.ToString("yyyy-MM-dd"));

        if (articulo is Libro libro)
        {
            cmd.Parameters.AddWithValue("$isbn", libro.Isbn);
            cmd.Parameters.AddWithValue("$prestado", libro.Prestado ? 1 : 0);
            cmd.Parameters.AddWithValue("$inicio", DBNull.Value);
            cmd.Parameters.AddWithValue("$fin", DBNull.Value);
        }
        else if (articulo is Audiolibro audio)
        {
            cmd.Parameters.AddWithValue("$isbn", DBNull.Value);
            cmd.Parameters.AddWithValue("$prestado", DBNull.Value);
            cmd.Parameters.AddWithValue("$inicio", audio.FechaInicioDisponibilidad.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fin", audio.FechaFinDisponibilidad.ToString("yyyy-MM-dd"));
        }
    }
}
