using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPasswordGeneratorService
    {
        string GeneratePassword(
            int length,
            bool includeUppercase,
            bool includeLowercase,
            bool includeNumbers,
            bool includeSymbols,
            bool excludeSimilarCharacters,
            bool excludeAmbiguousSymbols);
    }
}
