using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Hermes_Executor.Core
{
    public class LuaColorizer : DocumentColorizingTransformer
    {
        // Madium / VS Code Dark+ Color Palette Brushes
        private static readonly SolidColorBrush CommentBrush  = new SolidColorBrush(Color.FromRgb(0x6A, 0x99, 0x55)); // Muted Green #6A9955
        private static readonly SolidColorBrush StringBrush   = new SolidColorBrush(Color.FromRgb(0xE0, 0x6C, 0x75)); // Coral Pink #E06C75
        private static readonly SolidColorBrush KeywordBrush  = new SolidColorBrush(Color.FromRgb(0xE5, 0xC0, 0x7B)); // Warm Yellow/Orange #E5C07B
        private static readonly SolidColorBrush FuncCallBrush = new SolidColorBrush(Color.FromRgb(0x61, 0xAF, 0xEF)); // Light Blue #61AFEF
        private static readonly SolidColorBrush GlobalBrush   = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xAA)); // Gold #DCDCAA
        private static readonly SolidColorBrush RobloxBrush   = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)); // Teal #4EC9B0
        private static readonly SolidColorBrush NumberBrush   = new SolidColorBrush(Color.FromRgb(0xB5, 0xCE, 0xA8)); // Mint Green #B5CEA8
        private static readonly SolidColorBrush BoolNilBrush  = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)); // Cyan Blue #569CD6

        static LuaColorizer()
        {
            CommentBrush.Freeze();
            StringBrush.Freeze();
            KeywordBrush.Freeze();
            FuncCallBrush.Freeze();
            GlobalBrush.Freeze();
            RobloxBrush.Freeze();
            NumberBrush.Freeze();
            BoolNilBrush.Freeze();
        }

        private static readonly HashSet<string> LuaKeywords = new HashSet<string>
        {
            "and", "break", "do", "else", "elseif", "end", "for", "function",
            "goto", "if", "in", "local", "not", "or", "repeat", "return",
            "then", "until", "while"
        };

        private static readonly HashSet<string> LuaGlobalFunctions = new HashSet<string>
        {
            "assert", "collectgarbage", "dofile", "error", "getmetatable", "ipairs",
            "load", "loadfile", "loadstring", "next", "pairs", "pcall", "print",
            "rawequal", "rawget", "rawlen", "rawset", "require", "select",
            "setmetatable", "tonumber", "tostring", "type", "unpack", "warn", "xpcall"
        };

        private static readonly HashSet<string> RobloxGlobals = new HashSet<string>
        {
            "game", "workspace", "script", "plugin", "wait", "delay", "spawn",
            "tick", "time", "task", "Instance", "Enum", "Vector2", "Vector3",
            "CFrame", "Color3", "UDim", "UDim2", "TweenInfo", "BrickColor",
            "Ray", "Region3", "NumberSequence", "ColorSequence", "NumberRange",
            "Rect", "PhysicalProperties", "getfenv", "setfenv", "getrenv",
            "getgenv", "getrawmetatable", "setrawmetatable", "hookfunction",
            "newcclosure", "syn", "Drawing"
        };

        private static readonly HashSet<string> BoolNilKeywords = new HashSet<string>
        {
            "true", "false", "nil"
        };

        private static readonly Regex TokenRegex = new Regex(
            @"(?<comment>--.*)|(?<string>""([^""\\]|\\.)*""|'([^'\\]|\\.)*')|(?<number>\b\d+(\.\d*)?\b|0[xX][0-9a-fA-F]+\b)|(?<word>\b[a-zA-Z_][a-zA-Z0-9_]*\b)",
            RegexOptions.Compiled);

        protected override void ColorizeLine(DocumentLine line)
        {
            string lineText = CurrentContext.Document.GetText(line.Offset, line.Length);
            int lineOffset = line.Offset;

            MatchCollection matches = TokenRegex.Matches(lineText);
            foreach (Match match in matches)
            {
                int start = lineOffset + match.Index;
                int end = start + match.Length;

                if (match.Groups["comment"].Success)
                {
                    ChangeLinePart(start, end, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(CommentBrush);
                    });
                }
                else if (match.Groups["string"].Success)
                {
                    ChangeLinePart(start, end, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(StringBrush);
                    });
                }
                else if (match.Groups["number"].Success)
                {
                    ChangeLinePart(start, end, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(NumberBrush);
                    });
                }
                else if (match.Groups["word"].Success)
                {
                    string word = match.Value;

                    if (LuaKeywords.Contains(word))
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(KeywordBrush);
                        });
                    }
                    else if (BoolNilKeywords.Contains(word))
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(BoolNilBrush);
                        });
                    }
                    else if (LuaGlobalFunctions.Contains(word))
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(GlobalBrush);
                        });
                    }
                    else if (RobloxGlobals.Contains(word))
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(RobloxBrush);
                        });
                    }
                    else
                    {
                        // Check if it's a function call like funcName(...)
                        int matchEndInLine = match.Index + match.Length;
                        if (IsFollowedByOpenParen(lineText, matchEndInLine))
                        {
                            ChangeLinePart(start, end, element =>
                            {
                                element.TextRunProperties.SetForegroundBrush(FuncCallBrush);
                            });
                        }
                    }
                }
            }
        }

        private static bool IsFollowedByOpenParen(string lineText, int startIndex)
        {
            for (int i = startIndex; i < lineText.Length; i++)
            {
                char c = lineText[i];
                if (char.IsWhiteSpace(c)) continue;
                return c == '(';
            }
            return false;
        }
    }
}
