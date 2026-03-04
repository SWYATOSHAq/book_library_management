using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Book_Library_Management.Data;
using Book_Library_Management.Models;
using Book_Library_Management.Views;
using Microsoft.EntityFrameworkCore;

namespace Book_Library_Management;

public partial class MainWindow : Window
{
    private List<Book> _allBooks = new();
    private bool _suppressFilter;

    public MainWindow()
    {
        InitializeComponent();
        LoadData();
    }

    // ── Загрузка данных ─────────────────────────────────────────────────────

    private void LoadData()
    {
        using var db = new LibraryDbContext();

        _allBooks = db.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .OrderBy(b => b.Title)
            .ToList();

        RefreshFilters(db);
        ApplyFilters();
    }

    private void RefreshFilters(LibraryDbContext db)
    {
        _suppressFilter = true;

        var selectedAuthorId = (AuthorFilter.SelectedItem as Author)?.Id;
        var selectedGenreId  = (GenreFilter.SelectedItem  as Genre)?.Id;

        var authors = db.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).ToList();
        AuthorFilter.ItemsSource   = new List<object> { "Все авторы" }.Concat(authors.Cast<object>()).ToList();
        AuthorFilter.SelectedIndex = authors.FindIndex(a => a.Id == selectedAuthorId) is >= 0 and var ai ? ai + 1 : 0;

        var genres = db.Genres.OrderBy(g => g.Name).ToList();
        GenreFilter.ItemsSource   = new List<object> { "Все жанры" }.Concat(genres.Cast<object>()).ToList();
        GenreFilter.SelectedIndex = genres.FindIndex(g => g.Id == selectedGenreId) is >= 0 and var gi ? gi + 1 : 0;

        _suppressFilter = false;
    }

    // ── Фильтрация ──────────────────────────────────────────────────────────

    private void ApplyFilters()
    {
        IEnumerable<Book> result = _allBooks;

        var search = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(search))
            result = result.Where(b => b.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (AuthorFilter.SelectedItem is Author author)
            result = result.Where(b => b.AuthorId == author.Id);

        if (GenreFilter.SelectedItem is Genre genre)
            result = result.Where(b => b.GenreId == genre.Id);

        var list = result.ToList();
        BooksGrid.ItemsSource = list;

        int total = _allBooks.Sum(b => b.QuantityInStock);
        StatusLabel.Text = $"Показано: {list.Count} из {_allBooks.Count} книг  |  Всего экземпляров: {total}";
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilters();

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_suppressFilter) ApplyFilters();
    }

    private void OnResetFilters(object? sender, RoutedEventArgs e)
    {
        _suppressFilter = true;
        SearchBox.Text        = string.Empty;
        AuthorFilter.SelectedIndex = 0;
        GenreFilter.SelectedIndex  = 0;
        _suppressFilter = false;
        ApplyFilters();
    }

    // ── CRUD книг ───────────────────────────────────────────────────────────

    private async void OnAddBook(object? sender, RoutedEventArgs e)
    {
        var win = new BookEditWindow();
        await win.ShowDialog(this);
        LoadData();
    }

    private async void OnEditBook(object? sender, RoutedEventArgs e)
    {
        if (BooksGrid.SelectedItem is not Book book) return;
        var win = new BookEditWindow(book.Id);
        await win.ShowDialog(this);
        LoadData();
    }

    private async void OnDeleteBook(object? sender, RoutedEventArgs e)
    {
        if (BooksGrid.SelectedItem is not Book book) return;

        bool confirmed = await ShowConfirmDialog($"Удалить книгу «{book.Title}»?");
        if (!confirmed) return;

        using var db = new LibraryDbContext();
        var entity = await db.Books.FindAsync(book.Id);
        if (entity != null)
        {
            db.Books.Remove(entity);
            await db.SaveChangesAsync();
        }
        LoadData();
    }

    // ── Управление справочниками ────────────────────────────────────────────

    private async void OnManageAuthors(object? sender, RoutedEventArgs e)
    {
        var win = new AuthorsWindow();
        await win.ShowDialog(this);
        LoadData();
    }

    private async void OnManageGenres(object? sender, RoutedEventArgs e)
    {
        var win = new GenresWindow();
        await win.ShowDialog(this);
        LoadData();
    }

    // ── Диалог подтверждения ────────────────────────────────────────────────

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(string message)
    {
        bool result = false;

        var dialog = new Window
        {
            Title  = "Подтверждение",
            Width  = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        });

        var buttons = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var yesBtn = new Button { Content = "Удалить" };
        var noBtn  = new Button { Content = "Отмена"  };
        buttons.Children.Add(yesBtn);
        buttons.Children.Add(noBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        yesBtn.Click += (_, _) => { result = true; dialog.Close(); };
        noBtn.Click  += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);
        return result;
    }
}
