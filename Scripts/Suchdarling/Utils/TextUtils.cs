using System.Text;
using System.Text.RegularExpressions;

namespace Suchdarling.Utils
{
    public class TextUtils
    {
        private static readonly Regex _repeatRegex = new Regex(@"(.)\1{2,}", RegexOptions.Compiled);

        private static readonly Regex _separatorRegex = new Regex(@"[-_\.]+", RegexOptions.Compiled); 

        private static readonly Regex _leetRegex = new Regex(@"[0-9]", RegexOptions.Compiled);

        private static readonly Dictionary<string, bool> _wordCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _cacheLock = new object();

        private static bool _enableStrictMode = true;

        private static bool _replaceWithRandom = true;

        private static int _minWordLength = 2;

        private static readonly string[] _replacements = new string[] 
        { 
            "#", "@", "$"
        };

        private static readonly HashSet<string> _badWordRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "срв", "эмези", "клобир", "шлюх", "хуй", "хуё", "уебищ", "мамаш", "ебат", "ебут", "ебан", "залуп", "сос", "гондо", "пидор", "пизд",
            "пидиди", "уебок", "пидр", "еблан", "шлюш", "долбое", "далбае", "долбае", "пенис", "гей", "сучк", "сук", "ахуе",
            "гандон", "гомик", "педик", "проститут", "бляд", "мудак", "мудил", "долбое", "далбое", "уебан", "конч", "пидарас", 
            "дебил", "дегенерат", "секс", "трах", "выеб", "выёб", "сперм", "оргазм"
        };

        private static readonly Dictionary<string, string[]> _wordVariations = new Dictionary<string, string[]>
        {
            { "хуй", new[] { "хуй", "хуё", "хуя", "хую", "хуе" } },
            { "пизд", new[] { "пизд", "пизж", "пизо", "писд" } },
            { "сук", new[] { "сук", "сучий", "сучар", "сучк" } }
        };

        private static readonly Dictionary<char, char> _homoglyphs = new Dictionary<char, char>
        {
            { 'a', 'а' }, { 'x', 'х' }, { 'c', 'с' }, { 'y', 'у' }, { 'p', 'р' },
            { 'b', 'в' }, { 'k', 'к' }, { 'e', 'е' }, { 'o', 'о' }, { 'h', 'н' },
            { 'm', 'м' }, { 't', 'т' }, { 'n', 'п' }, { 'r', 'г' }, { 'd', 'д' },
            { '0', 'о' }, { '1', 'і' }, { '3', 'з' }, { '4', 'ч' }, { '5', 'ѕ' },
            { '6', 'б' }, { '8', 'в' }, { '9', 'д' }, { 'э', 'е' }, { 'і', 'и' }, 
            { 'ї', 'и' }, { 'є', 'е' }
        };

        private static readonly char[] _separators = new char[]
        { 
            ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '"', '\'', 
            '(', ')', '[', ']', '{', '}', '@', '#', '$', '%', '^', '&', '*',
            '-', '_', '+', '=', '|', '\\', '/', '<', '>', '~', '`'
        };

        private static readonly string _englishCharse = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateRandomString(int length)
        {
            Random random = new Random();
            return new string(Enumerable.Repeat(_englishCharse, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Filter(string input, bool strictMode = true)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                _enableStrictMode = strictMode;
                string[] words = input.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder result = new StringBuilder();
                int lastIndex = 0;
                for (int i = 0; i <= input.Length; i++)
                {
                    if (i == input.Length || _separators.Contains(input[i]))
                    {
                        if (i > lastIndex)
                        {
                            string word = input.Substring(lastIndex, i - lastIndex);
                            string processedWord = ProcessWord(word);
                            result.Append(processedWord);
                        }
                        if (i < input.Length)
                        {
                            result.Append(input[i]);
                        }
                        lastIndex = i + 1;
                    }
                }
                return result.ToString();
            }
            return input;
        }

        private static string ProcessWord(string word)
        {
            if (word.Length >= _minWordLength)
            {
                string originalWord = word;
                lock (_cacheLock)
                {
                    if (_wordCache.TryGetValue(word, out bool isBad))
                    {
                        return isBad ? ReplaceBadWord(word) : word;
                    }
                }
                string normalized = NormalizeWord(word);
                bool containsBadWord = CheckForBadWords(normalized);
                lock (_cacheLock)
                {
                    if (_wordCache.Count > 10000)
                    {
                        _wordCache.Clear();
                    }
                    _wordCache[word] = containsBadWord;
                }
                return containsBadWord ? ReplaceBadWord(originalWord) : originalWord;
            }
            return word;
        }

        private static string NormalizeWord(string word)
        {
            if (!string.IsNullOrEmpty(word))
            {
                string result = word.ToLowerInvariant();
                result = ReplaceHomoglyphs(result);
                result = _repeatRegex.Replace(result, "$1$1");
                result = _separatorRegex.Replace(result, "");
                result = _leetRegex.Replace(result, m => 
                {
                    char digit = m.Value[0];
                    return _homoglyphs.ContainsKey(digit) ? _homoglyphs[digit].ToString() : "";
                });
                result = new string(result.Where(c => IsCyrillic(c) || c == ' ').ToArray());
                return result;
            }
            return word;
        }

        private static bool CheckForBadWords(string normalizedWord)
        {
            if (_badWordRoots.Any(badWord => normalizedWord.Contains(badWord)))
            {
                return true;
            }
            foreach (var kvp in _wordVariations)
            {
                if (kvp.Value.Any(x => normalizedWord.Contains(x)))
                {
                    return true;
                }
            }
            if (_enableStrictMode)
            {
                for (int i = 0; i < normalizedWord.Length - 2; i++)
                {
                    for (int j = 3; j <= Math.Min(6, normalizedWord.Length - i); j++)
                    {
                        string substring = normalizedWord.Substring(i, j);
                        if (_badWordRoots.Contains(substring))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static string ReplaceBadWord(string originalWord)
        {
            if (!_replaceWithRandom)
            {
                return new string('*', originalWord.Length);
            }
            Random random = new Random(Guid.NewGuid().GetHashCode());
            StringBuilder result = new StringBuilder();
            foreach (char c in originalWord)
            {
                if (char.IsLetter(c))
                {
                    result.Append(_replacements[random.Next(_replacements.Length)]);
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        private static string ReplaceHomoglyphs(string input)
        {
            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                char lowerChar = char.ToLowerInvariant(c);
                if (_homoglyphs.TryGetValue(lowerChar, out char replacement))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool IsCyrillic(char c)
        {
            return (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё';
        }

        public static void AddBadWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                _badWordRoots.Add(word.ToLowerInvariant());
                ClearCache();
            }
        }

        public static void RemoveBadWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                _badWordRoots.Remove(word.ToLowerInvariant());
                ClearCache();
            }
        }

        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _wordCache.Clear();
            }
        }

        public static bool ContainsBadWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                string[] words = text.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (word.Length >= _minWordLength)
                    {
                        string normalized = NormalizeWord(word);
                        if (CheckForBadWords(normalized))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
