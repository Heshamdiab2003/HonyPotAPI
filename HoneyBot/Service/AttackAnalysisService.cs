using System.Text.RegularExpressions;

namespace HoneyBot.Service;

public class AttackAnalysisService : IAttackAnalysisService
{
    public string AnalyzeRequest(string combinedInput, string userAgent)
    {
        // Using Regex for more accurate, case-insensitive matching
        if (Regex.IsMatch(combinedInput, @"('|--|;|\b(union|select|insert|update|delete|drop|exec|char|cast)\b)", RegexOptions.IgnoreCase))
            return "SQL Injection";

        if (Regex.IsMatch(combinedInput, @"(<script|<img\s|onerror=|onload=|<iframe>|<svg|alert\()", RegexOptions.IgnoreCase))
            return "Cross-Site Scripting (XSS)";

        if (combinedInput.Contains("../") || combinedInput.Contains("..\\"))
            return "Directory Traversal / LFI";

        if (Regex.IsMatch(combinedInput, @"\.(js|html|svg|php|exe|sh|jar)\b", RegexOptions.IgnoreCase))
            return "Malicious File Upload";

        if (Regex.IsMatch(userAgent, @"(sqlmap|nmap|nikto|acunetix|burp|nessus|feroxbuster)", RegexOptions.IgnoreCase))
            return "Automated Tool Scan";

        return "Normal"; // Default if no attack signature is found
    }
}