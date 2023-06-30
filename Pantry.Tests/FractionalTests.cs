namespace Pantry.Tests;

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FractionalTests
{
    [TestMethod]
    public void Equals_WithDefault_Equality()
    {
        var other = (Fractional)0.5f;

        EqualityTests.TestEqualObjects(default, new Fractional());
        EqualityTests.TestUnequalObjects(default, other);
        EqualityTests.TestAgainstNull(other);
    }

    [TestMethod]
    public void Equals_TwoValues_NotEqual()
    {
        var f1 = Fractional.Parse("1/2");
        var f2 = Fractional.Parse("1/3");

        EqualityTests.TestUnequalObjects(f1, f2);
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void Parse_GibberishText_NotParsed()
    {
        Assert.AreEqual(default, Fractional.Parse("Abracadabra"));
    }

    [TestMethod]
    public void Equals_ParseToString_Equality()
    {
        var fraction = Fractional.Parse("1/7");
        var parsedFraction = Fractional.Parse(fraction.ToString());

        EqualityTests.TestEqualObjects(fraction, parsedFraction);
    }

    [TestMethod]
    public void ToString_DefaultFormat_RegularDigitsVirgule()
    {
        var fraction = Fractional.Parse("1 1/2");
        Assert.AreEqual("1 1/2", fraction.ToString());
    }

    [TestMethod]
    public void ToString_GeneralFormat_RegularDigitsVirgule()
    {
        var fraction = Fractional.Parse("1 1/2");
        Assert.AreEqual("1 1/2", fraction.ToString("G"));
        Assert.AreEqual("1 1/2", fraction.ToString("g"));
    }

    [TestMethod]
    public void ToString_NumberFormat_SubSupDigitsSolidus()
    {
        var fraction = Fractional.Parse("1 1/2");
        Assert.AreEqual("1¹⁄₂", fraction.ToString("N"));
        Assert.AreEqual("1¹⁄₂", fraction.ToString("n"));
    }

    [TestMethod]
    public void ToString_VulgarFormat_VulgarsIfPossible()
    {
        var fraction = Fractional.Parse("1 1/2");
        Assert.AreEqual("1½", fraction.ToString("V"));
        Assert.AreEqual("1½", fraction.ToString("v"));
    }

    [TestMethod]
    public void VulgarFraction_Parsed()
    {
        var writing = "⅑";
        var result = Fractional.TryParse(writing, out var fraction);

        Assert.IsTrue(result);
        Assert.AreEqual(1, fraction.Numerator);
        Assert.AreEqual(9, fraction.Denominator);
    }

    [TestMethod]
    public void Fraction_ObliqueBarWithInteger_Parsed()
    {
        var writing = "5 3/4";
        var result = Fractional.TryParse(writing, out var fraction);

        Assert.IsTrue(result);
        Assert.AreEqual(23, fraction.Numerator);
        Assert.AreEqual(4, fraction.Denominator);
        Assert.AreEqual("5³⁄₄", fraction.ToString("N"));
    }

    [TestMethod]
    public void Fraction_DiagonalBarWithInteger_Parsed()
    {
        var writing = "5³⁄₄";
        var result = Fractional.TryParse(writing, out var fraction);

        Assert.IsTrue(result);
        Assert.AreEqual(23, fraction.Numerator);
        Assert.AreEqual(4, fraction.Denominator);
        Assert.AreEqual("5³⁄₄", fraction.ToString("N"));
    }

    [TestMethod]
    public void Fractional_Default_Equality()
    {
        var fraction = (Fractional)0.5f;

        EqualityTests.TestEqualObjects(default, new Fractional());
        EqualityTests.TestUnequalObjects(default, fraction);
        EqualityTests.TestAgainstNull(fraction);
    }

    [TestMethod]
    public void Fractional_TwoValues_NotEqual()
    {
        var f1 = (Fractional)1.5f;
        var f2 = (Fractional)3.15f;

        EqualityTests.TestUnequalObjects(f1, f2);
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void Fractional_GibberishText_NotParsed()
    {
        Assert.IsFalse(Fractional.TryParse("Abracadabra", out var gibberish));
        Assert.AreEqual(default, gibberish);

        Assert.AreEqual(default, Fractional.Parse("Abracadabra"));
    }

    [TestMethod]
    public void Fractional_FractionString_Parsed()
    {
        var fraction = (Fractional)0.5;
        var parsedFraction = Fractional.Parse(fraction.ToString());

        EqualityTests.TestEqualObjects(fraction, parsedFraction);
    }

    [TestMethod]
    public void Fractional_DecimalPoint_RealNumber()
    {
        var fraction = Fractional.Parse(".5", CultureInfo.InvariantCulture);
        Assert.AreEqual(0.5f, fraction);
    }

    [TestMethod]
    public void Fractional_DashAtEnd_FractionFound()
    {
        Assert.IsTrue(Fractional.TryParse("3/4-inch", null, out var fraction, out var length));

        Assert.AreEqual(4, length);
        Assert.AreEqual(Fractional.Parse("³⁄₄"), fraction);
    }
}
