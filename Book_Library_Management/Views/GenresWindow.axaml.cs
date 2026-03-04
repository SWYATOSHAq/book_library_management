using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Book_Library_Management.Data;
using Book_Library_Management.Models;

namespace Book_Library_Management.Views;

public partial class GenresWindow : Window
{
    private int? _editingId;

    public GenresWindow()
    {
        InitializeComponent();
        LoadGenres();
    }

    // ── Загрузка списка ─────────────────────────────────────────────────────

    private void LoadGenres()
    {
        using var db = new LibraryDbContext();
        GenresGrid.ItemsSource = db.Genres.OrderBy(g => g.Name).ToList();
    }

    // ── Выбор строки → заполнение формы ────────────────────────────────────

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GenresGrid.SelectedItem is Genre g) FillForm(g);
    }

    private void FillForm(Genre g)
    {
        _editingId         = g.Id;
        NameBox.Text        = g.Name;
        DescriptionBox.Text = g.Description;
        HideError();
    }

    // ── Добавить ────────────────────────────────────────────────────────────

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        _editingId = null;
        GenresGrid.SelectedItem = null;
        ClearForm();
    }

    // ── Редактировать ───────────────────────────────────────────────────────

    private void OnEdit(object? sender, RoutedEventArgs e)
    {
        if (GenresGrid.SelectedItem is not Genre g)
        { ShowError("Выберите жанр в таблице."); return; }
        FillForm(g);
    }

    // ── Удалить ─────────────────────────────────────────────────────────────

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (GenresGrid.SelectedItem is not Genre g)
        { ShowError("Выберите жанр для удаления."); return; }

        bool ok = await ConfirmDelete($"Удалить жанр «{g.Name}»?\n(будут удалены все книги этого жанра)");
        if (!ok) return;

        using var db = new LibraryDbContext();
        var entity = await db.Genres.FindAsync(g.Id);
        if (entity != null)
        {
            db.Genres.Remove(entity);
            await db.SaveChangesAsync();
        }

        ClearForm();
        LoadGenres();
    }

    // ── Сохранить ───────────────────────────────────────────────────────────

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        HideError();

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        { ShowError("Введите название жанра."); return; }

        using var db = new LibraryDbContext();

        if (_editingId.HasValue)
        {
            var entity = await db.Genres.FindAsync(_editingId.Value);
            if (entity == null) return;
            FillEntity(entity);
        }
        else
        {
            var entity = new Genre();
            FillEntity(entity);
            db.Genres.Add(entity);
        }

        await db.SaveChangesAsync();
        ClearForm();
        LoadGenres();
    }

    private void FillEntity(Genre g)
    {
        g.Name        = NameBox.Text!.Trim();
        g.Description = DescriptionBox.Text?.Trim() ?? string.Empty;
    }

    // ── Вспомогательные ────────────────────────────────────────────────────

    private void OnClearForm(object? sender, RoutedEventArgs e)
    {
        _editingId = null;
        GenresGrid.SelectedItem = null;
        ClearForm();
    }

    private void ClearForm()
    {
        NameBox.Text        = string.Empty;
        DescriptionBox.Text = string.Empty;
        _editingId = null;
        HideError();
    }

    private void ShowError(string msg) { FormError.Text = msg; FormError.IsVisible = true; }
    private void HideError() { FormError.IsVisible = false; }

    private async System.Threading.Tasks.Task<bool> ConfirmDelete(string message)
    {
        bool result = false;
        var dlg = new Window
        {
            Title = "Подтверждение", Width = 400, Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14 });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        var yes = new Button { Content = "Удалить" };
        var no  = new Button { Content = "Отмена"  };
        buttons.Children.Add(yes);
        buttons.Children.Add(no);
        panel.Children.Add(buttons);
        dlg.Content = panel;

        yes.Click += (_, _) => { result = true; dlg.Close(); };
        no.Click  += (_, _) => dlg.Close();

        await dlg.ShowDialog(this);
        return result;
    }
}
