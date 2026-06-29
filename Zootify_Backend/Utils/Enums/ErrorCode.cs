namespace Zootify_Backend.Enums;

/// <summary>
/// Application-level error codes. Human-readable messages for each code
/// are defined in <c>Resources/ErrorMessages.resx</c> (de) and
/// <c>Resources/ErrorMessages.en.resx</c> (en).
/// </summary>
public enum ErrorCode
{
    /// <summary>No error (default/success value).</summary>
    None = 0,

    /// <summary>Unspecified/unexpected error.</summary>
    DefaultErrorCode = -1000000,

    /// <summary>A database operation failed.</summary>
    DatabaseError = -1000001,

    /// <summary>The caller is not allowed to perform this action.</summary>
    Forbidden = -1000002,

    /// <summary>An email could not be sent.</summary>
    EmailError = -1001000,

    /// <summary>The password is shorter than the required minimum length.</summary>
    PasswordTooShort = -1001001,

    /// <summary>The password is missing an uppercase letter.</summary>
    PasswordMissingUppercase = -1001002,

    /// <summary>The password is missing a lowercase letter.</summary>
    PasswordMissingLowercase = -1001003,

    /// <summary>The password is missing a digit.</summary>
    PasswordMissingDigit = -1001004,

    /// <summary>The password is missing a special character.</summary>
    PasswordMissingSpecialCharacter = -1001005,

    /// <summary>The password and its confirmation do not match.</summary>
    PasswordNotTheSame = -1001006,

    /// <summary>The provided password is incorrect.</summary>
    IncorrectPassword = -1001007,

    /// <summary>A user with this email address already exists.</summary>
    UserWithThisEmailAlreadyExists = -1002000,

    /// <summary>No user matches the supplied token.</summary>
    NoUserWithThisToken = -1002001,

    /// <summary>No user matches the supplied email address.</summary>
    NoUserWithThisEmail = -1002002,

    /// <summary>The user account is already active.</summary>
    UserAlreadyActive = -1002003,

    /// <summary>The user account is blocked.</summary>
    UserIsBlocked = -1002004,

    /// <summary>The user account is deleted.</summary>
    UserIsDeleted = -1002005,

    /// <summary>The user account is still pending activation.</summary>
    UserPending = -1002006,

    /// <summary>No matching user was found.</summary>
    NoUserFound = -1002007,

    /// <summary>The update request contained no changed data.</summary>
    NoUpdatedData = -1002008,

    /// <summary>The registration token is invalid.</summary>
    InvalidRegistrationToken = -1003000,

    /// <summary>The registration token has expired.</summary>
    RegistrationTokenExpired = -1003001,

    /// <summary>The reset-password token is invalid.</summary>
    InvalidResetPasswordToken = -1004000,

    /// <summary>The reset-password token has expired.</summary>
    ResetPasswordTokenExpired = -1004001,

    /// <summary>No matching tags were found.</summary>
    NoTagsFound = -1005001,

    /// <summary>No matching taxonomy node was found.</summary>
    NoTaxonomyFound = -1006001,

    /// <summary>No matching species was found.</summary>
    SpeciesNotFound =  -1006002,

    /// <summary>The supplied address could not be resolved.</summary>
    AddressDoesNotExist =  -1006003,

    /// <summary>The referenced taxonomy node is not of rank species.</summary>
    TaxonomyRankIsNoSpecies = -1006004,

    /// <summary>No matching collection was found.</summary>
    CollectionNotFound = -1007001,

    /// <summary>No matching specimen was found.</summary>
    NoSpecimenFound = -1007002,

    /// <summary>The specimen is already part of the collection.</summary>
    SpecimenAlreadyInCollection = -1007003,

    /// <summary>The specimen is not part of the collection.</summary>
    SpecimenNotInCollection = -1007004,

    /// <summary>The taxonomy node has already been approved.</summary>
    TaxonomyAlreadyApproved = -1008001,

    /// <summary>The taxonomy node has already been rejected.</summary>
    TaxonomyAlreadyRejected = -1008002,

    /// <summary>No matching loan was found.</summary>
    LoanNotFound = -1009001,

    /// <summary>The loan has already been returned.</summary>
    LoanAlreadyReturned = -1009002,

    /// <summary>No matching loan request was found.</summary>
    LoanRequestNotFound = -1009003,

    /// <summary>The specimen already has an open loan request.</summary>
    SpecimenAlreadyRequested = -1009004,

    /// <summary>The specimen is already out on an active loan.</summary>
    SpecimenAlreadyOnLoan = -1009005,

    /// <summary>The loan request has already been approved or rejected.</summary>
    LoanRequestAlreadyDecided = -1009006,

    /// <summary>The caller is not allowed to decide this loan request.</summary>
    NotAllowedToDecideLoanRequest = -1009007,

    /// <summary>No matching image was found.</summary>
    ImageNotFound = -1010001,

    /// <summary>The target object is not in an active state.</summary>
    ObjectIsNotActive = -1011001,

    /// <summary>At least one notification channel must remain enabled.</summary>
    OneNotificationNeedsToBeActivated = -1011002,

    /// <summary>No settings were found for the user.</summary>
    UserSettingsNotFound = -1012001,

    /// <summary>The uploaded import file is invalid or unreadable.</summary>
    InvalidImportFile = -1013001
}
