using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IPasswordStrengthService
{
    int CalculateScore(string password);

    string GetStrengthLabel(int score);
}