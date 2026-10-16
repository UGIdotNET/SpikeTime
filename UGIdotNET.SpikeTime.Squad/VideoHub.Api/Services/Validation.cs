using System.Security.Cryptography;
using System.Text;
using VideoHub.Data;

namespace VideoHub.Api.Services;

public static class Validation
{
    public static Dictionary<string, string[]> Video(string? title, string? description, string? url)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "title", title, Limits.Title);
        Required(errors, "url", url, Limits.Url);
        if (description is { Length: > Limits.Description })
        {
            errors["description"] = [$"Max {Limits.Description} characters."];
        }

        if (!errors.ContainsKey("url") && !VideoUrl.TryParse(url!.Trim(), out _))
        {
            errors["url"] = ["Must be an https YouTube or Vimeo link."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Comment(string? author, string? text)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "author", author, Limits.Author);
        Required(errors, "text", text, Limits.CommentText);
        return errors;
    }

    public static string Hash(string salt, string value) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(salt), Encoding.UTF8.GetBytes(value)));

    private static void Required(Dictionary<string, string[]> errors, string field, string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errors[field] = ["Required."];
        }
        else if (trimmed.Length > max)
        {
            errors[field] = [$"Max {max} characters."];
        }
        else if (trimmed.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
        {
            errors[field] = ["Contains invalid characters."];
        }
    }
}
