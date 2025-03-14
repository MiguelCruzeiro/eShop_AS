using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class MaskSensitiveDataProcessor : BaseProcessor<Activity>
{
    private static readonly Regex EmailRegex = new(@"^([^@]+)@(.+)$", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"^(\d{6})(\d+)(\d{4})$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^(\+\d{1,3}|\d{1,4})(\d+)(\d{2,4})$", RegexOptions.Compiled);
    private static readonly Regex IpAddressRegex = new(@"^(\d{1,3}\.\d{1,3})(\.\d{1,3}\.\d{1,3})$", RegexOptions.Compiled);
    private static readonly Regex CreditCardNumberRegex = new(@"^\d{13,19}$", RegexOptions.Compiled);

    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.TagObjects)
        {
            if (tag.Key.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, MaskEmail(tag.Value?.ToString() ?? ""));
            }
            else if (tag.Key.Contains("user.id", StringComparison.OrdinalIgnoreCase) || 
                     tag.Key.Contains("customer.id", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, MaskUserId(tag.Value?.ToString() ?? ""));
            }
            else if (tag.Key.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, MaskPhone(tag.Value?.ToString() ?? ""));
            }
            else if (tag.Key.Contains("card", StringComparison.OrdinalIgnoreCase) || 
                     tag.Key.Contains("credit", StringComparison.OrdinalIgnoreCase) ||
                     tag.Key.Contains("payment", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, MaskCreditCard(tag.Value?.ToString() ?? ""));
            }
            else if (tag.Key.Contains("address", StringComparison.OrdinalIgnoreCase) || 
                     tag.Key.Contains("ip", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, MaskIpAddress(tag.Value?.ToString() ?? ""));
            }
            else if (tag.Key.Contains("password", StringComparison.OrdinalIgnoreCase) || 
                     tag.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) || 
                     tag.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                     tag.Key.Contains("key", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, "********");
            }
            else if (tag.Value is string valueStr)
            {
                if (EmailRegex.IsMatch(valueStr))
                {
                    activity.SetTag(tag.Key, MaskEmail(valueStr));
                }
                else if (IsCreditCardNumber(valueStr))
                {
                    activity.SetTag(tag.Key, MaskCreditCard(valueStr));
                }
            }
        }
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email;

        var match = EmailRegex.Match(email);
        if (!match.Success)
            return email;

        string username = match.Groups[1].Value;
        string domain = match.Groups[2].Value;
        
        if (username.Length == 0)
            return $"@{domain}";
            
        return $"{username[0]}{new string('*', username.Length - 1)}@{domain}";
    }

    private string MaskUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId) || userId.Length <= 4)
            return "****";
            
        return $"{new string('*', userId.Length - 4)}{userId.Substring(userId.Length - 4)}";
    }

    private string MaskCreditCard(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
            return "************";
            
        cardNumber = cardNumber.Replace(" ", "").Replace("-", "");
        
        if (cardNumber.Length < 10)
            return "************";
            
        var match = CreditCardRegex.Match(cardNumber);
        if (!match.Success)
        {
            if (cardNumber.Length >= 10)
                return $"{cardNumber.Substring(0, 6)}{new string('*', cardNumber.Length - 10)}{cardNumber.Substring(cardNumber.Length - 4)}";
            else
                return "************";
        }
        
        return $"{match.Groups[1].Value}{new string('*', match.Groups[2].Value.Length)}{match.Groups[3].Value}";
    }

    private string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 7)
            return phone;
            
        var match = PhoneRegex.Match(phone);
        if (!match.Success)
            return $"{new string('*', phone.Length - 4)}{phone.Substring(phone.Length - 4)}";
        
        return $"{match.Groups[1].Value}{new string('*', match.Groups[2].Value.Length)}{match.Groups[3].Value}";
    }

    private string MaskIpAddress(string ip)
    {
        if (string.IsNullOrEmpty(ip))
            return ip;
            
        var match = IpAddressRegex.Match(ip);
        if (!match.Success)
            return ip;
        
        return $"{match.Groups[1].Value}.*.*";
    }

    private bool IsCreditCardNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
            
        value = value.Replace(" ", "").Replace("-", "");
        
        if (!CreditCardNumberRegex.IsMatch(value))
            return false;
        
        return true;
    }
}