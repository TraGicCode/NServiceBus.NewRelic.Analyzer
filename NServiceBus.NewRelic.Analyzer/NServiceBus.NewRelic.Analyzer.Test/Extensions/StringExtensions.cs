namespace NServiceBus.NewRelic.Analyzer.Test.Extensions
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndings(this string input)
        {
            if (input.Contains("\n") && !input.Contains("\r\n"))
            {
                input = input.Replace("\n", "\r\n");
            }

            return input;
        }
    }
}