using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BooleanPInvokeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BooleanPInvokeAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BooleanPInvokeAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Interoperability";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var methodSymbol = (IMethodSymbol)context.Symbol;

            if(methodSymbol.IsExtern
                && methodSymbol.Parameters.Any(p => p.Type.SpecialType == SpecialType.System_Boolean 
                    && !p.GetAttributes().Any(att => att.AttributeClass.Name.Equals("MarshalAsAttribute"))))
            {
                var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
