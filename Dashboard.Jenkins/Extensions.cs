using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Dashboard.Jenkins
{
    internal static class Extensions
    {
        internal static bool Read(this JsonReader reader, JsonToken token)
        {
            return reader.Read() && reader.TokenType == token;
        }

        internal static bool IsProperty(this JsonReader reader, string propertyName)
        {
            return
                reader.TokenType == JsonToken.PropertyName &&
                (string)reader.Value == propertyName;
        }

        /// <summary>
        /// Read the entire property value and discard it.  Needs to handle nested objects, arrays, etc ... 
        /// </summary>
        internal static void ReadProperty(this JsonReader reader)
        {
            Debug.Assert(reader.TokenType == JsonToken.PropertyName);

            var depth = reader.Depth;
            if (!reader.Read())
            {
                return;
            }

            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                case JsonToken.String:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.Null:
                case JsonToken.Date:
                    reader.Read();
                    break;
                case JsonToken.StartArray:
                case JsonToken.StartConstructor:
                case JsonToken.StartObject:

                    // For nested objects just read until the depth gets the same again
                    do
                    {
                        reader.Read();
                    } while (reader.Depth != depth);

                    // now we are on the end token so read past that
                    reader.Read();
                    break;
                default:
                    throw new Exception($"Unrecognized property value kind {reader.TokenType}");
            }
        }

        /// <summary>
        /// Called when in the middle of an object member list.  Will read until it hits a <see cref="JsonToken.EndObject"/>.  Properly handles
        /// all properties.
        /// </summary>
        internal static void ReadTillEndOfObject(this JsonReader reader)
        {
            Debug.Assert(reader.TokenType == JsonToken.PropertyName || reader.TokenType == JsonToken.EndObject);
            while (reader.TokenType == JsonToken.PropertyName)
            {
                reader.ReadProperty();
            }

            if (reader.TokenType != JsonToken.EndObject)
            {
                throw new Exception($"Unexpected token state trying to reach end of object: {reader.TokenType}");
            }
        }
    }
}
