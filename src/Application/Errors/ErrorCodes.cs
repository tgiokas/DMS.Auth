namespace Authentication.Application.Errors;

public static class ErrorCodes
{
    public static class AUTH
    {
        public const string GenericUnexpected = "AUTH-000";
        public const string AuthenticationFailed = "AUTH-001";
        public const string RefreshFailed = "AUTH-002";
        public const string LogoutFailed = "AUTH-003";
        public const string UsersNotFound = "AUTH-004";
        public const string UserNotFoundInKeycloak = "AUTH-005"; 
        public const string UsernameAlreadyExists = "AUTH-006";
        public const string CreateUserInKeycloakFailed = "AUTH-007";
        public const string CreateUserInDbFailed = "AUTH-008";
        public const string AssignAdminFailed = "AUTH-009";
        public const string UserIdNotFound = "AUTH-010";
        public const string UpdateInKeycloakFailed = "AUTH-011";
        public const string UpdatePasswordFailed = "AUTH-012";
        public const string UpdateDbRolledBack = "AUTH-013";
        public const string AssignAdminOnUpdateFailed = "AUTH-014";
        public const string RemoveAdminOnUpdateFailed = "AUTH-015";
        public const string DeleteTargetNotFound = "AUTH-016";
        public const string DeleteInKeycloakFailed = "AUTH-017";
        public const string SetAttributeFailed = "AUTH-018";
        public const string AttributeNotFound = "AUTH-019";
        public const string DeleteAttributeFailed = "AUTH-020";  
        public const string RolesNotFound = "AUTH-021";
        public const string RoleNotFound = "AUTH-022";
        public const string UserRolesNotFound = "AUTH-023";
        public const string RoleAlreadyExists = "AUTH-024";
        public const string CreateRoleFailed = "AUTH-025";
        public const string UpdateRoleFailed = "AUTH-026";
        public const string DeleteRoleFailed = "AUTH-027";
        public const string UsernameAndRolesRequired = "AUTH-028";
        public const string AssignRoleFailed = "AUTH-029";
        public const string RemoveRoleFailed = "AUTH-030"; 
        public const string RulesNotFound = "AUTH-031";
        public const string RuleNotFound = "AUTH-032";
        public const string UserIdNotValid = "AUTH-033";
        public const string RoleIdNotValid = "AUTH-034";
        public const string EmailVerificationSendFailed = "AUTH-035";
        public const string VerifyEmailTokenInValid = "AUTH-036";
        public const string LoginSessionExpired = "AUTH-037";
        public const string NoSecretInDBForUser = "AUTH-038";
        public const string InvalidTOTPcode = "AUTH-039";
        public const string InvalidEmailCode = "AUTH-040";
        public const string InvalidSmsCode = "AUTH-041";
        public const string NoEmailAvailableForMFA = "AUTH-042";
        public const string InvalidPhone = "AUTH-043";
        public const string EmailMfaSendFailed = "AUTH-044";
        public const string UserNotFoundInDB = "AUTH-045";
        public const string TOTPExists = "AUTH-046";
        public const string PasswordRequired = "AUTH-047";
        public const string KeycloakConflictInUser = "AUTH-048";
        public const string PasswordResetTokenInvalid = "AUTH-049";
        public const string InvalidMfaType = "AUTH-050";
    }
}