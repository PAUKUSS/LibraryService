using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LibraryService.Service.Security;

public static class JwtHelper
{
    private static readonly byte[] _key = Encoding.UTF8.GetBytes("LibraryServiceSecretKey32Chars!!");

    public static string GenerateToken(string username, string role, TimeSpan lifetime)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new Dictionary<string, object>
        {
            ["sub"] = username,
            ["role"] = role,
            ["exp"] = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds(),
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var h = B64(JsonSerializer.Serialize(header));
        var p = B64(JsonSerializer.Serialize(payload));
        return $"{h}.{p}.{Sign($"{h}.{p}")}";
    }

    public static ClaimsPrincipal? Validate(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3 || Sign($"{parts[0]}.{parts[1]}") != parts[2]) return null;

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(Pad(parts[1].Replace('-', '+').Replace('_', '/'))));
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

            if (DateTimeOffset.FromUnixTimeSeconds(payload["exp"].GetInt64()) < DateTimeOffset.UtcNow) return null;

            var claims = new List<Claim> { new(ClaimTypes.Name, payload["sub"].GetString()!) };
            if (payload.TryGetValue("role", out var r))
                claims.Add(new Claim(ClaimTypes.Role, r.GetString()!));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "JWT"));
        }
        catch { return null; }
    }

    public static string? GetUser(ClaimsPrincipal p) => p.FindFirst(ClaimTypes.Name)?.Value;
    public static string? GetRole(ClaimsPrincipal p) => p.FindFirst(ClaimTypes.Role)?.Value;

    static string Sign(string data) { using var h = new HMACSHA256(_key); return B64(h.ComputeHash(Encoding.UTF8.GetBytes(data))); }
    static string B64(string s) => B64(Encoding.UTF8.GetBytes(s));
    static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    static string Pad(string s) => s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
}

public static class UserStore
{
    public record User(string Username, string Password, string Role);

    public static readonly Dictionary<string, User> Users = new()
    {
        ["librarian"] = new("librarian", "lib123", "Librarian"),
        ["reader"] = new("reader", "read123", "Reader"),
        ["admin"] = new("admin", "admin123", "Admin"),
    };
}
