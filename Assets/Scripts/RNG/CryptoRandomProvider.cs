using System;
using System.Security.Cryptography;

namespace Underpin.SlotMachine.RNG
{
    /// <summary>
    /// RNG used for real spins.
    ///
    /// Two things matter for a fair slot:
    /// 1. Unpredictability - we use the OS cryptographic generator, so results can't be
    ///    guessed from previous spins (UnityEngine.Random / System.Random can be).
    /// 2. No modulo bias - "random % n" slightly favours small numbers when 2^32 is not a
    ///    multiple of n. We throw away draws from the uneven tail (rejection sampling), so
    ///    every reel stop is exactly equally likely.
    ///
    /// If the platform has no crypto generator, we fall back to a well-seeded System.Random
    /// so the game still runs (with the same bias-free Range logic).
    /// </summary>
    public sealed class CryptoRandomProvider : IRandomProvider, IDisposable
    {
        private const ulong UInt32Space = 1UL << 32;

        private readonly RandomNumberGenerator cryptoGenerator;
        private readonly Random fallbackGenerator;
        private readonly byte[] buffer = new byte[4];

        public bool IsUsingCryptoSource => cryptoGenerator != null;

        public CryptoRandomProvider()
        {
            try
            {
                cryptoGenerator = RandomNumberGenerator.Create();
                cryptoGenerator.GetBytes(buffer); // fail early here, not in the middle of a spin
            }
            catch (Exception)
            {
                cryptoGenerator = null;
                fallbackGenerator = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
            }
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentException("maxExclusive must be greater than minInclusive");
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);

            // Largest multiple of `range` that fits in 32 bits. Draws at or above it are rejected.
            ulong limit = UInt32Space - (UInt32Space % range);

            ulong draw;
            do
            {
                draw = NextUInt32();
            }
            while (draw >= limit);

            return (int)(minInclusive + (long)(draw % range));
        }

        private uint NextUInt32()
        {
            if (cryptoGenerator != null)
            {
                cryptoGenerator.GetBytes(buffer);
            }
            else
            {
                fallbackGenerator.NextBytes(buffer);
            }

            return BitConverter.ToUInt32(buffer, 0);
        }

        public void Dispose()
        {
            cryptoGenerator?.Dispose();
        }
    }
}
