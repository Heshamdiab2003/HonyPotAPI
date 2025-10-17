using System.Text.RegularExpressions;

namespace HoneyBot.Service
{
    public class AttackAnalysisService : IAttackAnalysisService
    {
        public string AnalyzeRequest(string combinedInput, string userAgent)
        {
            Console.WriteLine($"[DEBUG] Analyzing input: {combinedInput.Substring(0, Math.Min(100, combinedInput.Length))}...");
            
            // More specific SQLi detection - avoid matching HTML tags
            if (Regex.IsMatch(combinedInput, @"(^|[^<])(\b(union\s+all\s+select|union\s+select|select\s+.+\s+from|insert\s+into|update\s+.+\s+set|delete\s+from|drop\s+table|exec\s|execute\s|char\s*\(|nchar\s*\(|varchar\s*\(|cast\s*\(|convert\s*\()\b|\b(OR|AND)\b\s+\d\s*=\s*\d|\b(OR|AND)\b\s+'\w*'\s*=\s*'\w*'|0x[0-9a-fA-F]+|'[^']*'[^<]*--|""[^""]*""[^<]*--|`[^`]*`[^<]*--)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Console.WriteLine("[DEBUG] Detected: SQL Injection");
                return "SQL Injection";
            }

            // Broaden XSS detection: handle event handlers, img/svg/script, js: URIs, and common alert patterns
            if (Regex.IsMatch(combinedInput, @"(<script|<img\s|<iframe|<svg|onerror\s*=|onload\s*=|onclick\s*=|javascript:\s*|data:\s*text\/html|alert\s*\(|prompt\s*\(|confirm\s*\()", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Console.WriteLine("[DEBUG] Detected: Cross-Site Scripting (XSS)");
                return "Cross-Site Scripting (XSS)";
            }

            if (combinedInput.Contains("../") || combinedInput.Contains("..\\"))
            {
                Console.WriteLine("[DEBUG] Detected: Directory Traversal / LFI");
                return "Directory Traversal / LFI";
            }

            // More specific file upload detection - only match actual filenames in form data or file uploads, not URL paths
            if (Regex.IsMatch(combinedInput, @"(filename=|name=.*\.(jsp?|html?|svg|php\d*|aspx?|exe|sh|bat|ps1|jar|war|ear|cgi|pl)\b|\b\w+\.(jsp?|php\d*|aspx?|exe|sh|bat|ps1|jar|war|ear|cgi|pl)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Console.WriteLine("[DEBUG] Detected: Malicious File Upload");
                return "Malicious File Upload";
            }

            if (Regex.IsMatch(userAgent, @"(sqlmap|nmap|nikto|acunetix|burp|nessus|feroxbuster)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Console.WriteLine("[DEBUG] Detected: Automated Tool Scan");
                return "Automated Tool Scan";
            }

            Console.WriteLine("[DEBUG] Detected: Normal");
            return "Normal";
        }
    }
}