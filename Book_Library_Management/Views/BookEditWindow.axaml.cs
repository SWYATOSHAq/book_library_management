using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Book_Library_Management.Data;
using Book_Library_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_Library_Management.Views;

public partial class BookEditWindow : Window
{
    private int? _bookId;

    // Parameterless constructor required by Avalonia XAML compiler
    public BookEditWindow()
    {
        InitializeComponent();
        Title = "Добавить книгу";
        LoadComboBoxes();
    }

    public BookEditWindow(int bookId) : this()
    {
        _bookId = bookId;
        Title   = "Редактировать книгу";
        LoadBook(bookId);
    }

    private void LoadComboBoxes()
    {
        using var db = new LibraryDbContext();
        AuthorCombo.ItemsSource = db.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).ToList();
        GenreCombo.ItemsSource  = db.Genres.OrderBy(g => g.Name).ToList();
    }

    private void LoadBook(int id)
    {
        using var db = new LibraryDbContext();
        var book = db.Books.Include(b => b.Author).Include(b => b.Genre)
                           .FirstOrDefault(b => b.Id == id);
        if (book == null) return;

        TitleBox.Text     = book.Title;
        YearBox.Value     = book.PublishYear;
        IsbnBox.Text      = book.ISBN;
        QuantityBox.Value = book.QuantityInStock;

        var authors = (System.Collections.Generic.List<Author>)AuthorCombo.ItemsSource!;
        AuthorCombo.SelectedItem = authors.FirstOrDefault(a => a.Id == book.AuthorId);

        var genres = (System.Collections.Generic.List<Genre>)GenreCombo.ItemsSource!;
        GenreCombo.SelectedItem = genres.FirstOrDefault(g => g.Id == book.GenreId);
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        { ShowError("Введите название книги."); return; }

        if (AuthorCombo.SelectedItem is not Author selectedAuthor)
        { ShowError("Выберите автора."); return; }

        if (GenreCombo.SelectedItem is not Genre selectedGenre)
        { ShowError("Выберите жанр."); return; }

        using var db = new LibraryDbContext();

        if (_bookId.HasValue)
        {
            var book = await db.Books.FindAsync(_bookId.Value);
            if (book == null) return;
            FillBook(book, selectedAuthor.Id, selectedGenre.Id);
        }
        else
        {
            var book = new Book();
            FillBook(book, selectedAuthor.Id, selectedGenre.Id);
            db.Books.Add(book);
        }

        await db.SaveChangesAsync();
        Close();
    }

    private void FillBook(Book book, int authorId, int genreId)
    {
        book.Title           = TitleBox.Text!.Trim();
        book.AuthorId        = authorId;
        book.GenreId         = genreId;
        book.PublishYear     = (int)(YearBox.Value ?? 2024);
        book.ISBN            = IsbnBox.Text?.Trim() ?? string.Empty;
        book.QuantityInStock = (int)(QuantityBox.Value ?? 0);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text      = message;
        ErrorLabel.IsVisible = true;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
