using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Antlr4.Runtime;
using Microsoft.Vbe.Interop;
using Rubberduck.Inspections;
using Rubberduck.VBA;
using Rubberduck.VBA.Grammar;

namespace Rubberduck.Extensions
{
    [ComVisible(false)]
    public static class VbeExtensions
    {
        /// <summary>
        /// Finds all code modules that match the specified project and component names.
        /// </summary>
        /// <param name="vbe"></param>
        /// <param name="projectName"></param>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public static IEnumerable<CodeModule> FindCodeModules(this VBE vbe, string projectName, string componentName)
        {
            var matches = 
                vbe.VBProjects.Cast<VBProject>()
                              .Where(project => project.Name == projectName)
                              .SelectMany(project => project.VBComponents.Cast<VBComponent>()
                                                                         .Where(component => component.Name == componentName))
                              .Select(component => component.CodeModule);
            return matches;
        }

        public static CodeModuleSelection FindInstruction(this VBE vbe, QualifiedModuleName qualifiedModuleName, ParserRuleContext context)
        {
            var projectName = qualifiedModuleName.ProjectName;
            var componentName = qualifiedModuleName.ModuleName;

            var modules = FindCodeModules(vbe, projectName, componentName);
            foreach (var module in modules)
            {
                var selection = context.GetSelection();

                if (module.Lines[selection.StartLine, selection.LineCount]
                    .Replace(" _\n", " ").Contains(context.GetText()))
                {
                    return new CodeModuleSelection(module, selection);
                }
            }

            return null;
        }

        /// <summary> Returns the type of Office Application that is hosting the VBE. </summary>
        /// <returns> Returns null if Unit Testing does not support Host Application.</returns>
        public static IHostApplication HostApplication(this VBE vbe)
        {
            foreach (Reference reference in vbe.ActiveVBProject.References)
            {
                if (reference.BuiltIn && reference.Name != "VBA")
                {
                    if (reference.Name == "Excel") return new ExcelApp();
                    if (reference.Name == "Access") return new AccessApp();
                    if (reference.Name == "Word") return new WordApp();
                    if (reference.Name == "PowerPoint") return new PowerPointApp();
                    if (reference.Name == "Outlook") return new OutlookApp();
                    if (reference.Name == "Publisher") return new PublisherApp();
                }
            }

            return null;
        }
    }
}