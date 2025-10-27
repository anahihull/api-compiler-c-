using System;
using System.Collections.Generic;
using System.Linq;

namespace Compilador
{
    internal class Lexer
    {
        private string input;
        private int currentPos;
        private int currentLine;
        private int currentColumn;

        // Character arrays for classification
        public static char[] specialCharacters = { '[', ']', '(', ')', '{', '}', ',', ';', ':', '.', '+', '-', '*', '/', '%', '>', '<', '=', '!', '&', '|', '"' };
        public static char[] numeros = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static char[] letters = {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '_'
        };

        // DFA Transition Dictionary
        public Dictionary<(string, char), string> transitionsA;

        // Final states arrays
        public string[] finalNum = { "q1", "q2" };
        public string[] finalOperators;
        public string[] finalDelimiters;
        public string[] finalKeywords;
        public string[] finalLiteralString = { "q_string_end" };
        public string[] finalComment = { "q_comment_end" };
        public string[] finalPathParam = { "q_path_end" };
        public string[] notFinal;

        // Keywords dictionary
        private Dictionary<string, Tools.TokenType> keywords;

        public Lexer(string inputText)
        {
            input = inputText ?? "";
            currentPos = 0;
            currentLine = 1;
            currentColumn = 1;

            InitializeKeywords();
            InitializeTransitions();
            InitializeFinalStates();
        }

        private void InitializeKeywords()
        {
            keywords = new Dictionary<string, Tools.TokenType>
            {
                // API/Service keywords
                { "service", Tools.TokenType.SERVICE },
                { "route", Tools.TokenType.ROUTE },
                { "envroute", Tools.TokenType.ENVROUTE },
                { "GET", Tools.TokenType.GET },
                { "POST", Tools.TokenType.POST },
                { "PUT", Tools.TokenType.PUT },
                { "PATCH", Tools.TokenType.PATCH },
                { "DELETE", Tools.TokenType.DELETE },
                { "params", Tools.TokenType.PARAMS },
                { "query", Tools.TokenType.QUERY },
                { "body", Tools.TokenType.BODY },
                { "status", Tools.TokenType.STATUS },
                { "return", Tools.TokenType.RETURN },
                { "error", Tools.TokenType.ERROR },
                { "schema", Tools.TokenType.SCHEMA },
                { "required", Tools.TokenType.REQUIRED },
                { "optional", Tools.TokenType.OPTIONAL },
                { "default", Tools.TokenType.DEFAULT },
                { "use", Tools.TokenType.USE },
                { "before", Tools.TokenType.BEFORE },
                { "after", Tools.TokenType.AFTER },
                { "auth", Tools.TokenType.AUTH },
                { "allow", Tools.TokenType.ALLOW },
                { "deny", Tools.TokenType.DENY },
                { "let", Tools.TokenType.LET },
                { "const", Tools.TokenType.CONST },

                // Traditional keywords
                { "if", Tools.TokenType.IF },
                { "else", Tools.TokenType.ELSE },
                { "while", Tools.TokenType.WHILE },
                { "for", Tools.TokenType.FOR },
                { "break", Tools.TokenType.BREAK },
                { "continue", Tools.TokenType.CONTINUE },
                { "var", Tools.TokenType.VAR },
                { "int", Tools.TokenType.INT },
                { "bool", Tools.TokenType.BOOL },
                { "null", Tools.TokenType.NULL },
                { "true", Tools.TokenType.TRUE },
                { "false", Tools.TokenType.FALSE },
                { "function", Tools.TokenType.FUNCTION },
                { "class", Tools.TokenType.CLASS },
                { "public", Tools.TokenType.PUBLIC },
                { "private", Tools.TokenType.PRIVATE },
                { "protected", Tools.TokenType.PROTECTED },
                { "static", Tools.TokenType.STATIC },
                { "void", Tools.TokenType.VOID },
                { "new", Tools.TokenType.NEW },
                { "this", Tools.TokenType.THIS }
            };
        }

