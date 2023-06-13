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
    }
}
