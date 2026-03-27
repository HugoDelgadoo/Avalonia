using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Biblioteca.Modelo;

namespace Biblioteca.Vista;

// ──────────────────────────────────────────────────────────
// Diálogo para añadir/editar un Libro
// ──────────────────────────────────────────────────────────
public class DialogoLibro : Window
{
    private readonly TextBox _titulo = new() { Watermark = "Título del libro" };
    private readonly NumericUpDown _anio = new() { Minimum = 1500, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year };
    private readonly DatePicker _fechaAdq = new() { SelectedDate = DateTime.Today };
    private readonly TextBox _isbn = new() { Watermark = "ISBN-10 (10 dígitos)" };
    private readonly CheckBox _prestado = new() { Content = "Está prestado" };
    private readonly Libro? _libroExistente;

    public DialogoLibro(Libro? libro = null)
    {
        _libroExistente = libro;
        Title = libro == null ? "Añadir Libro" : "Editar Libro";
        Width = 380; Height = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        // Prellenar si es edición
        if (libro != null)
        {
            _titulo.Text = libro.Titulo;
            _anio.Value = libro.Anio;
            _fechaAdq.SelectedDate = libro.FechaAdquisicion;
            _isbn.Text = libro.Isbn;
            _prestado.IsChecked = libro.Prestado;
        }

        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Título", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _titulo,
                new TextBlock { Text = "Año", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _anio,
                new TextBlock { Text = "Fecha de adquisición", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _fechaAdq,
                new TextBlock { Text = "ISBN-10", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _isbn,
                _prestado,
                ConstruirBotones()
            }
        };
    }

    private StackPanel ConstruirBotones()
    {
        var btnGuardar = new Button { Content = "Guardar", HorizontalAlignment = HorizontalAlignment.Right };
        var btnCancelar = new Button { Content = "Cancelar", HorizontalAlignment = HorizontalAlignment.Right };

        btnGuardar.Click += Guardar;
        btnCancelar.Click += (_, _) => Close(null);

        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { btnCancelar, btnGuardar } };
    }

    private async void Guardar(object? sender, RoutedEventArgs e)
    {
        try
        {
            var titulo = Articulo.FormatearTitulo(_titulo.Text ?? "");
            var anio = (int)(_anio.Value ?? DateTime.Now.Year);
            var isbn = _isbn.Text?.Trim() ?? "";
            var fecha = _fechaAdq.SelectedDate?.DateTime ?? DateTime.Today;

            if (string.IsNullOrWhiteSpace(titulo)) throw new Exception("El título es obligatorio.");
            if (!Articulo.ValidarAnio(anio)) throw new Exception($"El año debe estar entre 1500 y {DateTime.Now.Year}.");
            if (!Libro.ValidarIsbn(isbn)) throw new Exception("ISBN-10 inválido. Debe ser de 10 dígitos y pasar la validación matemática.");

            var libro = _libroExistente ?? new Libro();
            libro.Titulo = titulo;
            libro.Anio = anio;
            libro.FechaAdquisicion = fecha;
            libro.Isbn = isbn;
            libro.Prestado = _prestado.IsChecked ?? false;

            Close(libro);
        }
        catch (Exception ex)
        {
            await new DialogoMensaje("Error de validación", ex.Message).ShowDialog(this);
        }
    }
}

// ──────────────────────────────────────────────────────────
// Diálogo para añadir/editar un Audiolibro
// ──────────────────────────────────────────────────────────
public class DialogoAudiolibro : Window
{
    private readonly TextBox _titulo = new() { Watermark = "Título del audiolibro" };
    private readonly NumericUpDown _anio = new() { Minimum = 1500, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year };
    private readonly DatePicker _fechaAdq = new() { SelectedDate = DateTime.Today };
    private readonly DatePicker _fechaInicio = new() { SelectedDate = DateTime.Today };
    private readonly DatePicker _fechaFin = new() { SelectedDate = DateTime.Today.AddMonths(1) };
    private readonly Audiolibro? _audioExistente;

    public DialogoAudiolibro(Audiolibro? audio = null)
    {
        _audioExistente = audio;
        Title = audio == null ? "Añadir Audiolibro" : "Editar Audiolibro";
        Width = 380; Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        if (audio != null)
        {
            _titulo.Text = audio.Titulo;
            _anio.Value = audio.Anio;
            _fechaAdq.SelectedDate = audio.FechaAdquisicion;
            _fechaInicio.SelectedDate = audio.FechaInicioDisponibilidad;
            _fechaFin.SelectedDate = audio.FechaFinDisponibilidad;
        }

        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Título", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _titulo,
                new TextBlock { Text = "Año", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _anio,
                new TextBlock { Text = "Fecha de adquisición", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _fechaAdq,
                new TextBlock { Text = "Disponible desde", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _fechaInicio,
                new TextBlock { Text = "Disponible hasta", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _fechaFin,
                ConstruirBotones()
            }
        };
    }

    private StackPanel ConstruirBotones()
    {
        var btnGuardar = new Button { Content = "Guardar", HorizontalAlignment = HorizontalAlignment.Right };
        var btnCancelar = new Button { Content = "Cancelar" };
        btnGuardar.Click += Guardar;
        btnCancelar.Click += (_, _) => Close(null);
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right, Children = { btnCancelar, btnGuardar } };
    }

    private async void Guardar(object? sender, RoutedEventArgs e)
    {
        try
        {
            var titulo = Articulo.FormatearTitulo(_titulo.Text ?? "");
            var anio = (int)(_anio.Value ?? DateTime.Now.Year);
            var fecha = _fechaAdq.SelectedDate?.DateTime ?? DateTime.Today;
            var inicio = _fechaInicio.SelectedDate?.DateTime ?? DateTime.Today;
            var fin = _fechaFin.SelectedDate?.DateTime ?? DateTime.Today;

            if (string.IsNullOrWhiteSpace(titulo)) throw new Exception("El título es obligatorio.");
            if (!Articulo.ValidarAnio(anio)) throw new Exception($"El año debe estar entre 1500 y {DateTime.Now.Year}.");
            if (fin < inicio) throw new Exception("La fecha de fin no puede ser anterior a la de inicio.");

            var audio = _audioExistente ?? new Audiolibro();
            audio.Titulo = titulo;
            audio.Anio = anio;
            audio.FechaAdquisicion = fecha;
            audio.FechaInicioDisponibilidad = inicio;
            audio.FechaFinDisponibilidad = fin;

            Close(audio);
        }
        catch (Exception ex)
        {
            await new DialogoMensaje("Error de validación", ex.Message).ShowDialog(this);
        }
    }
}

// ──────────────────────────────────────────────────────────
// Diálogo para añadir una valoración
// ──────────────────────────────────────────────────────────
public class DialogoValoracion : Window
{
    private readonly NumericUpDown _nota = new() { Minimum = 0, Maximum = 10, Value = 5 };
    private readonly TextBox _usuario = new() { Watermark = "ID de usuario (obligatorio)" };
    private readonly TextBox _comentario = new() { Watermark = "Comentario (opcional)", Height = 60, AcceptsReturn = true };
    private readonly TextBox _palabras = new() { Watermark = "Palabras clave separadas por comas (opcional)" };

