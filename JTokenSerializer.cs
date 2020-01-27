using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using System;

namespace MongoDB.Bson.Serialization
{
    public class JObjectGenericSerializer : IBsonSerializer<JToken>
    {
        public Type ValueType => typeof(JToken);

        public JToken Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return GenerateJToken(context.Reader, null);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JToken value)
        {
            GenerateBsonValue(context.Writer, value);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            GenerateBsonValue(context.Writer, value as JToken);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var response = GenerateJToken(context.Reader, null);
            return response;
        }


        #region GenerateBsonValue

        private void GenerateBsonValue(IBsonWriter writer, JToken value)
        {
            if (value != null)
            {
                switch (value.Type)
                {
                    case JTokenType.Undefined:
                        writer.WriteUndefined();
                        return;
                    case JTokenType.Null:
                    case JTokenType.None:
                        writer.WriteNull();
                        return;
                    case JTokenType.Object:
                        GenerateBsonDocument(writer, value);
                        return;
                    case JTokenType.Array:
                        GenerateBsonArray(writer, value);
                        return;
                    case JTokenType.Integer:
                        writer.WriteInt32(value.Value<int>());
                        return;
                    case JTokenType.Float:
                        writer.WriteDouble(value.Value<double>());
                        return;
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                    case JTokenType.Raw:
                    case JTokenType.Date:
                    case JTokenType.Constructor:
                    case JTokenType.Property:
                    case JTokenType.Comment:
                    case JTokenType.String:
                        writer.WriteString(value.Value<string>());
                        return;
                    case JTokenType.Boolean:
                        writer.WriteBoolean(value.Value<bool>());
                        return;
                    case JTokenType.Bytes:
                        writer.WriteBytes(value.Value<byte[]>());
                        return;
                    case JTokenType.TimeSpan:
                        writer.WriteInt64(value.Value<long>());
                        return;
                    default:
                        break;
                }
            }
            writer.WriteNull();
        }

        private void GenerateBsonDocument(IBsonWriter writer, JToken parent)
        {
            writer.WriteStartDocument();

            foreach (var property in (parent as JObject).Properties())
            {
                writer.WriteName(property.Name);
                GenerateBsonValue(writer, property.Value);
            }

            writer.WriteEndDocument();
        }

        private void GenerateBsonArray(IBsonWriter writer, JToken parent)
        {
            writer.WriteStartArray();

            foreach (var value in (parent as JArray).Children())
            {
                GenerateBsonValue(writer, value);
            }

            writer.WriteEndArray();
        }

        #endregion

        #region GenerateJToken..

        private JToken GenerateJToken(IBsonReader reader, JToken parent)
        {
            switch (reader.CurrentBsonType)
            {
                case BsonType.Symbol:
                case BsonType.JavaScriptWithScope:
                case BsonType.JavaScript:
                case BsonType.ObjectId:
                case BsonType.RegularExpression:
                case BsonType.DateTime:
                case BsonType.Decimal128:
                case BsonType.MinKey:
                case BsonType.MaxKey:
                case BsonType.String:
                    return reader.ReadString();
                case BsonType.Binary:
                    return reader.ReadBytes();
                case BsonType.Undefined:
                    reader.ReadUndefined();
                    return JValue.CreateUndefined();
                case BsonType.Boolean:
                    return reader.ReadBoolean();
                case BsonType.Null:
                    reader.ReadNull();
                    return JValue.CreateNull();
                case BsonType.Int32:
                    return reader.ReadInt32();
                case BsonType.Int64:
                case BsonType.Timestamp:
                    return reader.ReadInt64();
                case BsonType.Double:
                    return reader.ReadDouble();
                case BsonType.Document:
                    return GenerateJObject(reader, parent);
                case BsonType.Array:
                    return GenerateJArray(reader, parent);
                case BsonType.EndOfDocument:
                    break;
                default:
                    break;
            }
            return null;
        }

        private JObject GenerateJObject(IBsonReader reader, JToken parent)
        {
            var jobject = new JObject();
            reader.ReadStartDocument();
            string propertyName = null;
            do
            {
                if (reader.State == BsonReaderState.Type)
                {
                    reader.ReadBsonType();
                }

                if (reader.State == BsonReaderState.Name)
                {
                    propertyName = reader.ReadName();
                }

                if (reader.State == BsonReaderState.Value)
                {
                    var value = GenerateJToken(reader, jobject);
                    jobject.Add(propertyName, value);
                }

            } while (reader.State != BsonReaderState.EndOfDocument);
            reader.ReadEndDocument();
            return jobject;
        }

        private JArray GenerateJArray(IBsonReader reader, JToken parent)
        {
            var jarray = new JArray();
            reader.ReadStartArray();
            do
            {
                if (reader.State == BsonReaderState.Type)
                {
                    reader.ReadBsonType();
                }

                if (reader.State == BsonReaderState.Value)
                {
                    var value = GenerateJToken(reader, jarray);
                    jarray.Add(value);
                }
            } while (reader.State != BsonReaderState.EndOfArray);
            reader.ReadEndArray();
            return jarray;
        }

        #endregion
    }
}
