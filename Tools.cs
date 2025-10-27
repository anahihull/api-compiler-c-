using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{
    public class Tools
    {
        public enum TokenType
        {
            // API/Service Keywords
            SERVICE, ROUTE, ENVROUTE, GET, POST, PUT, PATCH, DELETE,
            PARAMS, QUERY, BODY, STATUS, RETURN, ERROR, SCHEMA,
            REQUIRED, OPTIONAL, DEFAULT, USE, BEFORE, AFTER,
            AUTH, ALLOW, DENY, LET, CONST,

            // Traditional Keywords  
            IF, ELSE, WHILE, FOR, BREAK, CONTINUE, VAR, INT, BOOL, NULL, TRUE, FALSE,
            FUNCTION, CLASS, PUBLIC, PRIVATE, PROTECTED, STATIC, VOID, NEW, THIS,

            // Operators
            SUMA, RESTA, MULT, DIV, MOD,
            PLUS_EQUAL, MINUS_EQUAL, MULT_EQUAL, DIV_EQUAL, MOD_EQUAL,
            EQUAL, EQUAL_EQUAL, NOT_EQUAL,
            MAYOR, MENOR, GREATER_EQUAL, LESS_EQUAL,
            AND, OR, NOT,

            // Delimiters
            LPAREN, RPAREN, LBRACE, RBRACE, LBRACKET, RBRACKET,
            COMMA, SEMICOLON, DOT, COLON, PIPE,

            // Literals
            INTEGER, FLOAT, STRING, IDENTIFIER,

            // Path Parameters
            PATH_PARAM,

            // Comments
            COMMENT,

            // Special
            INVALID, WHITESPACE, EOF
        }
    }

}
