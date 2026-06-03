using System.Collections.Generic;

namespace BruteForceApp
{
    /// <summary>
    /// Generates all character combinations from length 1 up to maxLength.
    /// Does not know the target password length — searches blindly.
    /// </summary>
    public class BruteForceGenerator
    {
        private readonly string _charset;
        private readonly int _maxLength;

        public BruteForceGenerator(int maxLength = 6)
        {
            _charset = PasswordManager.AllCharacters;
            _maxLength = maxLength;
        }

        /// <summary>Number of characters in the alphabet.</summary>
        public int CharsetLength => _charset.Length;

        /// <summary>
        /// Maps a numeric index in [0, charset^length) to its combination of the given
        /// length, treating the index as a base-N number (N = alphabet size).
        /// Lets threads each cover a contiguous slice of the search space on the fly,
        /// with no need to materialise the whole list of candidates in memory.
        /// </summary>
        public string IndexToCombination(long index, int length)
        {
            int n = _charset.Length;
            var chars = new char[length];
            for (int i = length - 1; i >= 0; i--)
            {
                chars[i] = _charset[(int)(index % n)];
                index /= n;
            }
            return new string(chars);
        }

        /// <summary>
        /// Enumerates every combination from length 1 to maxLength in order.
        /// </summary>
        public IEnumerable<string> GenerateAll()
        {
            for (int length = 1; length <= _maxLength; length++)
                foreach (var combo in GenerateOfLength(length))
                    yield return combo;
        }

        /// <summary>
        /// Enumerates all combinations of exactly the given length.
        /// </summary>
        public IEnumerable<string> GenerateOfLength(int length)
        {
            int[] indices = new int[length];
            int charCount = _charset.Length;

            while (true)
            {
                // Build candidate from current index state
                var chars = new char[length];
                for (int i = 0; i < length; i++)
                    chars[i] = _charset[indices[i]];
                yield return new string(chars);

                // Advance indices (like incrementing a base-N number)
                int pos = length - 1;
                while (pos >= 0)
                {
                    indices[pos]++;
                    if (indices[pos] < charCount)
                        break;
                    indices[pos] = 0;
                    pos--;
                }
                if (pos < 0)
                    yield break;
            }
        }

        /// <summary>
        /// Returns the total number of candidates for a given length.
        /// </summary>
        public long CombinationsForLength(int length)
        {
            long total = 1;
            for (int i = 0; i < length; i++)
                total *= _charset.Length;
            return total;
        }

        /// <summary>
        /// Total candidates from length 1 through maxLength.
        /// </summary>
        public long TotalCombinations()
        {
            long total = 0;
            for (int l = 1; l <= _maxLength; l++)
                total += CombinationsForLength(l);
            return total;
        }
    }
}
