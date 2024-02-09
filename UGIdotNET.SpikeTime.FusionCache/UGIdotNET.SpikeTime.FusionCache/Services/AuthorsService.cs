
using Dapper;
using Microsoft.Data.SqlClient;
using UGIdotNET.SpikeTime.FusionCache.Models;

namespace UGIdotNET.SpikeTime.FusionCache.Services;

public class AuthorsService
{
    private readonly string _connectionString;

    public AuthorsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("BooksDatabase")!;
    }

    public async Task<Author[]> GetAuthorsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        await Task.Delay(2000);

        var authors = await connection.QueryAsync<Author>(
            "SELECT id AS Id, first_name AS FirstName, last_name AS LastName, middle_name AS MiddleName FROM authors ORDER BY first_name, last_name");

        return authors?.ToArray() ?? [];
    }

    public async Task<Author?> GetAuthorByIdAsync(int authorId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var author = await connection.QueryFirstOrDefaultAsync<Author>(
            "SELECT id AS Id, first_name AS FirstName, last_name AS LastName, middle_name AS MiddleName FROM authors WHERE id=@authorId",
            new { authorId });

        await Task.Delay(3000);

        return author;
    }
}