        private void InitializeTransitions()
        {
            transitionsA = new Dictionary<(string, char), string>
            {
                // Numbers
                {("q0", '0'), "q1"}, {("q0", '1'), "q1"}, {("q0", '2'), "q1"}, {("q0", '3'), "q1"},
                {("q0", '4'), "q1"}, {("q0", '5'), "q1"}, {("q0", '6'), "q1"}, {("q0", '7'), "q1"},
                {("q0", '8'), "q1"}, {("q0", '9'), "q1"},
                {("q1", '0'), "q1"}, {("q1", '1'), "q1"}, {("q1", '2'), "q1"}, {("q1", '3'), "q1"},
                {("q1", '4'), "q1"}, {("q1", '5'), "q1"}, {("q1", '6'), "q1"}, {("q1", '7'), "q1"},
                {("q1", '8'), "q1"}, {("q1", '9'), "q1"}, {("q1", '.'), "q2"},
                {("q2", '0'), "q2"}, {("q2", '1'), "q2"}, {("q2", '2'), "q2"}, {("q2", '3'), "q2"},
                {("q2", '4'), "q2"}, {("q2", '5'), "q2"}, {("q2", '6'), "q2"}, {("q2", '7'), "q2"},
                {("q2", '8'), "q2"}, {("q2", '9'), "q2"},

                // Single character operators
                {("q0", '+'), "q_plus"}, {("q0", '-'), "q_minus"}, {("q0", '*'), "q_mult"},
                {("q0", '/'), "q_div"}, {("q0", '%'), "q_mod"}, {("q0", '='), "q_equal"},
                {("q0", '>'), "q_greater"}, {("q0", '<'), "q_less"}, {("q0", '!'), "q_not"},
                {("q0", '&'), "q_and_start"}, {("q0", '|'), "q_or_start"},

                // Compound operators
                {("q_plus", '='), "q_plus_equal"}, {("q_minus", '='), "q_minus_equal"},
                {("q_mult", '='), "q_mult_equal"}, {("q_div", '='), "q_div_equal"},
                {("q_mod", '='), "q_mod_equal"}, {("q_equal", '='), "q_equal_equal"},
                {("q_greater", '='), "q_greater_equal"}, {("q_less", '='), "q_less_equal"},
                {("q_not", '='), "q_not_equal"}, {("q_and_start", '&'), "q_and"},
                {("q_or_start", '|'), "q_or"},

                // Delimiters
                {("q0", '('), "q_lparen"}, {("q0", ')'), "q_rparen"}, {("q0", '{'), "q_lbrace"},
                {("q0", '}'), "q_rbrace"}, {("q0", '['), "q_lbracket"}, {("q0", ']'), "q_rbracket"},
                {("q0", ','), "q_comma"}, {("q0", ';'), "q_semicolon"}, {("q0", '.'), "q_dot"},
                {("q0", ':'), "q_colon"}, {("q_or_start", '#'), "q_pipe"},

                // Comments
                {("q_div", '/'), "q_comment_start"},
                {("q_comment_start", '#'), "q_comment_body"}, {("q_comment_body", '#'), "q_comment_body"},

                // Strings
                {("q0", '"'), "q_string_start"}, {("q_string_start", '#'), "q_string_body"},
                {("q_string_body", '#'), "q_string_body"}, {("q_string_body", '"'), "q_string_end"},

                // Path parameters {word}
                {("q_lbrace", '@'), "q_path_start"}, {("q_path_start", '@'), "q_path_body"},
                {("q_path_body", '@'), "q_path_body"}, {("q_path_body", '}'), "q_path_end"},

                // Identifiers (starting with letters)
                {("q0", '@'), "q_id"}, {("q_id", '@'), "q_id"}
            };

            // Add all letter transitions for identifiers
            foreach (char c in letters)
            {
                transitionsA[("q0", c)] = "q_id";
                transitionsA[("q_id", c)] = "q_id";
                transitionsA[("q_path_start", c)] = "q_path_body";
                transitionsA[("q_path_body", c)] = "q_path_body";
            }

            // Add number transitions to identifiers
            foreach (char c in numeros)
            {
                transitionsA[("q_id", c)] = "q_id";
                transitionsA[("q_path_body", c)] = "q_path_body";
            }
        }

