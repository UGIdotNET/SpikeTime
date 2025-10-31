// See https://aka.ms/new-console-template for more information
using OpenSearch.Client;
using System.Runtime.CompilerServices;

Console.WriteLine("Hello, World!");

var settings = new ConnectionSettings(new Uri("https://localhost:9200"))
    .ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
    .BasicAuthentication("admin", "<your password>") // Add credentials
    .DefaultIndex("students");

var client = new OpenSearchClient(settings);

//SeedData(client);


Console.ReadLine();

void SeedData(OpenSearchClient client)
{
    var students = new List<Student>
    {
        new Student { Id = 1, FirstName = "Alice", LastName = "Johnson", Gpa = 3.85, GradYear = 2024 },
        new Student { Id = 2, FirstName = "Bob", LastName = "Smith", Gpa = 3.42, GradYear = 2023 },
        new Student { Id = 3, FirstName = "Carol", LastName = "Davis", Gpa = 3.91, GradYear = 2025 },
        new Student { Id = 4, FirstName = "David", LastName = "Wilson", Gpa = 2.87, GradYear = 2022 },
        new Student { Id = 5, FirstName = "Emma", LastName = "Brown", Gpa = 3.76, GradYear = 2024 },
        new Student { Id = 6, FirstName = "Frank", LastName = "Miller", Gpa = 3.15, GradYear = 2023 },
        new Student { Id = 7, FirstName = "Grace", LastName = "Taylor", Gpa = 3.98, GradYear = 2025 },
        new Student { Id = 8, FirstName = "Henry", LastName = "Anderson", Gpa = 3.33, GradYear = 2022 },
        new Student { Id = 9, FirstName = "Iris", LastName = "Thomas", Gpa = 3.67, GradYear = 2024 },
        new Student { Id = 10, FirstName = "Jack", LastName = "White", Gpa = 2.95, GradYear = 2023 }
    };

    foreach (var student in students)
    {
        var response = client.Index(student, i => i.Index("students"));
        
        if (response.IsValid)
        {
            Console.WriteLine($"Student {student.FirstName} {student.LastName} indexed successfully");
        }
        else
        {
            Console.WriteLine($"ERROR indexing {student.FirstName} {student.LastName}: {response}");
        }
    }
}


public class Student
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int GradYear { get; init; }
    public double Gpa { get; init; }
}