namespace Zootify_Backend.Enums;

/// <summary>
/// Application-level error codes. Human-readable messages for each code
/// are defined in <c>Resources/ErrorMessages.resx</c> (de) and
/// <c>Resources/ErrorMessages.en.resx</c> (en).
/// </summary>
public enum ErrorCode
{
    None = 0,

    DefaultErrorCode = -1000000,

    DatabaseError = -1000001,
    
    Forbidden = -1000002,

    EmailError = -1001000,

    PasswordTooShort = -1001001,

    PasswordMissingUppercase = -1001002,

    PasswordMissingLowercase = -1001003,

    PasswordMissingDigit = -1001004,

    PasswordMissingSpecialCharacter = -1001005,

    PasswordNotTheSame = -1001006,

    IncorrectPassword = -1001007,

    UserWithThisEmailAlreadyExists = -1002000,

    NoUserWithThisToken = -1002001,

    NoUserWithThisEmail = -1002002,

    UserAlreadyActive = -1002003,

    UserIsBlocked = -1002004,

    UserIsDeleted = -1002005,

    UserPending = -1002006,
    
    NoUserWithThisGuid = -1002007,
    
    NoUpdatedData = -1002008,

    InvalidRegistrationToken = -1003000,

    RegistrationTokenExpired = -1003001,

    InvalidResetPasswordToken = -1004000,

    ResetPasswordTokenExpired = -1004001,
    
    NoTagsFound = -1005001,
    
    NoTaxonomyWithThisGuid = -1006001,
    
    SpeciesNotFound =  -1006002,
    
    AddressDoesNotExist =  -1006003,
    
    TaxonomyRankIsAtTheDeepestLevel = -1006004,
    
    TaxonomyRankIsNoSpecies = -1006005,

    CollectionNotFound = -1007001,

    NoSpecimenWithThisGuid = -1007002,

    SpecimenAlreadyInCollection = -1007003,

    SpecimenNotInCollection = -1007004

}