        private void InitializeFinalStates()
        {
            finalOperators = new string[] {
                "q_plus", "q_minus", "q_mult", "q_div", "q_mod", "q_equal", "q_greater", "q_less", "q_not",
                "q_plus_equal", "q_minus_equal", "q_mult_equal", "q_div_equal", "q_mod_equal",
                "q_equal_equal", "q_greater_equal", "q_less_equal", "q_not_equal", "q_and", "q_or"
            };

            finalDelimiters = new string[] {
                "q_lparen", "q_rparen", "q_lbrace", "q_rbrace", "q_lbracket", "q_rbracket",
                "q_comma", "q_semicolon", "q_dot", "q_colon", "q_pipe"
            };

            finalKeywords = new string[] { "q_id" };

            notFinal = new string[] { "q_and_start", "q_or_start", "q_string_start", "q_comment_start", "q_path_start" };
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (currentPos < input.Length)
            {
                // Skip whitespace
                if (char.IsWhiteSpace(input[currentPos]))
                {
                    if (input[currentPos] == '\n')
                    {
                        currentLine++;
                        currentColumn = 1;
                    }
                    else
                    {
                        currentColumn++;
                    }
                    currentPos++;
                    continue;
                }

                var token = GetNextToken();
                if (token != null && token.Type != Tools.TokenType.WHITESPACE)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(new Token(Tools.TokenType.EOF, "", currentLine, currentColumn));
            return tokens;
        }

        private Token GetNextToken()
        {
            if (currentPos >= input.Length)
                return new Token(Tools.TokenType.EOF, "", currentLine, currentColumn);

            int startLine = currentLine;
            int startColumn = currentColumn;
            string temp = "";
            string currentState = "q0";

            while (currentPos < input.Length)
            {
                char c = input[currentPos];
                char x = GetTransitionChar(c, currentState);

                // Check if we have a transition
                if (transitionsA.TryGetValue((currentState, x), out string nextState))
                {
                    currentState = nextState;
                    temp += c;
                    currentPos++;
                    currentColumn++;
                }
                else
                {
                    // Try with generic character classes
                    if (letters.Contains(c))
                    {
                        x = '@';
                        if (transitionsA.TryGetValue((currentState, x), out string next))
                        {
                            currentState = next;
                            temp += c;
                            currentPos++;
                            currentColumn++;
                        }
                        else
                        {
                            break; // No valid transition
                        }
                    }
                    else
                    {
                        break; // No valid transition
                    }
                }

                // Check for end of token conditions
                if (IsEndOfToken(currentState, c))
                {
                    break;
                }
            }

            // Create token based on final state
            return CreateToken(currentState, temp, startLine, startColumn);
        }

        private char GetTransitionChar(char c, string state)
        {
            // Special handling for strings and comments
            if (state == "q_string_start" || state == "q_string_body")
            {
                if (c == '"') return '"';
                return '#'; // Generic character for string content
            }

            if (state == "q_comment_start" || state == "q_comment_body")
            {
                if (c == '\n') return '\n';
                return '#'; // Generic character for comment content
            }

            if (state == "q_or_start" && c != '|')
            {
                return '#'; // Single pipe
            }

            // Numbers
            if (numeros.Contains(c)) return c;

            // Special characters
            if (specialCharacters.Contains(c)) return c;

            // Letters
            if (letters.Contains(c)) return '@';

            return c;
        }

        private bool IsEndOfToken(string state, char nextChar)
        {
            if (state == "q_string_end") return true;                // String closed
            if (state == "q_comment_body" && nextChar == '\n') return true; // Comment to EOL
            if (state == "q_path_end") return true;                  // Path param }

            return false;
        }

        private Token CreateToken(string finalState, string value, int line, int column)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Token(Tools.TokenType.INVALID, "", line, column);
            }

            // Numbers
            if (finalNum.Contains(finalState))
            {
                var tokenType = finalState == "q1" ? Tools.TokenType.INTEGER : Tools.TokenType.FLOAT;
                return new Token(tokenType, value, line, column);
            }

            // Operators
            if (finalOperators.Contains(finalState))
            {
                var tokenType = GetOperatorTokenType(finalState);
                return new Token(tokenType, value, line, column);
            }

            // Delimiters
            if (finalDelimiters.Contains(finalState))
            {
                var tokenType = GetDelimiterTokenType(finalState);
                return new Token(tokenType, value, line, column);
            }

            // String literal
            if (finalLiteralString.Contains(finalState))
            {
                return new Token(Tools.TokenType.STRING, value, line, column);
            }

            // Comment
            if (finalComment.Contains(finalState))
            {
                return new Token(Tools.TokenType.COMMENT, value, line, column);
            }

            // Path parameter
            if (finalPathParam.Contains(finalState))
            {
                return new Token(Tools.TokenType.PATH_PARAM, value, line, column);
            }

