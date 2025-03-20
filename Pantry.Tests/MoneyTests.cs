namespace Pantry.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MoneyTests
    {
        [TestMethod]
        public void TryParse_RubleSignUnicodeSpace_CorrectSumRubCurrency()
        {
            var money = Money.Parse("3 744 ₽");
            Assert.AreEqual(3744.0m, money.Sum);
            Assert.AreEqual("RUB", money.Currency.Code);
        }

        [TestMethod]
        public void TryParse_SumSpace_RubCurrency()
        {
            Assert.IsTrue(Currency.TryParseMoneyExact("32 360", out var money));
            Assert.AreEqual(32360.0m, money.Sum);
            Assert.AreEqual("RUB", money.Currency.Code);
        }

        [TestMethod]
        public void TryParse_WritingFractions_Parsed()
        {
            Assert.IsTrue(Currency.TryParseMoneyExact("3 руб 15 коп", out var money));
            Assert.AreEqual(3.15m, money.Sum);
            Assert.AreEqual("RUB", money.Currency.Code);
        }

        [TestMethod]
        public void TryParse_WritingBrokenFraction_Parsed()
        {
            Assert.IsTrue(Currency.TryParseMoneyExact("3 руб 15", out var money));
            Assert.AreEqual(3.15m, money.Sum);
            Assert.AreEqual("RUB", money.Currency.Code);
        }

        [TestMethod]
        public void ToString_OneLira_NoFraction()
        {
            var lira = Money.Parse("₺1");

            Assert.AreEqual(1m, lira.Sum);
            Assert.AreEqual("TRY", lira.Currency.Code);
            Assert.AreEqual("₺1", lira.ToString());
        }

        [TestMethod]
        public void TryParse_SumTL_Parsed()
        {
            Assert.IsTrue(Money.TryParse("42000 tl", out var lira));

            Assert.AreEqual(42_000m, lira.Sum);
            Assert.AreEqual("TRY", lira.Currency.Code);
            Assert.AreEqual("₺42.000", lira.ToString());
        }

        [TestMethod]
        public void TryParse_TurkishNumberFormat_Parsed()
        {
            Assert.IsTrue(Money.TryParse("₺42.000", out var lira));

            Assert.AreEqual(42_000m, lira.Sum);
            Assert.AreEqual("TRY", lira.Currency.Code);
            Assert.AreEqual("₺42.000", lira.ToString());
        }

        [DataTestMethod]
        [DataRow("1000 руб.    ", 1000d, "RUB", "1 000 руб.")]
        [DataRow("    1000 руб.", 1000d, "RUB", "1 000 руб.")]
        [DataRow("    1000 руб.    ", 1000d, "RUB", "1 000 руб.")]
        public void TryParse_WhitespaceMoney_Parsed(string writing, double sum, string currencyCode, string text)
        {
            Assert.IsTrue(Money.TryParse(writing, out var rub));

            Assert.AreEqual((decimal)sum, rub.Sum);
            Assert.AreEqual(currencyCode, rub.Currency.Code);
            Assert.AreEqual(text, rub.ToString());
        }

        [DataTestMethod]
        [DataRow("10 AED", 10.0d, "AED")]
        [DataRow("1000 د.إ", 1000.0d, "AED")]
        [DataRow("5.50 د.إ", 5.50d, "AED")]
        [DataRow("10 AMD", 10.0d, "AMD")]
        [DataRow("1000 ֏", 1000.0d, "AMD")]
        [DataRow("5,50 ֏", 5.50d, "AMD")]
        [DataRow("10 AUD", 10.0d, "AUD")]
        [DataRow("1000 AU$", 1000.0d, "AUD")]
        [DataRow("5.50 A$", 5.50d, "AUD")]
        [DataRow("10 AZN", 10.0d, "AZN")]
        [DataRow("₼1000", 1000.0d, "AZN")]
        [DataRow("₼5,50", 5.50d, "AZN")]
        [DataRow("10 BGN", 10.0d, "BGN")]
        [DataRow("1000 лв", 1000.0d, "BGN")]
        [DataRow("5,50 лв", 5.50d, "BGN")]
        [DataRow("10 BRL", 10.0d, "BRL")]
        [DataRow("1000 R$", 1000.0d, "BRL")]
        [DataRow("5,50 R$", 5.50d, "BRL")]
        [DataRow("10 BYN", 10.0d, "BYN")]
        [DataRow("1000 Br", 1000.0d, "BYN")]
        [DataRow("5,50 Br", 5.50d, "BYN")]
        [DataRow("10 CAD", 10.0d, "CAD")]
        [DataRow("1000 CA$", 1000.0d, "CAD")]
        [DataRow("5.50 CA$", 5.50d, "CAD")]
        [DataRow("10 CHF", 10.0d, "CHF")]
        [DataRow("1000 CHF", 1000.0d, "CHF")]
        [DataRow("5.50 CHF", 5.50d, "CHF")]
        [DataRow("10 CNY", 10.0d, "CNY")]
        [DataRow("1000 ¥", 1000.0d, "CNY")]
        [DataRow("5.50 ¥", 5.50d, "CNY")]
        [DataRow("10 CZK", 10.0d, "CZK")]
        [DataRow("1000 Kč", 1000.0d, "CZK")]
        [DataRow("5,50 Kč", 5.50d, "CZK")]
        [DataRow("10 DKK", 10.0d, "DKK")]
        [DataRow("1000 kr", 1000.0d, "DKK")]
        [DataRow("5,50 kr", 5.50d, "DKK")]
        [DataRow("10 EGP", 10.0d, "EGP")]
        [DataRow("1000 ج.م.", 1000.0d, "EGP")]
        [DataRow("5٫50 ج.م.", 5.50d, "EGP")]
        [DataRow("10 EUR", 10.0d, "EUR")]
        [DataRow("1000 €", 1000.0d, "EUR")]
        [DataRow("5,50 €", 5.50d, "EUR")]
        [DataRow("10 GBP", 10.0d, "GBP")]
        [DataRow("£1000", 1000.0d, "GBP")]
        [DataRow("£5.50", 5.50d, "GBP")]
        [DataRow("10 GEL", 10.0d, "GEL")]
        [DataRow("1000 ₾", 1000.0d, "GEL")]
        [DataRow("5,50 ₾", 5.50d, "GEL")]
        [DataRow("10 HKD", 10.0d, "HKD")]
        [DataRow("1000 HK$", 1000.0d, "HKD")]
        [DataRow("5.50 HK$", 5.50d, "HKD")]
        [DataRow("10 HUF", 10.0d, "HUF")]
        [DataRow("1000 Ft", 1000.0d, "HUF")]
        [DataRow("5,50 Ft", 5.50d, "HUF")]
        [DataRow("10 IDR", 10.0d, "IDR")]
        [DataRow("1000 Rp", 1000.0d, "IDR")]
        [DataRow("5,50 Rp", 5.50d, "IDR")]
        [DataRow("10 ILS", 10.0d, "ILS")]
        [DataRow("1000 ₪", 1000.0d, "ILS")]
        [DataRow("5.50 ₪", 5.50d, "ILS")]
        [DataRow("10 INR", 10.0d, "INR")]
        [DataRow("1000 ₹", 1000.0d, "INR")]
        [DataRow("5.50 ₹", 5.50d, "INR")]
        [DataRow("10 JPY", 10.0d, "JPY")]
        [DataRow("￥1000", 1000.0d, "JPY")]
        [DataRow("￥5.50", 5.50d, "JPY")]
        [DataRow("10 KGS", 10.0d, "KGS")]
        [DataRow("1000 сом", 1000.0d, "KGS")]
        [DataRow("5,50 сом", 5.50d, "KGS")]
        [DataRow("10 KRW", 10.0d, "KRW")]
        [DataRow("1000 ₩", 1000.0d, "KRW")]
        [DataRow("5.50 ₩", 5.50d, "KRW")]
        [DataRow("10 KZT", 10.0d, "KZT")]
        [DataRow("1000 ₸", 1000.0d, "KZT")]
        [DataRow("5,50 ₸", 5.50d, "KZT")]
        [DataRow("10 MDL", 10.0d, "MDL")]
        [DataRow("1000 L", 1000.0d, "MDL")]
        [DataRow("5,50 L", 5.50d, "MDL")]
        [DataRow("10 MXN", 10.0d, "MXN")]
        [DataRow("1000 MX$", 1000.0d, "MXN")]
        [DataRow("5.50 MX$", 5.50d, "MXN")]
        [DataRow("10 NOK", 10.0d, "NOK")]
        [DataRow("1000 kr", 1000.0d, "NOK")]
        [DataRow("5,50 kr", 5.50d, "NOK")]
        [DataRow("10 NZD", 10.0d, "NZD")]
        [DataRow("1000 NZ$", 1000.0d, "NZD")]
        [DataRow("5.50 NZ$", 5.50d, "NZD")]
        [DataRow("10 PLN", 10.0d, "PLN")]
        [DataRow("1000 zł", 1000.0d, "PLN")]
        [DataRow("5,50 zł", 5.50d, "PLN")]
        [DataRow("10 QAR", 10.0d, "QAR")]
        [DataRow("1000 ر.ق", 1000.0d, "QAR")]
        [DataRow("5٫50 ر.ق", 5.50d, "QAR")]
        [DataRow("10 RUB", 10.0d, "RUB")]
        [DataRow("1000 ₽", 1000.0d, "RUB")]
        [DataRow("5,50 ₽", 5.50d, "RUB")]
        [DataRow("10 RON", 10.0d, "RON")]
        [DataRow("1000 lei", 1000.0d, "RON")]
        [DataRow("5,50 lei", 5.50d, "RON")]
        [DataRow("10 RSD", 10.0d, "RSD")]
        [DataRow("1000 дин", 1000.0d, "RSD")]
        [DataRow("5,50 дин", 5.50d, "RSD")]
        [DataRow("10 SEK", 10.0d, "SEK")]
        [DataRow("1000 kr", 1000.0d, "SEK")]
        [DataRow("5,50 kr", 5.50d, "SEK")]
        [DataRow("10 SGD", 10.0d, "SGD")]
        [DataRow("1000 S$", 1000.0d, "SGD")]
        [DataRow("5.50 S$", 5.50d, "SGD")]
        [DataRow("10 THB", 10.0d, "THB")]
        [DataRow("1000 ฿", 1000.0d, "THB")]
        [DataRow("5.50 ฿", 5.50d, "THB")]
        [DataRow("10 TJS", 10.0d, "TJS")]
        [DataRow("1000 ЅМ", 1000.0d, "TJS")]
        [DataRow("5,50 сом.", 5.50d, "TJS")]
        [DataRow("10 TMT", 10.0d, "TMT")]
        [DataRow("1000 m", 1000.0d, "TMT")]
        [DataRow("5,50 m", 5.50d, "TMT")]
        [DataRow("10 TRY", 10.0d, "TRY")]
        [DataRow("1000 ₺", 1000.0d, "TRY")]
        [DataRow("5,50 ₺", 5.50d, "TRY")]
        [DataRow("10 UAH", 10.0d, "UAH")]
        [DataRow("1000 ₴", 1000.0d, "UAH")]
        [DataRow("5,50 ₴", 5.50d, "UAH")]
        [DataRow("10 USD", 10.0d, "USD")]
        [DataRow("1000 $", 1000.0d, "USD")]
        [DataRow("5.50 $", 5.50d, "USD")]
        [DataRow("10 UZS", 10.0d, "UZS")]
        [DataRow("1000 сўм", 1000.0d, "UZS")]
        [DataRow("5,50 сўм", 5.50d, "UZS")]
        [DataRow("10 VND", 10.0d, "VND")]
        [DataRow("1000 ₫", 1000.0d, "VND")]
        [DataRow("5,50 ₫", 5.50d, "VND")]
        [DataRow("10 XDR", 10.0d, "XDR")]
        [DataRow("1000 SDR", 1000.0d, "XDR")]
        [DataRow("5.50 SDR", 5.50d, "XDR")]
        [DataRow("10 ZAR", 10.0d, "ZAR")]
        [DataRow("1000 R", 1000.0d, "ZAR")]
        [DataRow("5,50 R", 5.50d, "ZAR")]
        public void TryParse_Currency_Parsed(string writing, double sum, string currencyCode)
        {
            Assert.IsTrue(Money.TryParse(writing, out var money));
            Assert.AreEqual((decimal)sum, money.Sum);
            Assert.AreEqual(currencyCode, money.Currency.Code);
        }
    }
}
