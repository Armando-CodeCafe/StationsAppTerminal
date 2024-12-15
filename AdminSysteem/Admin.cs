using System.Security.Cryptography;
using System.Text;
public class Admin()
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public static string HashString(string s)
    {
        SHA256 hash = SHA256.Create();
        string hashedstring = Encoding.UTF8.GetString(hash.ComputeHash(Encoding.UTF8.GetBytes(s)));
        return hashedstring;
    }
}