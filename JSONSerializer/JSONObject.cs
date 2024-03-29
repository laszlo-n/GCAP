using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace JSONSerializer
{
    public class JSONObject
    {
        private Dictionary<string, Tuple<JSONType, Object>> children;

        public JSONObject()
        {
            this.children = new Dictionary<string, Tuple<JSONType, Object>>();
        }

        internal JSONObject(Dictionary<string, Tuple<JSONType, Object>> children)
        {
            this.children = children;
        }

        public JSONObject(string source) : this()
        {
            this.children = JSONObject.FromString(source).children;
        }

        internal static JSONObject FromString(string source)
        {
            StringReader instream = new StringReader(source);
            JsonTextReader reader = new JsonTextReader(instream);
            if(reader.Read())
            {
                if(reader.TokenType == JsonToken.StartObject)
                {
                    return JSONObject.FromJSONReader(ref reader);
                }
            }
            throw new ArgumentException("Given string is not a JSON object.");
        }

        internal static JSONObject FromJSONReader(ref JsonTextReader reader)
        {
            JSONObject result = new JSONObject();
            string currentKey = string.Empty;

            void CheckKey()
            {
                if(currentKey == string.Empty)
                    throw new ArgumentException("Empty key for a JSON value.");
            }

            while(reader.Read())
            {
                switch(reader.TokenType)
                {
                    case JsonToken.StartObject:
                        CheckKey();
                        result.AddObjectChild(currentKey, JSONObject.FromJSONReader(ref reader));
                        break;
                    case JsonToken.EndObject:
                        return result;
                    case JsonToken.StartArray:
                        CheckKey();
                        result.AddArrayChild(currentKey, JSONArray.FromJSONReader(ref reader));
                        break;
                    case JsonToken.PropertyName:
                        currentKey = reader.Value as String;
                        break;
                    case JsonToken.String:
                        CheckKey();
                        result.AddStringChild(currentKey, reader.Value as String);
                        currentKey = "";
                        break;
                    case JsonToken.Float:
                        CheckKey();
                        result.AddFloatChild(currentKey, reader.Value.ToString().Replace(',', '.'));
                        currentKey = "";
                        break;
                    case JsonToken.Integer:
                        CheckKey();
                        result.AddIntChild(currentKey, int.Parse(reader.Value.ToString()));
                        currentKey = "";
                        break;
                    case JsonToken.Boolean:
                        CheckKey();
                        result.AddBoolChild(currentKey, bool.Parse(reader.Value.ToString()));
                        currentKey = "";
                        break;
                    case JsonToken.Null:
                        CheckKey();
                        result.AddNullChild(currentKey);
                        currentKey = "";
                        break;
                    default:
                        throw new ArgumentException("Unexpected token type.");
                }
            }
            throw new ArgumentException("No end of object.");
        }

        #region add and remove elements
        public void AddObjectChild(string key, JSONObject value)
            => Add(key, value == null ? JSONType.Null : JSONType.Object, value);

        public void AddStringChild(string key, string value)
            => Add(key, value == null ? JSONType.Null : JSONType.String, value);

        public void AddIntChild(string key, int value)
            => Add(key, JSONType.Number, value);
        
        public void AddFloatChild(string key, string value)
            => Add(key, JSONType.Number, value);

        public void AddArrayChild(string key, JSONArray value)
            => Add(key, value == null ? JSONType.Null : JSONType.Array, value);

        public void AddBoolChild(string key, bool value)
            => Add(key, JSONType.Boolean, value);

        public void AddNullChild(string key)
            => this.children.Add(key, new Tuple<JSONType, Object>(JSONType.Null, null));

        public void RemoveChild(string key)
        {
            if(this.children.ContainsKey(key)) // to prevent nullreferenceexceptions
            {
                this.children.Remove(key);
            }
        }

        private void Add(string key, JSONType type, Object value)
        {
            this.children.Add(key, new Tuple<JSONType, Object>(type, value));
        }
        #endregion

        #region get elements
        public List<KeyValuePair<string, object>> GetChildren()
        {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();

            foreach(KeyValuePair<string, Tuple<JSONType, object>> child in this.children)
            {
                result.Add(new KeyValuePair<string, object>(child.Key, child.Value.Item2));
            }

            return result;
        }
        
        public (JSONType, object) GetChild(string key)
        {
            try
            {
                Tuple<JSONType, Object> child = children[key];
                return (child.Item1, child.Item2);
            }
            catch(KeyNotFoundException ex)
            {
                throw new KeyNotFoundException("A child with the given key couldn't be found in this JSON object.", ex);
            }
        }

        public string GetStringChild(string key)
        {
            var tmp = this.GetChild(key);
            if(tmp.Item1 != JSONType.String)
            {
                throw new InvalidOperationException($"The child with key {key} couldn't be converted to string.");
            }

            return (string)tmp.Item2;
        }

        public int GetIntChild(string key)
        {
            var tmp = this.GetChild(key);
            if(tmp.Item1 != JSONType.Number)
            {
                throw new InvalidOperationException($"The child with key {key} couldn't be converted to int.");
            }

            return (int)tmp.Item2;
        }

        public bool GetBoolChild(string key)
        {
            var tmp = this.GetChild(key);
            if(tmp.Item1 != JSONType.Boolean)
            {
                throw new InvalidOperationException($"The child with key {key} couldn't be converted to boolean.");
            }

            return (bool)tmp.Item2;
        }

        public JSONArray GetArrayChild(string key)
        {
            var tmp = this.GetChild(key);
            if(tmp.Item1 != JSONType.Array)
            {
                throw new InvalidOperationException($"The child with key {key} couldn't be converted to JSONArray.");
            }

            return (JSONArray)tmp.Item2;
        }

        public JSONObject GetObjectChild(string key)
        {
            var tmp = this.GetChild(key);
            if(tmp.Item1 != JSONType.Object)
            {
                throw new InvalidOperationException($"The child with key {key} couldn't be converted to JSONObject.");
            }

            return (JSONObject)tmp.Item2;
        }
        #endregion

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("{");
            foreach(KeyValuePair<string, Tuple<JSONType, Object>> child in this.children)
            {
                builder.Append("\"");
                builder.Append(child.Key);
                builder.Append("\":");
                if(child.Value.Item1 == JSONType.String)
                {
                    builder.Append("\"");
                }
                switch(child.Value.Item1)
                {
                    case JSONType.Object:
                        builder.Append(((JSONObject)child.Value.Item2).ToString());
                        break;
                    case JSONType.Array:
                        builder.Append(((JSONArray)child.Value.Item2).ToString());
                        break;
                    case JSONType.Null:
                        builder.Append("null");
                        break;
                    default:
                        builder.Append(child.Value.Item2.ToString());
                        break;
                }
                if(child.Value.Item1 == JSONType.String)
                {
                    builder.Append("\"");
                }
                builder.Append(",");
            }

            if(this.children.Count > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append("}");
            return builder.ToString();
        }
    }
}