            // Identifier / keyword
            if (finalKeywords.Contains(finalState) || finalState == "q_id")
            {
                if (keywords.ContainsKey(value))
                {
                    return new Token(keywords[value], value, line, column);
                }
                return new Token(Tools.TokenType.IDENTIFIER, value, line, column);
            }

            if (notFinal.Contains(finalState))
            {
                return new Token(Tools.TokenType.INVALID, value, line, column);
            }

            return new Token(Tools.TokenType.IDENTIFIER, value, line, column);
        }

        private Tools.TokenType GetOperatorTokenType(string state)
        {
            return state switch
            {
                "q_plus" => Tools.TokenType.SUMA,
                "q_minus" => Tools.TokenType.RESTA,
                "q_mult" => Tools.TokenType.MULT,
                "q_div" => Tools.TokenType.DIV,
                "q_mod" => Tools.TokenType.MOD,
                "q_equal" => Tools.TokenType.EQUAL,
                "q_greater" => Tools.TokenType.MAYOR,
                "q_less" => Tools.TokenType.MENOR,
                "q_not" => Tools.TokenType.NOT,
                "q_plus_equal" => Tools.TokenType.PLUS_EQUAL,
                "q_minus_equal" => Tools.TokenType.MINUS_EQUAL,
                "q_mult_equal" => Tools.TokenType.MULT_EQUAL,
                "q_div_equal" => Tools.TokenType.DIV_EQUAL,
                "q_mod_equal" => Tools.TokenType.MOD_EQUAL,
                "q_equal_equal" => Tools.TokenType.EQUAL_EQUAL,
                "q_greater_equal" => Tools.TokenType.GREATER_EQUAL,
                "q_less_equal" => Tools.TokenType.LESS_EQUAL,
                "q_not_equal" => Tools.TokenType.NOT_EQUAL,
                "q_and" => Tools.TokenType.AND,
                "q_or" => Tools.TokenType.OR,
                _ => Tools.TokenType.INVALID
            };
        }

        private Tools.TokenType GetDelimiterTokenType(string state)
        {
            return state switch
            {
                "q_lparen" => Tools.TokenType.LPAREN,
                "q_rparen" => Tools.TokenType.RPAREN,
                "q_lbrace" => Tools.TokenType.LBRACE,
                "q_rbrace" => Tools.TokenType.RBRACE,
                "q_lbracket" => Tools.TokenType.LBRACKET,
                "q_rbracket" => Tools.TokenType.RBRACKET,
                "q_comma" => Tools.TokenType.COMMA,
                "q_semicolon" => Tools.TokenType.SEMICOLON,
                "q_dot" => Tools.TokenType.DOT,
                "q_colon" => Tools.TokenType.COLON,
                "q_pipe" => Tools.TokenType.PIPE,
                _ => Tools.TokenType.INVALID
            };
        }

        // === ÚNICA SALIDA QUE QUEREMOS ===
        private static readonly HashSet<Tools.TokenType> DelimiterTypes = new HashSet<Tools.TokenType>
        {
            Tools.TokenType.LPAREN, Tools.TokenType.RPAREN,
            Tools.TokenType.LBRACE, Tools.TokenType.RBRACE,
            Tools.TokenType.LBRACKET, Tools.TokenType.RBRACKET,
            Tools.TokenType.COMMA, Tools.TokenType.SEMICOLON,
            Tools.TokenType.DOT, Tools.TokenType.COLON,
            Tools.TokenType.PIPE
        };

        public List<string> GetFormattedTokens()
        {
            var formatted = new List<string>();
            foreach (var tok in Tokenize())
            {
                if (tok.Type == Tools.TokenType.EOF || tok.Type == Tools.TokenType.COMMENT)
                    continue;

                string tipo = MapDisplayType(tok.Type);
                string lexema = EscapeLexeme(tok.Value);
                formatted.Add($"<{tipo}, \"{lexema}\">");
            }
            return formatted;
        }

        private string MapDisplayType(Tools.TokenType t)
        {
            if (t == Tools.TokenType.IDENTIFIER) return "ID";
            if (t == Tools.TokenType.INTEGER || t == Tools.TokenType.FLOAT) return "NUM";
            if (t == Tools.TokenType.STRING) return "LITERAL";
            if (DelimiterTypes.Contains(t)) return "DELIMITADOR";
            if (t == Tools.TokenType.EQUAL) return "ASIGNACION";

            return t.ToString();
        }

        private string EscapeLexeme(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
