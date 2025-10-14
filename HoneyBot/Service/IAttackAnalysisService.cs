namespace HoneyBot.Service
{
    public interface IAttackAnalysisService
    {
        string AnalyzeRequest(string combinedInput, string userAgent);
    }
}
