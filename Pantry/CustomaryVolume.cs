namespace Pantry
{
    using System;

    // https://en.wikipedia.org/wiki/Cooking_weights_and_measures#United_States_measures

    /// <summary>
    /// Represents volume in US fluid ounces.
    /// </summary>
    public class CustomaryVolume : CustomaryUnit
    {
        private readonly string[] drops = { "dr", "gt", "gtt", "drop", "drops", };
        private readonly string[] smidgens = { "smi", "smdg", "smidgen", "smidgens", };
        private readonly string[] pinches = { "pn", "pinch", "pinches", };
        private readonly string[] dashes = { "ds", "dash", "dashes", };
        private readonly string[] saltspoons = { "ssp", "saltspoon", "saltspoons", "scruple", "scruples", };
        private readonly string[] coffeespoons = { "csp", "coffeespoon", "coffeespoons", };
        private readonly string[] fluidDrams = { "fl dr", "fluid dram", "fluid drams", };
        private readonly string[] teaspoons = { "t", "tsp", "tsps", "teasp", "teasps", "teaspn", "teaspns", "teaspoon", "teaspoons", };
        private readonly string[] dessertspoons = { "dsp", "dssp", "dstspn", "dessertspoon", "dessertspoons", };
        private readonly string[] tablespoons = { "T", "tbsp", "tbsps", "tblsp", "tblsps", "tablespoon", "tablespoons", };
        private readonly string[] fluidOunces = { "fl oz", "fl ozs", "fluid ounce", "fluid ounces", };
        private readonly string[] wineglasses = { "wgf", "glass", "glasses", "wineglass", "wineglasses", };
        private readonly string[] teacups = { "tcf", "gill", "teacup", "teacups", };
        private readonly string[] cups = { "c", "C", "cup", "cups" };
        private readonly string[] pints = { "pt", "pts", "pint", "pints", };
        private readonly string[] quarts = { "qt", "qts", "quart", "quarts", };
        private readonly string[] pottles = { "pot", "pottle", "pottles", };
        private readonly string[] gallons = { "gal", "gallon", "gallons", };

        public CustomaryVolume()
        {
             Add(this.drops, 1f/576f);
             Add(this.smidgens, 1f/256f);
             Add(this.pinches, 1f/128f);
             Add(this.dashes, 1f/64f);
             Add(this.saltspoons, 1f/32f);
             Add(this.coffeespoons, 1f/16f);
             Add(this.fluidDrams, 1f/8f);
             Add(this.teaspoons, 1f/6f, 0f, Single.Round(1f/3f, 6, MidpointRounding.ToPositiveInfinity), this.teaspoons[1], this.teaspoons[2]);
             Add(this.dessertspoons, 1f/3f);
             Add(this.tablespoons, 1f/2f, Single.Round(1f/3f, 6, MidpointRounding.ToPositiveInfinity), 4f, this.tablespoons[1], this.tablespoons[2]);
             Add(this.fluidOunces, 1f);
             Add(this.wineglasses, 2f);
             Add(this.teacups, 4f);
             Add(this.cups, 8f, 4f, 64f);
             Add(this.pints, 16f);
             Add(this.quarts, 32f);
             Add(this.pottles, 64f);
             Add(this.gallons, 128f, 64f, 12800f, this.gallons[0], this.gallons[0]);
        }

        public override string Name => "US Customary Volume";
        public override string Symbol => "fl.oz.";
        public override MeasurementType Type => MeasurementType.Volume;
    }
}