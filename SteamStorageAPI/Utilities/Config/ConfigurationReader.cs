namespace SteamStorageAPI.Utilities.Config;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class ConfigurationReader
{
    public static AppConfig Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        using StreamReader reader = new(path);

        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<AppConfig>(reader);
    }
}