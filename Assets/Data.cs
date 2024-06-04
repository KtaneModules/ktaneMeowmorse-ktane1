using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Data {
    //Bless Tandy for the easy word-to-morse function written here

    public static readonly Dictionary<char, string> MorseTranslation = new Dictionary<char, string>()
    {
        { 'A', ".-"   },
        { 'B', "-..." },
        { 'C', "-.-." },
        { 'D', "-.."  },
        { 'E', "."    },
        { 'F', "..-." },
        { 'G', "--."  },
        { 'H', "...." },
        { 'I', ".."   },
        { 'J', ".---" },
        { 'K', "-.-"  },
        { 'L', ".-.." },
        { 'M', "--"   },
        { 'N', "-."   },
        { 'O', "---"  },
        { 'P', ".--." },
        { 'Q', "--.-" },
        { 'R', ".-."  },
        { 'S', "..."  },
        { 'T', "-"    },
        { 'U', "..-"  },
        { 'V', "...-" },
        { 'W', ".--"  },
        { 'X', "-..-" },
        { 'Y', "-.--" },
        { 'Z', "--.." },
    };

    public static readonly Dictionary<string, string> TargetWords = new Dictionary<string, string>()
    {
        {"Luna", "growl" },
        {"Oliver", "white" },
        {"Bella", "purr" },
        {"Leo", "lion" },
        {"Max", "notes" },
        {"Tama", "pets" },
        {"Sora", "rainy" },
        {"Kitty", "meows" },
        {"Sugar", "sweets" },
        {"Siger", "fishes" },
        {"Tiger", "garfield" },
        {"Alice", "cheshire" },
        {"Melody", "notes" },
        {"Tom", "jerry" },
        {"Nyan", "nya" },
    };

    public static string GenerateSequence(char ch)
    {
        string output = "";
        foreach (char unit in MorseTranslation[ch])
        {
            output += unit;
        }
        return output + " ";
    }
    public static string GenerateSequence(string str)
    {
        string output = "";
        foreach (char ch in str)
            output += GenerateSequence(ch);
        output.Trim();
        return output;
    }
}
