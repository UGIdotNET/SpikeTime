
using Dapper;
using Microsoft.Data.SqlClient;
using UGIdotNET.SpikeTime.FusionCache.Models;

namespace UGIdotNET.SpikeTime.FusionCache.Services;

public class BooksService
{
    private readonly string _connectionString;

    private static int BookDetailRequestCount = 0;

    public BooksService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("BooksDatabase")!;
    }

    public async Task<Book[]> GetBooksAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var books = await connection.QueryAsync<Book>(
            "SELECT id AS Id, title AS Title, year AS Year, pages as Pages FROM books ORDER BY title");

        await Task.Delay(1000);

        return books?.ToArray() ?? [];
    }

    public async Task<Book?> GetBookByIdAsync(int bookId)
    {
        BookDetailRequestCount++;

        if (BookDetailRequestCount == 2)
        {
            throw new InvalidOperationException();
        }

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var book = await connection.QueryFirstOrDefaultAsync<Book>(
            "SELECT id AS Id, title AS Title, year AS Year, pages AS Pages FROM books WHERE id=@bookId", 
            new { bookId });

        await Task.Delay(2000);

        return book;
    }

    public async Task UpdateBookAsync(int id, Book model)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var sql = "UPDATE books SET title=@title WHERE id=@id";
        await connection.ExecuteAsync(sql, new { title = model.Title, id = id });
    }
}
