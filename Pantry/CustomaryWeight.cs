namespace Pantry
{
    // https://en.wikipedia.org/wiki/Avoirdupois_system#American_customary_system

    /// <summary>
    /// Represents weight in US dry ounces.
    /// </summary>
    public class CustomaryWeight : CustomaryUnit
    {
        private readonly string[] ounces = { "oz", "ozs", "ounce", "ounces", };
        private readonly string[] pounds = { "lb", "lbs", "pound", "pounds", };

        public override string Name => "US Customary Weight";
        public override string Symbol => "oz";
        public override MeasurementType Type => MeasurementType.Weight;

        public CustomaryWeight()
        {
            Add(this.ounces, 1f, 0f, 8f, this.ounces[0], this.ounces[1]);
            Add(this.pounds, 16f, 8f, 1600f, this.pounds[0], this.pounds[1]);
        }
    }
}