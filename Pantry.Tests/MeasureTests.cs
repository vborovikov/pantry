namespace Pantry.Tests
{
    using System;
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
    }
}
