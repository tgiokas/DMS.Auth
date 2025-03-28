using System.Text.Json.Serialization;

namespace DMS.Auth.Application.Dtos
{
    public class Credential
    {
        //[JsonProperty("algorithm")]
        //public string Algorithm { get; set; }
        //[JsonProperty("config")]
        //public IDictionary<string, string> Config { get; set; }
        //[JsonProperty("counter")]
        //public int? Counter { get; set; }
        //[JsonProperty("createdDate")]
        //public long? CreatedDate { get; set; }
        //[JsonProperty("device")]
        //public string Device { get; set; }
        //[JsonProperty("digits")]
        //public int? Digits { get; set; }
        //[JsonProperty("hashIterations")]
        //public int? HashIterations { get; set; }
        //[JsonProperty("hashSaltedValue")]
        //public string HashSaltedValue { get; set; }
        //[JsonProperty("period")]
        //public int? Period { get; set; }
        //[JsonProperty("salt")]
        //public string Salt { get; set; }
        [JsonPropertyName("temporary")]
        public bool? Temporary { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
