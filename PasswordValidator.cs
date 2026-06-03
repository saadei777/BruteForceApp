namespace BruteForceApp
{
    /// <summary>
    /// Validates a candidate string against a stored SHA256 hash.
    /// Operates independently from the generator — receives only the hash to check against.
    /// </summary>
    public class PasswordValidator
    {
        private readonly string _targetHash;

        public PasswordValidator(string targetHash)
        {
            _targetHash = targetHash;
        }

        /// <summary>
        /// Returns true when the candidate's hash matches the target hash.
        /// </summary>
        public bool IsMatch(string candidate)
        {
            string hash = PasswordManager.HashPassword(candidate);
            return hash == _targetHash;
        }
    }
}
