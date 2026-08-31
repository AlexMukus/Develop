using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Storage.JsonConverters;

/// <summary>
/// Конвертер для словаря <see cref="PhysicalKey"/> → <see cref="KeyStatistics"/>.
/// System.Text.Json по умолчанию не умеет сериализовать ключ-запись, поэтому сохраняем как массив пар Key/Value.
/// </summary>
internal sealed class PhysicalKeyDictionaryConverter : JsonConverter<IReadOnlyDictionary<PhysicalKey, KeyStatistics>>
{
    private const string KeyPropertyName = "Key";
    private const string ValuePropertyName = "Value";

    /// <inheritdoc />
    public override IReadOnlyDictionary<PhysicalKey, KeyStatistics>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Ожидался массив пар Key/Value.");
        }

        var dictionary = new Dictionary<PhysicalKey, KeyStatistics>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Ожидался объект пары Key/Value.");
            }

            PhysicalKey? key = null;
            KeyStatistics? value = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Ожидалось имя свойства.");
                }

                string propertyName = reader.GetString()!;
                reader.Read();

                if (propertyName == KeyPropertyName)
                {
                    key = JsonSerializer.Deserialize<PhysicalKey>(ref reader, options);
                }
                else if (propertyName == ValuePropertyName)
                {
                    value = JsonSerializer.Deserialize<KeyStatistics>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            if (key != null && value != null)
            {
                dictionary[key] = value;
            }
        }

        throw new JsonException("Неожиданный конец JSON при чтении словаря.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<PhysicalKey, KeyStatistics> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach ((PhysicalKey key, KeyStatistics statistics) in value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(KeyPropertyName);
            JsonSerializer.Serialize(writer, key, options);
            writer.WritePropertyName(ValuePropertyName);
            JsonSerializer.Serialize(writer, statistics, options);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
