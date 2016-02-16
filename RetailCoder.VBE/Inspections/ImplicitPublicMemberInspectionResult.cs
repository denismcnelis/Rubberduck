using System.Collections.Generic;
using Antlr4.Runtime;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.UI;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections
{
    public class ImplicitPublicMemberInspectionResult : InspectionResultBase
    {
        private readonly IEnumerable<CodeInspectionQuickFix> _quickFixes;

        public ImplicitPublicMemberInspectionResult(IInspection inspection, QualifiedContext<ParserRuleContext> qualifiedContext, Declaration item)
            : base(inspection, item)
        {
            _quickFixes = new CodeInspectionQuickFix[]
            {
                new SpecifyExplicitPublicModifierQuickFix(qualifiedContext.Context, QualifiedSelection), 
                new IgnoreOnceQuickFix(qualifiedContext.Context, QualifiedSelection, Inspection.AnnotationName), 
            };
        }

        public override IEnumerable<CodeInspectionQuickFix> QuickFixes { get { return _quickFixes; } }

        public override string Description
        {
            get
            {
                return string.Format(InspectionsUI.ImplicitPublicMemberInspectionResultFormat, Target.IdentifierName);
            }
        }
    }

    public class SpecifyExplicitPublicModifierQuickFix : CodeInspectionQuickFix
    {
        public SpecifyExplicitPublicModifierQuickFix(ParserRuleContext context, QualifiedSelection selection)
            : base(context, selection, RubberduckUI.Inspections_SpecifyPublicModifierExplicitly)
        {
        }

        public override void Fix()
        {
            var oldContent = Context.GetText();
            var newContent = Tokens.Public + ' ' + oldContent;

            var selection = Context.GetSelection();

            var module = Selection.QualifiedName.Component.CodeModule;
            var lines = module.get_Lines(selection.StartLine, selection.LineCount);

            module.DeleteLines(selection.StartLine, selection.LineCount);
            module.InsertLines(selection.StartLine, newContent);
        }
    }
}