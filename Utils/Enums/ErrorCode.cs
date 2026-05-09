using System.ComponentModel;

namespace Zootify_Backend.Enums;

public enum ErrorCode
{
    None = 0,

    [Description("Leider gibt es momentan Server Probleme. Bitte melden Sie sich bei dem Support.")]
    DefaultErrorCode = -1000000,
    
    [Description(
        "Es gibt einen Fehler mit der Datanbank - Bitte melden Sie sich an den Support.")]
    DatabaseError = -1000001,
    
    [Description("Sie haben eine ungütlige Email Adresse mitgegeben. Bitte geben Sie ein gütlige Email Adresse ein.")]
    EmailError = -1001000,
    
    [Description("Das Passwort muss mindestens 8 Zeichen lang sein.")]
    PasswordTooShort = -1001001,

    [Description("Das Passwort muss mindestens einen Großbuchstaben enthalten.")]
    PasswordMissingUppercase = -1001002,

    [Description("Das Passwort muss mindestens einen Kleinbuchstaben enthalten.")]
    PasswordMissingLowercase = -1001003,

    [Description("Das Passwort muss mindestens eine Ziffer enthalten.")]
    PasswordMissingDigit = -1001004,

    [Description("Das Passwort muss mindestens ein Sonderzeichen enthalten.")]
    PasswordMissingSpecialCharacter = -1001005,
    
    [Description("Die Passwörter sind nicht identisch.")]
    PasswordNotTheSame = -1001006,
    
    [Description("Ihr Password ist falsch.")]
    IncorrectPassword = -1001007,
    
    [Description("Ein User mit dieser Email existiert bereits.")]
    UserWithThisEmailAlreadyExists = -1002000,
    
    [Description("Dieser User exisitert nicht.")]
    NoUserWithThisId = -1002001,
    
    [Description("Ein User mit dieser Email existiert nicht.")]
    NoUserWithThisEmail = -1002002,
    
    [Description("Der User ist schon aktiv.")]
    UserAlreadyActive = -1002003,
    
    [Description("Der User ist blockiert.")]
    UserIsBlocked = -1002004,
    
    [Description("Der User ist gelöscht.")]
    UserIsDeleted= -1002005,
    
    [Description("Der User ist nicht aktiv.")]
    UserPending = -1002006,

    [Description("Der Bestätigungslink ist ungültig.")]
    InvalidRegistrationToken = -1003000,
    
    [Description("Der Bestätigungslink ist abgelaufen. Bitte fordern Sie einen neuen an.")]
    RegistrationTokenExpired = -1003001,
    
    [Description("Ihr Link zum Password zurücksetzen ist ungültig.")]
    InvalidResetPasswordToken = -1004000,
    
    [Description("Ihr Link zum Password zurücksetzen ist abelaufen.")]
    ResetPasswordTokenExpired = -1004001,
}