namespace Pantry.Tests;

using System;
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
}
