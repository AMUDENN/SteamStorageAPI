using System.Text;

namespace SteamStorageAPI.Utilities.Extensions;

public static class RandomExtension
{
    private static readonly char[] _alphabet =
    [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
        'W', 'X', 'Y', 'Z'
    ];

    public static string GenerateString(this Random rnd, int count)
    {
        StringBuilder str = new(count);
        for (int i = 0; i < count; i++)
            str.Append(_alphabet[rnd.Next(0, _alphabet.Length - 1)]);
        return str.ToString();
    }
}