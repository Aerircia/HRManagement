using System;
using System.Text.RegularExpressions;

namespace HRManagement.Utilities;

/// <summary>
/// Shared field-validation rules used by both the service layer (real
/// enforcement, cannot be bypassed) and ViewModels (instant UI feedback).
/// Keeping the rules here avoids the two layers drifting out of sync.
/// </summary>
public static class ValidationRules
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneDigitsRegex = new(
        @"^\d{10}$",
        RegexOptions.Compiled);

    // OWASP-recommended special character set for password composition rules.
    private const string SpecialCharacters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

    public const int MinimumAge = 18;
    public const int MinimumPasswordLength = 8;

    public static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
    }

    /// <summary>
    /// Phone must be exactly 10 digits. Formatting characters (spaces,
    /// dashes, parentheses) are not accepted - the raw value must be 10
    /// digits.
    /// </summary>
    public static bool IsValidPhone(string? phone)
    {
        return !string.IsNullOrWhiteSpace(phone) && PhoneDigitsRegex.IsMatch(phone.Trim());
    }

    public static bool IsValidHireDate(DateTime? hireDate)
    {
        return hireDate.HasValue && hireDate.Value.Date <= DateTime.Today;
    }

    public static bool IsValidBirthDate(DateTime? birthDate)
    {
        if (!birthDate.HasValue)
            return false;

        var minimumBirthDate = DateTime.Today.AddYears(-MinimumAge);
        return birthDate.Value.Date <= minimumBirthDate;
    }

    /// <summary>
    /// 8+ characters, at least one uppercase letter, at least one OWASP
    /// special character.
    /// </summary>
    public static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumPasswordLength)
            return false;

        var hasUpper = false;
        var hasSpecial = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c))
                hasUpper = true;
            else if (SpecialCharacters.IndexOf(c) >= 0)
                hasSpecial = true;

            if (hasUpper && hasSpecial)
                return true;
        }

        return false;
    }

    public const string EmailErrorMessage = "Please enter a valid email address.";
    public const string PhoneErrorMessage = "Phone number must be exactly 10 digits.";
    public const string HireDateErrorMessage = "Hire date cannot be in the future.";
    public const string BirthDateErrorMessage = "Employee must be at least 18 years old.";
    public const string PasswordErrorMessage = "Password must be at least 8 characters and include an uppercase letter and a special character.";
}
