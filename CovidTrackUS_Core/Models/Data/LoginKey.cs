using Dapper.Contrib.Extensions;
using System;
using System.Text;

namespace CovidTrackUS_Core.Models.Data
{
    /// <summary>
    /// Random code to generate for a subscriber to do one time login
    /// </summary>
    [Table("LoginKey")]
    public class LoginKey : CovidTrackDO
    {
        public string Kee { get; set; }
        public string Handle { get; set; }
        public DateTime ExpiresOn { get; set; }

        public static LoginKey GenerateFor(string handle)
        {
            var oneTimeKey = RandomishId.Generate();
            var newLoginKey = new LoginKey()
            {
                Handle = handle,
                Kee = oneTimeKey,
                ExpiresOn = DateTime.Now.AddMinutes(10),
            };
            return newLoginKey;

        }

        /// <summary>
        /// Create a short case sensitive key for a login key
        /// </summary>
        private static class RandomishId
        {
            private static char[] _base62chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            private static Random _random = new Random(Guid.NewGuid().GetHashCode() - Environment.TickCount);
            public static string Generate()
            {
                var sb = new StringBuilder(7);

                for (int i = 0; i < 7; i++)
                    sb.Append(_base62chars[_random.Next(62)]);

                return sb.ToString();
            }
        }

    }
}