using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Application.Interfaces;

namespace Application.Services
{
    public class PasswordGeneratorService : IPasswordGeneratorService
    {
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?";
        private const string SimilarCharacters = "0O1Il";
        private const string AmbiguousSymbols = "{}[]()/\\'\"`~,;:.<>";

        public string GeneratePassword(
            int length,
            bool includeUppercase,
            bool includeLowercase,
            bool includeNumbers,
            bool includeSymbols,
            bool excludeSimilarCharacters,
            bool excludeAmbiguousSymbols)
        {
            if (length < 8 || length > 128)
            {
                throw new ArgumentException("De lengte moet tussen 8 en 128 tekens liggen.");
            }

            var characterSets = new List<string>();

            if (includeUppercase)
                characterSets.Add(FilterCharacters(Uppercase, excludeSimilarCharacters, false));

            if (includeLowercase)
                characterSets.Add(FilterCharacters(Lowercase, excludeSimilarCharacters, false));

            if (includeNumbers)
                characterSets.Add(FilterCharacters(Numbers, excludeSimilarCharacters, false));

            if (includeSymbols)
                characterSets.Add(FilterCharacters(Symbols, false, excludeAmbiguousSymbols));

            characterSets = characterSets.Where(set => set.Length > 0).ToList();

            if (!characterSets.Any())
            {
                throw new ArgumentException("Selecteer minstens één type teken.");
            }

            var passwordCharacters = new List<char>();

            // Zorg dat elk gekozen type minstens één keer voorkomt
            foreach (var set in characterSets)
            {
                passwordCharacters.Add(GetRandomCharacter(set));
            }

            var allAllowedCharacters = string.Concat(characterSets);

            while (passwordCharacters.Count < length)
            {
                passwordCharacters.Add(GetRandomCharacter(allAllowedCharacters));
            }

            Shuffle(passwordCharacters);

            return new string(passwordCharacters.ToArray());
        }

        private static string FilterCharacters(
            string characters,
            bool excludeSimilarCharacters,
            bool excludeAmbiguousSymbols)
        {
            var excludedCharacters = new HashSet<char>();

            if (excludeSimilarCharacters)
            {
                foreach (var character in SimilarCharacters)
                {
                    excludedCharacters.Add(character);
                }
            }

            if (excludeAmbiguousSymbols)
            {
                foreach (var character in AmbiguousSymbols)
                {
                    excludedCharacters.Add(character);
                }
            }

            return new string(characters.Where(character => !excludedCharacters.Contains(character)).ToArray());
        }

        private static char GetRandomCharacter(string characters)
        {
            var index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }

        private static void Shuffle(List<char> characters)
        {
            for (int i = characters.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (characters[i], characters[j]) = (characters[j], characters[i]);
            }
        }
    }
}
