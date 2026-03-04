using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Book_Library_Management.Data;
using Book_Library_Management.Models;

namespace Book_Library_Management.Views;

public partial class AuthorsWindow : Window
{
    private int? _editingId;

    public AuthorsWindow()
    {
        InitializeComponent();
        LoadAuthors();
    }

    // ── Загрузка списка ─────────────────────────────────────────────────────

    private void LoadAuthors()
    {
        using var db = new LibraryDbContext();
        AuthorsGrid.ItemsSource = db.Authors
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToList();
    }

    // ── Выбор строки → заполнение формы ────────────────────────────────────

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AuthorsGrid.SelectedItem is Author a) FillForm(a);
    }

    private void FillForm(Author a)
    {
        _editingId        = a.Id;
        FirstNameBox.Text = a.FirstName;
        LastNameBox.Text  = a.LastName;
        CountryBox.Text   = a.Country;
        BirthDatePicker.SelectedDate = a.BirthDate.HasValue
            ? new DateTimeOffset(a.BirthDate.Value, TimeSpan.Zero)
            : null;
        HideError();
    }

    // ── Добавить ────────────────────────────────────────────────────────────

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        _editingId = null;
        AuthorsGrid.SelectedItem = null;
        ClearForm();
    }

    // ── Редактировать ───────────────────────────────────────────────────────

    private void OnEdit(object? sender, RoutedEventArgs e)
    {
        if (AuthorsGrid.SelectedItem is not Author a)
        { ShowError("Выберите автора в таблице."); return; }
        FillForm(a);
    }

    // ── Удалить ─────────────────────────────────────────────────────────────

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (AuthorsGrid.SelectedItem is not Author a)
        { ShowError("Выберите автора для удаления."); return; }

        bool ok = await ConfirmDelete($"Удалить автора «{a.FullName}»?\n(будут удалены все его книги)");
        if (!ok) return;

        using var db = new LibraryDbContext();
        var entity = await db.Authors.FindAsync(a.Id);
        if (entity != null)
        {
            db.Authors.Remove(entity);
            await db.SaveChangesAsync();
        }

        ClearForm();
        LoadAuthors();
    }

    // ── Сохранить ───────────────────────────────────────────────────────────

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        HideError();

        if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
        { ShowError("Введите имя."); return; }

        if (string.IsNullOrWhiteSpace(LastNameBox.Text))
        { ShowError("Введите фамилию."); return; }

        using var db = new LibraryDbContext();

        if (_editingId.HasValue)
        {
            var entity = await db.Authors.FindAsync(_editingId.Value);
            if (entity == null) return;
            FillEntity(entity);
        }
        else
        {
            var entity = new Author();
            FillEntity(entity);
            db.Authors.Add(entity);
        }

        await db.SaveChangesAsync();
        ClearForm();
        LoadAuthors();
    }

    private void FillEntity(Author a)
    {
        a.FirstName = FirstNameBox.Text!.Trim();
        a.LastName  = LastNameBox.Text!.Trim();
        a.Country   = CountryBox.Text?.Trim() ?? string.Empty;
        a.BirthDate = BirthDatePicker.SelectedDate?.DateTime;
    }

    // ── Вспомогательные ────────────────────────────────────────────────────

    private void OnClearForm(object? sender, RoutedEventArgs e)
    {
        _editingId = null;
        AuthorsGrid.SelectedItem = null;
        ClearForm();
    }

    private void ClearForm()
    {
        FirstNameBox.Text             = string.Empty;
        LastNameBox.Text              = string.Empty;
        CountryBox.Text               = string.Empty;
        BirthDatePicker.SelectedDate  = null;
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
