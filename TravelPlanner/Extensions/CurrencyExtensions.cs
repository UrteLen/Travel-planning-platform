using System;
using System.Globalization;

namespace TravelPlanner.Extensions
{
    public static class CurrencyExtensions
    {
        public static string ToCurrencyString(this decimal amount, string cultureCode = "lt-LT")
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureCode);
            return amount.ToString("C", culture);
        }

        public static decimal ToShare(this decimal totalAmount, int numberOfPeople)
        {
            decimal share = totalAmount / numberOfPeople;
            return Math.Round(share, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal[] SplitEvenly(this decimal totalAmount, int numberOfPeople)
        {
            decimal[] shares = new decimal[numberOfPeople];
            decimal baseShare = Math.Floor(totalAmount / numberOfPeople * 100) / 100;
            decimal distributed = baseShare * numberOfPeople;
            decimal remainder = totalAmount - distributed;

            for (int i = 0; i < numberOfPeople; ++i)
            {
                shares[i] = baseShare;
            }

            int cents = (int)Math.Round(remainder * 100);
            for (int i = 0; i < cents; ++i)
            {
                shares[i] += 0.01m;
            }

            return shares;
        }
    }
}