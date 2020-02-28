using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace JSONSerializer
{
    public class JSONArray
    {
        private List<Tuple<JSONType, object>> items;

        public JSONArray()
        {
            this.items = new List<Tuple<JSONType, object>>();
        }

        public JSONArray(string source)
        {
            this.items = JSONArray.FromString(source).items;
        }

        internal static JSONArray FromString(string source)
        {
            StringReader instream = new StringReader(source);
            JsonTextReader reader = new JsonTextReader(instream);
            if(reader.Read())
            {
                if(reader.TokenType == JsonToken.StartArray)
                {
                    return JSONArray.FromJSONReader(ref reader);
                }
            }
            throw new ArgumentException("Given string is not a JSON array.");
        }

        internal static JSONArray FromJSONReader(ref JsonTextReader reader)
        {
            JSONArray result = new JSONArray();
            while(reader.Read())
            {
                switch(reader.TokenType)
                {
                    case JsonToken.StartObject:
                        result.AddObjectItem(JSONObject.FromJSONReader(ref reader));
                        break;
                    case JsonToken.EndArray:
                        return result;
                    case JsonToken.StartArray:
                        result.AddArrayItem(JSONArray.FromJSONReader(ref reader));
                        break;
                    case JsonToken.String:
                        result.AddStringItem(reader.Value as String);
                        break;
                    case JsonToken.Float:
                        result.AddFloatItem(reader.Value.ToString().Replace(',', '.'));
                        break;
                    case JsonToken.Integer:
                        result.AddIntItem(int.Parse(reader.Value.ToString()));
                        break;
                    case JsonToken.Boolean:
                        result.AddBoolItem(bool.Parse(reader.Value.ToString()));
                        break;
                    case JsonToken.Null:
                        result.AddNullItem();
                        break;
                    default:
                        throw new ArgumentException("Unexpected token type.");
                }
            }
            throw new ArgumentException("No end of array.");
        }

        public void AddObjectItem(JSONObject item)
            => this.items.Add(new Tuple<JSONType, object>(item == null ? JSONType.Null : JSONType.Object, item));

        public void AddArrayItem(JSONArray item)
            => this.items.Add(new Tuple<JSONType, object>(item == null ? JSONType.Null : JSONType.Array, item));

        public void AddIntItem(int item)
            => this.items.Add(new Tuple<JSONType, object>(JSONType.Number, item));
        
        public void AddFloatItem(string item)
            => this.items.Add(new Tuple<JSONType, object>(JSONType.Number, item));

        public void AddStringItem(string item)
            => this.items.Add(new Tuple<JSONType, object>(item == null ? JSONType.Null : JSONType.String, item));

        public void AddBoolItem(bool item)
            => this.items.Add(new Tuple<JSONType, object>(JSONType.Boolean, item));

        public void AddNullItem()
            => this.items.Add(new Tuple<JSONType, object>(JSONType.Null, null));

        public void RemoveItem(int index)
            => this.items.RemoveAt(index);

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");

            foreach(Tuple<JSONType, object> item in this.items)
            {
                switch(item.Item1)
                {
                    case JSONType.Object:
                        builder.Append(((JSONObject)item.Item2).ToString());
                        break;
                    case JSONType.Array:
                        builder.Append(((JSONArray)item.Item2).ToString());
                        break;
                    case JSONType.Null:
                        builder.Append("null");
                        break;
                    case JSONType.String:
                        builder.Append($"\"{item.Item2}\"");
                        break;
                    default:
                        builder.Append(item.Item2.ToString());
                        break;
                }
                builder.Append(",");
            }

            if(this.items.Count != 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append("]");
            return builder.ToString();
        }
    }
}