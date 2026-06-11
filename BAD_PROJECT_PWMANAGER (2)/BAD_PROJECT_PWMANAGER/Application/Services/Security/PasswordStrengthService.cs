using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.Interfaces;
using System.Text.RegularExpressions;

namespace Application.Security;

public class PasswordStrengthService : IPasswordStrengthService
{
    public int CalculateScore(string password)
    {
        int score = 0;

        if (password.Length >= 8)
            score++;

        if (password.Length >= 12)
            score++;

        if (Regex.IsMatch(password, "[a-z]"))
            score++;

        if (Regex.IsMatch(password, "[A-Z]"))
            score++;

        if (Regex.IsMatch(password, "[0-9]"))
            score++;

        if (Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            score++;

        return score;
    }

    public string GetStrengthLabel(int score)
    {
        if (score <= 2)
            return "Zwak";

        if (score <= 4)
            return "Gemiddeld";

        return "Sterk";
    }
}