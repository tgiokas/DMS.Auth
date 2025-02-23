namespace DMS.Auth.Application.Dtos
{
    public partial class KeycloakClient
    {
        private class RoleRepresentation
        {
            public string id { get; set; }
            public string name { get; set; }
            public bool clientRole { get; set; }
            public bool composite { get; set; }
            // etc. (Keycloak returns more fields, but you only need what you plan to use)
        }    
    }
}
