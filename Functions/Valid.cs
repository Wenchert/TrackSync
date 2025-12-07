using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackSync.Functions
{
    class Valid
    {
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            int atIndex = email.IndexOf('@');
            if (atIndex == -1 || atIndex == 0 || atIndex == email.Length - 1)
            {
                return false;
            }

            string domain = email.Substring(atIndex + 1).ToLower();

            string[] allowedDomains = { "gmail.com", "yandex.com", "yandex.ru" };
            return allowedDomains.Any(allowed => domain.EndsWith(allowed));
        }
    }
}
