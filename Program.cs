using System;
using System.Collections.Generic;
using System.IO;
using Compilador;

class Program
{
    static void Main(string[] args)
    {
        string filePath = args.Length > 0 ? args[0] : "C:\\Users\\kajhu\\OneDrive\\Desktop\\api-compiler-c-\\operacionesaritmeticas.txt";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ No se encontró el archivo: {filePath}");
            Environment.Exit(1);
        }

        string input;
        try
        {
            input = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al leer el archivo: {ex.Message}");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("=== 🔹 Análisis Léxico ===\n");
        var lexer = new Lexer(input);
        var tokenList = lexer.Tokenize();

        foreach (var token in tokenList)
            Console.WriteLine(token);

        Console.WriteLine("\n=== 🔹 Análisis Sintáctico ===\n");

        try
        {
            var parser = new Parser(tokenList);
            parser.Parse();
            Console.WriteLine("\n✔ Análisis sintáctico completado sin errores.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error de sintaxis: {ex.Message}");
        }

        Console.WriteLine("\n=== 🔹 Análisis Semántico ===\n");
        try
        {
            var semantic = new SemanticAnalyzer(tokenList);
            semantic.Analyze();
            Console.WriteLine("\n✔ Análisis semántico completado sin errores.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error semántico: {ex.Message}");
        }

     Console.WriteLine("\n=== 🔹 Fin de la compilación ===");
    }
}