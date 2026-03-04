namespace Book_Library_Management.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int PublishYear { get; set; }
    public string ISBN { get; set; } = string.Empty;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;

    public int QuantityInStock { get; set; }
}
