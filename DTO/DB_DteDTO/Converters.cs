using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// Converter que automáticamente redondea decimales a 2 posiciones
    /// Para cumplir con los requerimientos del Ministerio de Hacienda
    /// </summary>
    public class DecimalRoundingConverter : JsonConverter<decimal>
    {
        private readonly int _decimalPlaces;

        public DecimalRoundingConverter(int decimalPlaces = 2)
        {
            _decimalPlaces = decimalPlaces;
        }

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var value = reader.GetDecimal();
                return Math.Round(value, _decimalPlaces, MidpointRounding.AwayFromZero);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (decimal.TryParse(stringValue, out var parsed))
                {
                    return Math.Round(parsed, _decimalPlaces, MidpointRounding.AwayFromZero);
                }
            }

            return 0m;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            var roundedValue = Math.Round(value, _decimalPlaces, MidpointRounding.AwayFromZero);
            writer.WriteNumberValue(roundedValue);
        }
    }

    /// <summary>
    /// Converter específico para montos monetarios (2 decimales)
    /// </summary>
    public class MoneyConverter : DecimalRoundingConverter
    {
        public MoneyConverter() : base(2) { }
    }

    /// <summary>
    /// Converter para cantidades que pueden tener más precisión (8 decimales)
    /// </summary>
    public class QuantityConverter : DecimalRoundingConverter
    {
        public QuantityConverter() : base(8) { }
    }

}
