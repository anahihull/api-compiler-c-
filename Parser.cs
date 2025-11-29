using System;
using System.Collections.Generic;

namespace Compilador
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token Current => current < tokens.Count ? tokens[current] : tokens[^1];
        private Token Advance() => current < tokens.Count ? tokens[current++] : tokens[^1];

        private bool Match(params Tools.TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Current.Type == type)
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private void Expect(Tools.TokenType type, string message)
        {
            if (Current.Type == type)
            {
                Advance();
            }
            else
            {
                throw new Exception($"Error de sintaxis en línea {Current.Line}: {message}. Se encontró '{Current.Value}'");
            }
        }

        public void Parse()
        {
            while (Current.Type != Tools.TokenType.EOF)
            {
                ParseService();
            }
            Console.WriteLine("✔ Análisis sintáctico completado sin errores.");
        }

        private void ParseService()
        {
            Expect(Tools.TokenType.SERVICE, "Se esperaba 'service'");
            Expect(Tools.TokenType.IDENTIFIER, "Se esperaba nombre del servicio");
            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' tras el nombre del servicio");

            while (Match(Tools.TokenType.ROUTE))
            {
                ParseRoute();
            }

            Expect(Tools.TokenType.RBRACE, "Se esperaba '}' al final del servicio");
        }

        private void ParseRoute()
        {
            if (Match(Tools.TokenType.STRING, Tools.TokenType.IDENTIFIER))
            {
                // ok
            }
            else if (Match(Tools.TokenType.DIV))
            {
                if (Match(Tools.TokenType.IDENTIFIER, Tools.TokenType.AUTH, Tools.TokenType.BODY,
                        Tools.TokenType.PARAMS, Tools.TokenType.QUERY, Tools.TokenType.STATUS,
                        Tools.TokenType.ERROR, Tools.TokenType.RETURN))
                {
                    // ok
                }
                else
                {
                    throw new Exception($"Error de sintaxis en línea {Current.Line}: Se esperaba un nombre de ruta después de '/'. Se encontró '{Current.Value}'");
                }
            }
            else
            {
                throw new Exception($"Error de sintaxis en línea {Current.Line}: Se esperaba nombre o ruta del endpoint. Se encontró '{Current.Value}'");
            }

            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' tras la ruta");

            // ✅ CORRECCIÓN: Verificar sin consumir
            while (Current.Type == Tools.TokenType.GET || 
                   Current.Type == Tools.TokenType.POST || 
                   Current.Type == Tools.TokenType.PUT || 
                   Current.Type == Tools.TokenType.PATCH || 
                   Current.Type == Tools.TokenType.DELETE)
            {
                Advance(); // Consume el método HTTP
                ParseMethodBody();
            }

            Expect(Tools.TokenType.RBRACE, "Se esperaba '}' al final de la ruta");
        }

        private void ParseMethodBody()
        {
            // ✅ Ya no esperamos el método HTTP aquí, fue consumido antes
            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' después del método HTTP");

            while (!Match(Tools.TokenType.RBRACE))
            {
                ParseStatement();
            }
        }

        private void ParseStatement()
        {
            if (Match(Tools.TokenType.LET))
            {
                ParseVariableDeclaration();
                Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de la declaración");
            }
            else if (Match(Tools.TokenType.RETURN))
            {
                if (Match(Tools.TokenType.ERROR))
                {
                    Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de return error");
                    return;
                }

                ParseExpression();
                Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de return");
            }
            else if (Match(Tools.TokenType.IF))
            {
                ParseIfStatement();
            }
            else
            {
                throw new Exception($"Error de sintaxis en línea {Current.Line}: Sentencia no reconocida cerca de '{Current.Value}'");
            }
        }

        private void ParseVariableDeclaration()
        {
            Expect(Tools.TokenType.IDENTIFIER, "Se esperaba un identificador después de 'let'");
            Expect(Tools.TokenType.EQUAL, "Se esperaba '=' en la asignación");
            ParseExpression();
        }

        private void ParseIfStatement()
        {
            Expect(Tools.TokenType.LPAREN, "Se esperaba '(' después de 'if'");
            ParseExpression();
            Expect(Tools.TokenType.RPAREN, "Se esperaba ')' después de la condición if");
            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' para abrir el bloque if");

            while (!Match(Tools.TokenType.RBRACE))
                ParseStatement();

            if (Match(Tools.TokenType.ELSE))
            {
                Expect(Tools.TokenType.LBRACE, "Se esperaba '{' después de 'else'");
                while (!Match(Tools.TokenType.RBRACE))
                    ParseStatement();
            }
        }

        private void ParseExpression()
        {
            ParseTerm();

            while (Match(Tools.TokenType.SUMA, Tools.TokenType.RESTA,
                         Tools.TokenType.EQUAL_EQUAL, Tools.TokenType.NOT_EQUAL,
                         Tools.TokenType.MAYOR, Tools.TokenType.MENOR,
                         Tools.TokenType.GREATER_EQUAL, Tools.TokenType.LESS_EQUAL,
                         Tools.TokenType.AND, Tools.TokenType.OR))
            {
                ParseTerm();
            }
        }

        private void ParseTerm()
        {
            if (Match(Tools.TokenType.IDENTIFIER, Tools.TokenType.INTEGER, Tools.TokenType.FLOAT,
                      Tools.TokenType.STRING, Tools.TokenType.TRUE, Tools.TokenType.FALSE,
                      Tools.TokenType.ERROR))
            {
                return;
            }

            if (Match(Tools.TokenType.LPAREN))
            {
                ParseExpression();
                Expect(Tools.TokenType.RPAREN, "Se esperaba ')' después de la expresión");
                return;
            }

            throw new Exception($"Error de sintaxis en línea {Current.Line}: Expresión no válida cerca de '{Current.Value}'");
        }
    }
}