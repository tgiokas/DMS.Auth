using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DMS.Auth.Application.Dtos;

public class KeycloakUser
{
    //[JsonProperty("id")]
    //public string Id { get; set; }
    //[JsonProperty("createdTimestamp")]
    //public long CreatedTimestamp { get; set; }
    [JsonPropertyName("username")]
    public string UserName { get; set; }
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
    //[JsonProperty("totp")]
    //public bool? Totp { get; set; }
    //[JsonProperty("emailVerified")]
    //public bool? EmailVerified { get; set; }
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }    
  
    [JsonPropertyName("credentials")]
    public IEnumerable<Credentials> Credentials { get; set; }
    //[JsonProperty("groups")]
    //public IEnumerable<string> Groups { get; set; }
    //[JsonProperty("origin")]
    //public string Origin { get; set; }
    //[JsonProperty("realmRoles")]
    //public IEnumerable<string> RealmRoles { get; set; }
    //[JsonProperty("self")]
    //public string Self { get; set; }
    //[JsonProperty("serviceAccountClientId")]
    //public string ServiceAccountClientId { get; set; }

}