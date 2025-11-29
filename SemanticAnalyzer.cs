using System;
using System.Collections.Generic;
using System.Linq;

namespace Compilador
{
    public class SemanticAnalyzer
    {
        private readonly List<Token> tokens;
        private int current = 0;

        private Dictionary<string, string> symbolTable = new();
        private HashSet<string> routesSeen = new();

        public SemanticAnalyzer(List<Token> tokens)
        {
            this.tokens = tokens ?? new List<Token>();
        }

        private Token Current => current < tokens.Count ? tokens[current] : tokens[^1];
        private Token Advance() => current < tokens.Count ? tokens[current++] : tokens[^1];

        private bool Match(params Tools.TokenType[] types)
        {
            foreach (var t in types)
            {
                if (Current.Type == t)
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
                throw new Exception($"[Semántico] Línea {Current.Line}: {message}. Se encontró '{Current.Value}'");
            }
        }

        public void Analyze()
        {
            current = 0;
            while (Current.Type != Tools.TokenType.EOF)
            {
                AnalyzeService();
            }

            Console.WriteLine("✔ Análisis semántico completado sin errores.");
        }

        private void AnalyzeService()
        {
            Expect(Tools.TokenType.SERVICE, "Se esperaba 'service'");
            Expect(Tools.TokenType.IDENTIFIER, "Se esperaba nombre del servicio");
            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' tras el nombre del servicio");

            routesSeen.Clear();

            while (Match(Tools.TokenType.ROUTE))
            {
                AnalyzeRoute();
            }

            Expect(Tools.TokenType.RBRACE, "Se esperaba '}' al final del servicio");
        }

        private void AnalyzeRoute()
        {
            string routeRepresentation = null;

            if (Match(Tools.TokenType.STRING))
            {
                routeRepresentation = tokens[current - 1].Value;
            }
            else if (Match(Tools.TokenType.IDENTIFIER))
            {
                routeRepresentation = tokens[current - 1].Value;
            }
            else if (Match(Tools.TokenType.DIV))
            {
                if (Match(Tools.TokenType.IDENTIFIER, Tools.TokenType.AUTH, Tools.TokenType.BODY,
                        Tools.TokenType.PARAMS, Tools.TokenType.QUERY, Tools.TokenType.STATUS,
                        Tools.TokenType.ERROR, Tools.TokenType.RETURN))
                {
                    routeRepresentation = "/" + tokens[current - 1].Value;
                }
                else
                {
                    throw new Exception($"[Semántico] Línea {Current.Line}: Se esperaba nombre de ruta después de '/'. Se encontró '{Current.Value}'");
                }
            }
            else
            {
                throw new Exception($"[Semántico] Línea {Current.Line}: Se esperaba nombre o ruta del endpoint. Se encontró '{Current.Value}'");
            }

            if (string.IsNullOrWhiteSpace(routeRepresentation))
                throw new Exception($"[Semántico] Línea {Current.Line}: Ruta inválida.");

            if (routesSeen.Contains(routeRepresentation))
                throw new Exception($"[Semántico] Línea {Current.Line}: Ruta duplicada '{routeRepresentation}'.");
            routesSeen.Add(routeRepresentation);

            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' tras la ruta");

            // ✅ CORRECCIÓN: Verificar sin consumir
            while (Current.Type == Tools.TokenType.GET || 
                   Current.Type == Tools.TokenType.POST || 
                   Current.Type == Tools.TokenType.PUT || 
                   Current.Type == Tools.TokenType.PATCH || 
                   Current.Type == Tools.TokenType.DELETE)
            {
                AnalyzeMethodBody();
            }

            Expect(Tools.TokenType.RBRACE, "Se esperaba '}' al final de la ruta");
        }

        private void AnalyzeMethodBody()
        {
            // ✅ CORRECCIÓN: Consumir el método HTTP aquí
            if (!Match(Tools.TokenType.GET, Tools.TokenType.POST, Tools.TokenType.PUT, 
                       Tools.TokenType.PATCH, Tools.TokenType.DELETE))
            {
                throw new Exception($"[Semántico] Línea {Current.Line}: Se esperaba un método HTTP.");
            }
            var httpMethodToken = tokens[current - 1];

            symbolTable = new Dictionary<string, string>(StringComparer.Ordinal);

            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' después del método HTTP");

            while (!Match(Tools.TokenType.RBRACE))
            {
                AnalyzeStatement();
            }
        }

        private void AnalyzeStatement()
        {
            if (Match(Tools.TokenType.LET))
            {
                AnalyzeVariableDeclaration();
                Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de la declaración");
            }
            else if (Match(Tools.TokenType.RETURN))
            {
                if (Match(Tools.TokenType.ERROR))
                {
                    Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de return error");
                    return;
                }

                var type = AnalyzeExpression();
                Expect(Tools.TokenType.SEMICOLON, "Se esperaba ';' después de return");
            }
            else if (Match(Tools.TokenType.IF))
            {
                AnalyzeIfStatement();
            }
            else
            {
                throw new Exception($"[Semántico] Línea {Current.Line}: Sentencia no reconocida cerca de '{Current.Value}'");
            }
        }

