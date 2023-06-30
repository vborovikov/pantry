namespace Pantry.Tests
{
    using System;
    using System.Globalization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MeasureTests
    {
        [TestMethod]
        public void TryParseMeasure_ByNumberUnit_Parsed()
        {
            var num = new Measure(24f, MeasureUnit.Number);
            Assert.IsTrue(num.Unit.TryParseMeasure("1", out var parsed));
        }

        [TestMethod]
        public void Equals_WithDefault_Equality()
        {
            var other = new Measure(1, MeasureUnit.Number);

            EqualityTests.TestEqualObjects(default, new Measure());
            EqualityTests.TestUnequalObjects(default, other);
            EqualityTests.TestAgainstNull(other);
        }

        [TestMethod]
        public void Equals_TwoValues_NotEqual()
        {
            var q1 = Measure.Parse("1 cup");
            var q2 = Measure.Parse("2 шт");

            EqualityTests.TestUnequalObjects(q1, q2);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Parse_GibberishText_NotParsed()
        {
            Assert.AreEqual(default, Measure.Parse("Abracadabra"));
        }

        [TestMethod]
        public void Equals_ParseToString_Equality()
        {
            var quantity = Measure.Parse("10 мл");
            var parsedQuantity = Measure.Parse(quantity.ToString());

            EqualityTests.TestEqualObjects(quantity, parsedQuantity);
        }

        [TestMethod]
        public void ToString_DefaultFormat_LargestUnitsUsed()
        {
            var twoCups = Measure.Parse("2 cups");
            Assert.AreEqual("2 cups", twoCups.ToString());
        }

        [TestMethod]
        public void Parse_2T_ParsedAsTablespoons()
        {
            var twoTbsps = Measure.Parse("2T");
            Assert.AreEqual("2 tbsps", twoTbsps.ToString());
        }

        [TestMethod]
        public void Parse_2t_ParsedAsTeaspoons()
        {
            var twoTsps = Measure.Parse("2t");
            Assert.AreEqual("2 tsps", twoTsps.ToString());
        }

        [DataTestMethod]
        [DataRow("1 шт", "1-")]
        [DataRow("2 pcs", "2-")]
        [DataRow("7", "7-")]
        public void ToString_NumbersNoCulture_MinusSignAtEnd(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Assert.AreEqual(expected, q.ToString(null, null));
        }

        [DataTestMethod]
        [DataRow("1 шт", "1-")]
        [DataRow("2 pcs", "2-")]
        [DataRow("7", "7-")]
        public void TryFormat_NumbersNoCulture_MinusSignAtEnd(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Span<char> buffer = stackalloc char[25];
            Assert.IsTrue(q.TryFormat(buffer, out var charsWritten, "", null));
            Assert.AreEqual(expected, buffer[..charsWritten].ToString());
        }

        [DataTestMethod]
        [DataRow("1 %", "1%")]
        [DataRow("2%", "2%")]
        public void ToString_PercentNoCulture_PercentSignAtEnd(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Assert.AreEqual(expected, q.ToString(null, null));
        }

        [DataTestMethod]
        [DataRow("1 %", "1%")]
        [DataRow("2%", "2%")]
        public void TryFormat_PercentNoCulture_PercentSignAtEnd(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Span<char> buffer = stackalloc char[25];
            Assert.IsTrue(q.TryFormat(buffer, out var charsWritten, "", null));
            Assert.AreEqual(expected, buffer[..charsWritten].ToString());
        }

        [DataTestMethod]
        [DataRow("1 large pkg", "1-")]
        [DataRow("1 piece", "1-")]
        [DataRow("2 cans", "2-")]
        public void TryParse_NumberOfItems_NumberMeasureUnit(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Assert.AreEqual(expected, q.ToString(null, null));
        }

        [DataTestMethod]
        [DataRow("one large pkg", "1-")]
        [DataRow("two eggs", "2-")]
        [DataRow("a hundred cans of beer", "100-")]
        public void TryParse_NumberWords_NumberMeasureUnit(string writing, string expected)
        {
            var q = Measure.Parse(writing);
            Assert.AreEqual(expected, q.ToString(null, null));
        }

        [TestMethod]
        public void Measure_MetricHundredGram_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("100 грамм", out var quantity));
            Assert.AreEqual(100f, quantity.Value.Value);
            Assert.AreEqual("100 гр", quantity.ToString());
        }

        [TestMethod]
        public void Measure_MetricHundredGramDecimalPointRus_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("100,00 грамм", CultureInfo.GetCultureInfo("ru-RU"), out var quantity));
            Assert.AreEqual(100f, quantity.Value.Value);
            Assert.AreEqual("100 гр", quantity.ToString());
        }

        [TestMethod]
        public void Measure_MetricHundredGramDecimalPointEng_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("100.00 gram)", CultureInfo.GetCultureInfo("en-US"), out var quantity));
            Assert.AreEqual(100f, quantity.Value.Value);
            Assert.AreEqual("100 gr", quantity.ToString());
        }

        [TestMethod]
        public void Measure_OneCup_8floz()
        {
            Assert.IsTrue(Measure.TryParse("1 cup", out var quantity));
            Assert.AreEqual(8f, quantity.Value.Value);
            Assert.AreEqual("1 cup", quantity.ToString());
        }

        [TestMethod]
        public void Measure_TwoCups_16floz()
        {
            Assert.IsTrue(Measure.TryParse("2 c.", out var quantity));
            Assert.AreEqual(16f, quantity.Value.Value);
            Assert.AreEqual("2 cups", quantity.ToString());
        }

        [TestMethod]
        public void Measure_OneAndHalfCups_12floz()
        {
            Assert.IsTrue(Measure.TryParse("1 1/2 cups", out var quantity));
            Assert.AreEqual(12f, quantity.Value.Value);
            Assert.AreEqual("1 1/2 cups", quantity.ToString());
        }

        [TestMethod]
        public void Measure_OneFluidOunce_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("1 fl. oz.", out var quantity));
            Assert.AreEqual(1f, quantity.Value.Value);
            Assert.AreEqual("2 tbsps", quantity.ToString());
        }

        [TestMethod]
        public void Measure_OneFluidOunceUnderscore_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("1 fl_oz", out var quantity));
            Assert.AreEqual(1f, quantity.Value.Value);
            Assert.AreEqual("2 tbsps", quantity.ToString());
        }

        [TestMethod]
        public void Measure_Default_Equality()
        {
            var quantity = new Measure(1f, MeasureUnit.Number);

            EqualityTests.TestEqualObjects(default, new Measure());
            EqualityTests.TestUnequalObjects(default, quantity);
            EqualityTests.TestAgainstNull(quantity);
        }

        [TestMethod]
        public void Measure_ThreeValues_NotEqual()
        {
            var m1 = new Measure(1f, MeasureUnit.Number); 
            var m2 = new Measure(2f, MeasureUnit.Number);
            var m3 = Measure.Parse("2 cups");

            EqualityTests.TestUnequalObjects(m1, m2);
            EqualityTests.TestUnequalObjects(m1, m3);
            EqualityTests.TestUnequalObjects(m2, m3);
        }

        [TestMethod]
        public void Measure_GibberishText_NotParsed()
        {
            Assert.IsFalse(Measure.TryParse("Abracadabra", out var gibberish));
            Assert.AreEqual(default, gibberish);
        }

        [TestMethod]
        public void Measure_Text_Parsed()
        {
            var quantity = new Measure(1f, MeasureUnit.Number);
            var parsedQuantity = Measure.Parse(quantity.ToString());

            EqualityTests.TestEqualObjects(quantity, parsedQuantity);
        }

        [TestMethod]
        public void Measure_FractionWithAmpersand_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("1 & 1/2 Tablespoon", out var measure));
            Assert.AreEqual("1 1/2 tbsps", measure.ToString());
        }

        [TestMethod]
        public void Measure_FractionWithDash_Parsed()
        {
            Assert.IsTrue(Measure.TryParse("2-1/2 cups", out var measure));
            Assert.AreEqual("2 1/2 cups", measure.ToString());
        }
    }
}
