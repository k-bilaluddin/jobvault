using BCrypt.Net;

Console.Write("Password: ");
var password = ReadPassword();

if (string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Password cannot be empty.");
    return 1;
}

var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
Console.WriteLine();
Console.WriteLine(hash);
return 0;

static string ReadPassword()
{
    var sb = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0) sb.Remove(sb.Length - 1, 1);
        }
        else
        {
            sb.Append(key.KeyChar);
        }
    }
    return sb.ToString();
}
