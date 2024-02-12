using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AkkoCore.Extensions;

public static class YamlExt
{
    private static readonly ISerializer _defaultSerializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreFields()
        .Build();

    private static readonly IDeserializer _defaultDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Serializes this object to Yaml.
    /// </summary>
    /// <param name="obj">This object.</param>
    /// <param name="serializer">The serializer to be used. By default, it omits fields, null properties, and uses snake_case convention for naming variables.</param>
    /// <returns>This object serialized to Yaml.</returns>
    public static string ToYaml(this object obj, ISerializer? serializer = default)
    {
        serializer ??= _defaultSerializer;
        return serializer.Serialize(obj);
    }

    /// <summary>
    /// Serializes this object to Yaml.
    /// </summary>
    /// <param name="obj">This object.</param>
    /// <param name="writer">The stream to write the Yaml to.</param>
    /// <param name="serializer">The serializer to be used. By default, it omits fields, null properties, and uses snake_case convention for naming variables.</param>
    /// <returns>The text stream with the serialized object.</returns>
    public static TextWriter ToYaml(this object obj, TextWriter writer, ISerializer? serializer = default)
    {
        serializer ??= _defaultSerializer;
        serializer.Serialize(writer, obj);

        return writer;
    }

    /// <summary>
    /// Deserializes this string to a <typeparamref name="T"/> object.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="input">This string.</param>
    /// <param name="deserializer">The deserializer to be used. By default, it omits unmatched properties.</param>
    /// <returns>A <typeparamref name="T"/> object.</returns>
    /// <exception cref="YamlException">Occurs when deserialization fails.</exception>
    public static T FromYaml<T>(this string input, IDeserializer? deserializer = default)
    {
        deserializer ??= _defaultDeserializer;
        return deserializer.Deserialize<T>(input);
    }

    /// <summary>
    /// Deserializes this stream to a <typeparamref name="T"/> object.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="input">This stream.</param>
    /// <param name="deserializer">The deserializer to be used. By default, it omits unmatched properties.</param>
    /// <returns>A <typeparamref name="T"/> object.</returns>
    /// <exception cref="YamlException">Occurs when deserialization fails.</exception>
    public static T FromYaml<T>(this TextReader input, IDeserializer? deserializer = default)
    {
        deserializer ??= _defaultDeserializer;
        return deserializer.Deserialize<T>(input);
    }
}