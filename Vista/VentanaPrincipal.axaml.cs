using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Biblioteca.Controlador;
using Biblioteca.Modelo;

namespace Biblioteca.Vista;

// DTO para mostrar datos en la tabla
public class ArticuloVm
{
    public int Id { get; set; }
    public string TipoArticulo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public int Anio { get; set; }
    public DateTime FechaAdquisicion { get; set; }
    public string ResumenExtra { get; set; } = "";
    public string EstadoTexto { get; set; } = "";
    public string MediaTexto { get; set; } = "";
    public Articulo Origen { get; set; } = null!;

    public static ArticuloVm Desde(Articulo a) => a switch
    {
        Libro l => new ArticuloVm
        {
            Id = l.Id, TipoArticulo = "Libro", Titulo = l.Titulo, Anio = l.Anio,
            FechaAdquisicion = l.FechaAdquisicion,
            ResumenExtra = l.Isbn,
            EstadoTexto = l.Prestado ? "Prestado" : "Disponible",
            MediaTexto = l.Valoraciones.Count > 0 ? $"{l.ValoracionMedia:F1} ⭐" : "-",
            Origen = l
        },
        Audiolibro au => new ArticuloVm
        {
            Id = au.Id, TipoArticulo = "Audiolibro", Titulo = au.Titulo, Anio = au.Anio,
            FechaAdquisicion = au.FechaAdquisicion,
            ResumenExtra = au.EstaDisponible ? "Disponible" : "No disponible",
            EstadoTexto = $"{au.FechaInicioDisponibilidad:dd/MM/yy}-{au.FechaFinDisponibilidad:dd/MM/yy}",
            MediaTexto = au.Valoraciones.Count > 0 ? $"{au.ValoracionMedia:F1} *" : "-",
            Origen = au
        },
        _ => new ArticuloVm { Origen = a }
    };
}

public partial class VentanaPrincipal : Window
{
    private readonly ControladorBiblioteca _ctrl;
    private readonly DataGrid _grilla;
    private readonly TextBox _txtBuscar;
    private readonly ComboBox _cmbTipo;

