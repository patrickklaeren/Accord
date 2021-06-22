namespace Accord.Bot.Extensions
{
    public static class UShortExtensions
    {
        public static string ToPaddedDiscriminator(this ushort input)
        {
            return input.ToString("0000");
        }
    }
}