        private void AnalyzeVariableDeclaration()
        {
            Expect(Tools.TokenType.IDENTIFIER, "Se esperaba un identificador después de 'let'");
            var idToken = tokens[current - 1];
            string id = idToken.Value;

            if (symbolTable.ContainsKey(id))
                throw new Exception($"[Semántico] Línea {idToken.Line}: Variable '{id}' ya declarada en este scope.");

            Expect(Tools.TokenType.EQUAL, "Se esperaba '=' en la asignación");

            string exprType = AnalyzeExpression();

            symbolTable[id] = exprType;
        }

        private void AnalyzeIfStatement()
        {
            Expect(Tools.TokenType.LPAREN, "Se esperaba '(' después de 'if'");
            string condType = AnalyzeExpression();

            if (condType != "bool")
                throw new Exception($"[Semántico] Línea {Current.Line}: condición del IF debe ser booleana (se obtuvo '{condType}').");

            Expect(Tools.TokenType.RPAREN, "Se esperaba ')' después de la condición if");
            Expect(Tools.TokenType.LBRACE, "Se esperaba '{' para abrir el bloque if");

            while (!Match(Tools.TokenType.RBRACE))
                AnalyzeStatement();

            if (Match(Tools.TokenType.ELSE))
            {
                Expect(Tools.TokenType.LBRACE, "Se esperaba '{' después de 'else'");
                while (!Match(Tools.TokenType.RBRACE))
                    AnalyzeStatement();
            }
        }

        private string AnalyzeExpression()
        {
            string leftType = AnalyzeTerm();

            while (Match(Tools.TokenType.SUMA, Tools.TokenType.RESTA,
                         Tools.TokenType.EQUAL_EQUAL, Tools.TokenType.NOT_EQUAL,
                         Tools.TokenType.MAYOR, Tools.TokenType.MENOR,
                         Tools.TokenType.GREATER_EQUAL, Tools.TokenType.LESS_EQUAL,
                         Tools.TokenType.AND, Tools.TokenType.OR))
            {
                var opToken = tokens[current - 1];
                string rightType = AnalyzeTerm();

                leftType = CombineTypesForOperator(leftType, rightType, opToken);
            }

            return leftType;
        }

        private string AnalyzeTerm()
        {
            if (Match(Tools.TokenType.INTEGER)) return "int";
            if (Match(Tools.TokenType.FLOAT)) return "float";
            if (Match(Tools.TokenType.STRING)) return "string";
            if (Match(Tools.TokenType.TRUE)) return "bool";
            if (Match(Tools.TokenType.FALSE)) return "bool";
            if (Match(Tools.TokenType.ERROR)) return "error";

            if (Match(Tools.TokenType.IDENTIFIER))
            {
                var id = tokens[current - 1].Value;
                if (!symbolTable.TryGetValue(id, out var t))
                {
                    throw new Exception($"[Semántico] Línea {tokens[current - 1].Line}: Variable '{id}' no declarada en este scope.");
                }
                return t;
            }

            if (Match(Tools.TokenType.LPAREN))
            {
                string inner = AnalyzeExpression();
                Expect(Tools.TokenType.RPAREN, "Se esperaba ')' después de la expresión");
                return inner;
            }

            throw new Exception($"[Semántico] Línea {Current.Line}: Expresión no válida cerca de '{Current.Value}'");
        }

        private string CombineTypesForOperator(string left, string right, Token opToken)
        {
            // Operadores de comparación (==, !=, >, <, >=, <=) se evalúan PRIMERO
            if (opToken.Type == Tools.TokenType.EQUAL_EQUAL || opToken.Type == Tools.TokenType.NOT_EQUAL ||
                opToken.Type == Tools.TokenType.MAYOR || opToken.Type == Tools.TokenType.MENOR ||
                opToken.Type == Tools.TokenType.GREATER_EQUAL || opToken.Type == Tools.TokenType.LESS_EQUAL)
            {
                // Tipos idénticos siempre son comparables
                if (left == right) return "bool";

                // int y float son compatibles para comparación
                if ((left == "int" && right == "float") || (left == "float" && right == "int"))
                    return "bool";
            
                throw new Exception($"[Semántico] Línea {opToken.Line}: Comparación '{opToken.Value}' inválida entre '{left}' y '{right}'.");
            }

            // Operadores lógicos (&&, ||) requieren booleanos
            if (opToken.Type == Tools.TokenType.AND || opToken.Type == Tools.TokenType.OR)
            {
                if (left != "bool" || right != "bool")
                    throw new Exception($"[Semántico] Línea {opToken.Line}: Operación lógica '{opToken.Value}' requiere ambos operandos booleanos. Recibido: '{left}' y '{right}'.");
                return "bool";
            }

            if (opToken.Type == Tools.TokenType.SUMA || opToken.Type == Tools.TokenType.RESTA ||
                opToken.Type == Tools.TokenType.MULT || opToken.Type == Tools.TokenType.DIV || 
                opToken.Type == Tools.TokenType.MOD)
            {
                if (opToken.Type == Tools.TokenType.SUMA && left == "string" && right == "string")
                    return "string";

                if ((left == "int" || left == "float") && (right == "int" || right == "float"))
                    return (left == "float" || right == "float") ? "float" : "int";

                throw new Exception($"[Semántico] Línea {opToken.Line}: Operación aritmética inválida entre '{left}' y '{right}'.");
            }

            throw new Exception($"[Semántico] Línea {opToken.Line}: Operador '{opToken.Value}' no soportado semánticamente.");
        }
    }
}