    public VentanaPrincipal()
    {
        InitializeComponent();
        _ctrl = new ControladorBiblioteca();

        // Barra de búsqueda
        _txtBuscar = new TextBox { Watermark = "Buscar por título...", Width = 220 };
        _txtBuscar.TextChanged += (_, _) => AplicarFiltros();

        _cmbTipo = new ComboBox { Width = 130 };
        _cmbTipo.Items.Add("Todos");
        _cmbTipo.Items.Add("Libro");
        _cmbTipo.Items.Add("Audiolibro");
        _cmbTipo.SelectedIndex = 0;
        _cmbTipo.SelectionChanged += (_, _) => AplicarFiltros();

        var barraBusqueda = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8),
            Children =
            {
                new TextBlock { Text = "Mi Biblioteca", FontSize = 22, FontWeight = FontWeight.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = SolidColorBrush.Parse("#1A237E") },
                _txtBuscar, _cmbTipo,
                Boton("Limpiar", "#546E7A", (_, _) => { _txtBuscar.Text = ""; _cmbTipo.SelectedIndex = 0; })
            }
        };

        // Barra de acciones — nuevo orden: CSV, añadir, editar/borrar, valoraciones
        var barraAcciones = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        barraAcciones.Children.Add(Boton("Importar CSV",        "#00838F", BtnImportarCsv_Click));
        barraAcciones.Children.Add(Boton("Exportar CSV",        "#00695C", BtnExportarCsv_Click));
        barraAcciones.Children.Add(Boton("+ Libro",             "#1565C0", BtnAnadirLibro_Click));
        barraAcciones.Children.Add(Boton("+ Audiolibro",        "#6A1B9A", BtnAnadirAudio_Click));
        barraAcciones.Children.Add(Boton("Editar",              "#E65100", BtnEditar_Click));
        barraAcciones.Children.Add(Boton("Eliminar",            "#B71C1C", BtnEliminar_Click));
        barraAcciones.Children.Add(Boton("Valorar",             "#2E7D32", BtnValoracion_Click));
        barraAcciones.Children.Add(Boton("Ver Valoraciones",    "#37474F", BtnVerValoraciones_Click));

        // Tabla principal
        _grilla = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            CanUserResizeColumns = true,
            SelectionMode = DataGridSelectionMode.Single
        };
        _grilla.Columns.Add(new DataGridTextColumn { Header = "ID",          Binding = new Avalonia.Data.Binding("Id"),              Width = new DataGridLength(50) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Tipo",        Binding = new Avalonia.Data.Binding("TipoArticulo"),    Width = new DataGridLength(100) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Título",      Binding = new Avalonia.Data.Binding("Titulo"),          Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Año",         Binding = new Avalonia.Data.Binding("Anio"),            Width = new DataGridLength(60) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Adquisición", Binding = new Avalonia.Data.Binding("FechaAdquisicion") { StringFormat = "{0:dd/MM/yyyy}" }, Width = new DataGridLength(110) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "ISBN / Disp.",Binding = new Avalonia.Data.Binding("ResumenExtra"),    Width = new DataGridLength(160) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Estado",      Binding = new Avalonia.Data.Binding("EstadoTexto"),     Width = new DataGridLength(130) });
        _grilla.Columns.Add(new DataGridTextColumn { Header = "Val. Media",  Binding = new Avalonia.Data.Binding("MediaTexto"),      Width = new DataGridLength(90) });

        // Layout principal
        var panel = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(barraBusqueda, Dock.Top);
        DockPanel.SetDock(barraAcciones, Dock.Top);
        panel.Children.Add(barraBusqueda);
        panel.Children.Add(barraAcciones);
        panel.Children.Add(_grilla);

        Content = panel;
        RefrescarTabla();
    }

    private static Button Boton(string texto, string? colorHex, EventHandler<RoutedEventArgs> click)
    {
        var btn = new Button { Content = texto, Margin = new Thickness(0, 0, 6, 6) };
        if (colorHex != null)
        {
            btn.Background = SolidColorBrush.Parse(colorHex);
            btn.Foreground = Brushes.White;
        }
        btn.Click += click;
        return btn;
    }

    private void RefrescarTabla(IEnumerable<Articulo>? lista = null)
    {
        _grilla.ItemsSource = (lista ?? _ctrl.Catalogo).Select(ArticuloVm.Desde).ToList();
    }

    private Articulo? ArticuloSeleccionado() => (_grilla.SelectedItem as ArticuloVm)?.Origen;

    private void AplicarFiltros()
    {
        var texto = _txtBuscar.Text ?? "";
        var tipo = _cmbTipo.SelectedItem?.ToString() ?? "Todos";
        var titulos = string.IsNullOrWhiteSpace(texto) ? null : new[] { texto };
        var filtros = tipo == "Todos" ? null : new Dictionary<string, string> { { "tipo", tipo } };
        RefrescarTabla(_ctrl.BuscarItems(titulos, filtros));
    }

    private async void BtnAnadirLibro_Click(object? sender, RoutedEventArgs e)
    {
        var res = await new DialogoLibro().ShowDialog<Libro?>(this);
        if (res != null)
            try { _ctrl.AnadirArticulo(res); RefrescarTabla(); }
            catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }

    private async void BtnAnadirAudio_Click(object? sender, RoutedEventArgs e)
    {
        var res = await new DialogoAudiolibro().ShowDialog<Audiolibro?>(this);
        if (res != null)
            try { _ctrl.AnadirArticulo(res); RefrescarTabla(); }
            catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }

    private async void BtnEditar_Click(object? sender, RoutedEventArgs e)
    {
        var art = ArticuloSeleccionado();
        if (art == null) { await new DialogoMensaje("Aviso", "Selecciona un artículo.").ShowDialog(this); return; }
        try
        {
            if (art is Libro l)
            {
                var res = await new DialogoLibro(l).ShowDialog<Libro?>(this);
                if (res != null) { _ctrl.EditarArticulo(res); RefrescarTabla(); }
            }
            else if (art is Audiolibro a)
            {
                var res = await new DialogoAudiolibro(a).ShowDialog<Audiolibro?>(this);
                if (res != null) { _ctrl.EditarArticulo(res); RefrescarTabla(); }
            }
        }
        catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }

    private async void BtnEliminar_Click(object? sender, RoutedEventArgs e)
    {
        var art = ArticuloSeleccionado();
        if (art == null) { await new DialogoMensaje("Aviso", "Selecciona un artículo.").ShowDialog(this); return; }
        var ok = await new DialogoConfirmar($"¿Eliminar '{art.Titulo}'?").ShowDialog<bool>(this);
        if (ok) { _ctrl.EliminarArticulo(art.Id); RefrescarTabla(); }
    }

    private async void BtnValoracion_Click(object? sender, RoutedEventArgs e)
    {
        var art = ArticuloSeleccionado();
        if (art == null) { await new DialogoMensaje("Aviso", "Selecciona un artículo.").ShowDialog(this); return; }
        var val = await new DialogoValoracion().ShowDialog<(int, string, string?, string?)?>(this);
        if (val.HasValue)
            try
            {
                _ctrl.AnadirValoracion(art.Id, val.Value.Item1, val.Value.Item2, val.Value.Item3, val.Value.Item4);
                RefrescarTabla();
            }
            catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }

    private async void BtnVerValoraciones_Click(object? sender, RoutedEventArgs e)
    {
        var art = ArticuloSeleccionado();
        if (art == null) { await new DialogoMensaje("Aviso", "Selecciona un artículo.").ShowDialog(this); return; }
        List<Valoracion> vals = art switch { Libro l => l.Valoraciones, Audiolibro a => a.Valoraciones, _ => new() };
        await new DialogoVerValoraciones(art.Titulo, vals).ShowDialog(this);
    }

    private async void BtnExportarCsv_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar catálogo CSV",
            SuggestedFileName = "catalogo.csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
        });
        if (file != null)
            try { _ctrl.ExportarCsv(file.Path.LocalPath); }
            catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }

    private async void BtnImportarCsv_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar catálogo CSV", AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
        });
        if (files.Count > 0)
            try { _ctrl.ImportarCsv(files[0].Path.LocalPath); RefrescarTabla(); }
            catch (Exception ex) { await new DialogoMensaje("Error", ex.Message).ShowDialog(this); }
    }
}
