using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BepInEx.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HarmonyMethodRefParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Harmony003";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HarmonyMethodRefParametersAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HarmonyMethodRefParametersAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HarmonyMethodRefParametersAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Method Declaration";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);

            if (!HarmonyUtil.IsMethodHarmonyPatchRelated(ref context, method, symbol))
                return;

            if (method.ParameterList?.Parameters.Count <= 0)
                return;

            var assignments = context.Node.DescendantNodes().OfType<AssignmentExpressionSyntax>();
            var varNames = assignments.Select(e => e.Left.ToString()).ToList();
            CheckExpressionSyntaxes(context, method, varNames, assignments.Cast<ExpressionSyntax>().ToList());
        }

        private static void CheckExpressionSyntaxes(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method,
            List<string> varNames, List<ExpressionSyntax> expressionSyntaxes)
        {
            for (int i = 0; i < varNames.Count(); i++)
            {
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken);

                    var parameterName = parameterSymbol.Name;
                    if (parameterSymbol.RefKind == RefKind.None && parameterName == varNames[i])
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, expressionSyntaxes[i].GetLocation(), expressionSyntaxes[i].ToString()));
                    }
                }
            }
        }
    }
}
