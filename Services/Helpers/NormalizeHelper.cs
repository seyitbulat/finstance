namespace Finstance.Services.Helpers;



public static class NormalizerHelper
{
    public static string NormalizeTurkish(string text)
    {
        var normalized = string.Concat(text.ToUpperInvariant().Select(x =>
{
switch (x)
{
   case 'Ö': x = 'O'; break;
   case 'İ': x = 'I'; break;
   case 'Ü': x = 'U'; break;
   case 'Ş': x = 'S'; break;
   case 'Ç': x = 'C'; break;
   case 'Ğ': x = 'G'; break;
}
return x;
}));

        return normalized;
    }

}