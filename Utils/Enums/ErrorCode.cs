using System.ComponentModel;

namespace Zootify_Backend.Enums;

public enum ErrorCode
{
    None = 0,

    [Description("Leider gibt es momentan Server Probleme. Bitte melden Sie sich bei ihrem Dozenten.")]
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
    
    [Description("Ein User mit dieser Email existiert bereits.")]
    UserWithThisEmailAlreadyExists = -1002000,
    
    [Description("Dieser User exisitert nicht.")]
    NoUserWithThisId = -1002001,
    
    [Description("Der Bestätigungslink ist ungültig.")]
    InvalidRegistrationToken = -1003000,
    
    [Description("Der Bestätigungslink ist abgelaufen. Bitte fordern Sie einen neuen an.")]
    RegistrationTokenExpired = -1003001,
}