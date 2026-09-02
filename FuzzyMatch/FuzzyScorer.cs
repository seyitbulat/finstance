

namespace Finstance.FuzzyMatch;


public class FuzzyScorer
{
    public int Distance(string a, string b)
    {
        int m = a.Length;
        int n = b.Length;

        var dp = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
        {
            dp[i, 0] = i;
        }

        for (int j = 0; j <= n; j++)
        {
            dp[0, j] = j;
        }
        dp[0, 0] = 0;


        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {

                if (a[i - 1] == b[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1];
                }
                else
                {
                    var leftUp = dp[i - 1, j - 1] + 1;
                    var up = dp[i - 1, j] + 1;
                    var left = dp[i, j - 1] + 1;

                    var min = Math.Min(leftUp, up);
                    min = Math.Min(min, left);

                    dp[i, j] = min;
                }

            }
        }

        return dp[m, n];
    }


    public int PartialRatio(string keyword, string text)
    {

        if (keyword.Length > text.Length)
        {
            (keyword, text) = (text, keyword);
        }

        var windowCount = text.Length - keyword.Length + 1;
        var size = keyword.Length;

        int min = int.MaxValue;
        for (int i = 0; i < windowCount; i++)
        {
            var word = text.Substring(i, size);
            var score = Distance(keyword, word);

            if (score < min)
                min = score;
        }

        if(keyword == "OBILET")
        {
            Console.WriteLine($"TEXT: {text} SCORE: {(int)((1.0 - (double)min / keyword.Length) * 100)}");
        }
        return (int)((1.0 - (double)min / keyword.Length) * 100);
    }
}