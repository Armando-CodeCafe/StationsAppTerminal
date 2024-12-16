using System.Security.Cryptography;
using System.Text;

public class Admin()
{
    public long Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }

    public static string HashString(string s)
    {
        // Create a SHA256 hash from the input string
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Compute the hash
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(s));

            // Convert the byte array to a hexadecimal string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2")); // Convert to hex
            }
            return builder.ToString();
        }
    }
}
