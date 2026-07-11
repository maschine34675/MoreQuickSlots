using System;
using EFT.InventoryLogic;
using Newtonsoft.Json;

namespace MoreQuickSlots
{
    public class BoundItemStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(EBoundItem) || objectType == typeof(EBoundItem?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(((EBoundItem)value).ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return objectType == typeof(EBoundItem?) ? (object)null : default(EBoundItem);
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return (EBoundItem)Convert.ToInt32(reader.Value);
            }

            string text = reader.Value?.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return default(EBoundItem);
            }

            if (int.TryParse(text, out int numeric))
            {
                return (EBoundItem)numeric;
            }

            return (EBoundItem)Enum.Parse(typeof(EBoundItem), text, true);
        }
    }
}
