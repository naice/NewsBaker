namespace NewsBaker
{
    class JsonConverter : IJsonConverter
    {
        public T DeserializeObject<T>(string raw) where T : class
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(raw);
        }

        public string SerializeObject(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