    public DialogoValoracion()
    {
        Title = "Añadir Valoración";
        Width = 380; Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var btnGuardar = new Button { Content = "Guardar", HorizontalAlignment = HorizontalAlignment.Right };
        var btnCancelar = new Button { Content = "Cancelar" };
        btnGuardar.Click += Guardar;
        btnCancelar.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Nota (0-10)*", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _nota,
                new TextBlock { Text = "Usuario*", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _usuario,
                new TextBlock { Text = "Comentario", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _comentario,
                new TextBlock { Text = "Palabras clave", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _palabras,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right, Children = { btnCancelar, btnGuardar } }
            }
        };
    }

    private async void Guardar(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_usuario.Text))
        {
            await new DialogoMensaje("Error", "El ID de usuario es obligatorio.").ShowDialog(this);
            return;
        }
        var nota = (int)(_nota.Value ?? 5);
        Close((nota, _usuario.Text.Trim(),
            string.IsNullOrWhiteSpace(_comentario.Text) ? null : _comentario.Text.Trim(),
            string.IsNullOrWhiteSpace(_palabras.Text) ? null : _palabras.Text.Trim()));
    }
}

// ──────────────────────────────────────────────────────────
// Diálogo para mostrar las valoraciones de un artículo
// ──────────────────────────────────────────────────────────
public class DialogoVerValoraciones : Window
{
    public DialogoVerValoraciones(string tituloArticulo, List<Valoracion> valoraciones)
    {
        Title = $"Valoraciones de: {tituloArticulo}";
        Width = 520; Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = valoraciones,
            Columns =
            {
                new DataGridTextColumn { Header = "Nota",     Binding = new Avalonia.Data.Binding("Nota"), Width = new DataGridLength(60) },
                new DataGridTextColumn { Header = "Usuario",  Binding = new Avalonia.Data.Binding("UsuarioId"), Width = new DataGridLength(120) },
                new DataGridTextColumn { Header = "Comentario", Binding = new Avalonia.Data.Binding("Comentario"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
                new DataGridTextColumn { Header = "Palabras", Binding = new Avalonia.Data.Binding("PalabrasClave"), Width = new DataGridLength(140) }
            }
        };

        var btnCerrar = new Button { Content = "Cerrar", HorizontalAlignment = HorizontalAlignment.Right };
        btnCerrar.Click += (_, _) => Close();

        Content = new DockPanel
        {
            Margin = new Thickness(12),
            Children =
            {
                new StackPanel { [DockPanel.DockProperty] = Dock.Bottom, Margin = new Thickness(0, 8, 0, 0), Children = { btnCerrar } },
                new TextBlock { [DockPanel.DockProperty] = Dock.Top,
                    Text = valoraciones.Count == 0 ? "Este artículo no tiene valoraciones." : $"{valoraciones.Count} valoración(es)",
                    Margin = new Thickness(0,0,0,8) },
                grid
            }
        };
    }
}

// ──────────────────────────────────────────────────────────
// Diálogo de confirmación Sí/No
// ──────────────────────────────────────────────────────────
public class DialogoConfirmar : Window
{
    public DialogoConfirmar(string mensaje)
    {
        Title = "Confirmar";
        Width = 320; Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var btnSi = new Button { Content = "Sí, eliminar", Background = Avalonia.Media.Brushes.IndianRed, Foreground = Avalonia.Media.Brushes.White };
        var btnNo = new Button { Content = "Cancelar" };
        btnSi.Click += (_, _) => Close(true);
        btnNo.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 16,
            Children =
            {
                new TextBlock { Text = mensaje, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right, Children = { btnNo, btnSi } }
            }
        };
    }
}

// ──────────────────────────────────────────────────────────
// Diálogo simple de mensaje informativo
// ──────────────────────────────────────────────────────────
public class DialogoMensaje : Window
{
    public DialogoMensaje(string titulo, string mensaje)
    {
        Title = titulo;
        Width = 340; Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var btn = new Button { Content = "Aceptar", HorizontalAlignment = HorizontalAlignment.Right };
        btn.Click += (_, _) => Close();

        Content = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 16,
            Children =
            {
                new TextBlock { Text = mensaje, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                btn
            }
        };
    }
}
