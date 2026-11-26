namespace Underpin.SlotMachine.RNG
{
    /// <summary>
    /// Source of random numbers for the game. Game code only talks to this interface,
    /// so the real game can use a crypto RNG while tests use a seeded, repeatable one.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }
}
