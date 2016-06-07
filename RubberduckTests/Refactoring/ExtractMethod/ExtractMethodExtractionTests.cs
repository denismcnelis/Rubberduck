﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Refactorings.ExtractMethod;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.VBEInterfaces.RubberduckCodeModule;

namespace RubberduckTests.Refactoring.ExtractMethod
{
    [TestClass]
    public class ExtractMethodExtractionTests
    {

#region inputCode
        string inputCode = @"
Option explicit
Public Sub CodeWithDeclaration()
    Dim x as long
    Dim y as long
    Dim z as long

    x = 1 + 2
    DebugPrint x
    y = x + 1
    x = 2
    DebugPrint y

    z = x
    DebugPrint z

End Sub
Public Sub DebugPrint(byval g as long)
End Sub


";
#endregion 

        [TestClass]
        public class WhenRemoveSelectionIsCalledWithValidSelection
        {
            [TestMethod]
            public void shouldRemoveLinesFromCodeModule()
            {

                var notifyCalls = new List<Tuple<int, int>>();
                var codeModule = new Mock<ICodeModuleWrapper>();
                codeModule.Setup(cm => cm.DeleteLines(It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<int, int>((start, count) => notifyCalls.Add(Tuple.Create(start, count)));
                var selections = new List<Selection>() { new Selection(5, 1, 5, 20), new Selection(10, 1, 12, 20) };
                var SUT = new ExtractMethodExtraction();
                SUT.removeSelection(codeModule.Object, selections);

                Assert.AreEqual(Tuple.Create(5, 1), notifyCalls[0]);
                Assert.AreEqual(Tuple.Create(10, 3), notifyCalls[1]);

            }
        }

        [TestClass]
        public class WhenApplyIsCalled
        {
            [TestMethod]
            [TestCategory("ExtractedMethodRefactoringTests")]
            public void shouldExtractTheTextForTheNewProcByCallingConstructLinesOfProc()
            {
                var newProc = @"
Public Sub NewMethod()
    DebugPrint ""a""
End Sub";
                var extraction = new Mock<ExtractMethodExtraction>() { CallBase = true };
                IExtractMethodExtraction SUT = extraction.Object;
                var codeModule = new Mock<ICodeModuleWrapper>();
                var model = new Mock<IExtractMethodModel>();
                var selection = new Selection(1, 1, 1, 1);
                var methodMock = new Mock<ExtractedMethod>() { CallBase = true }; 
                var method = methodMock.Object;
                method.Accessibility = Accessibility.Private;
                method.Parameters = new List<ExtractedParameter>();
                method.MethodName = "NewMethod";
                methodMock.Setup(m => m.NewMethodCall()).Returns("theMethodCall");
                model.Setup(m => m.PositionForNewMethod).Returns(selection);
                model.Setup(m => m.Method).Returns(method);
                extraction.Setup(em => em.constructLinesOfProc(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IExtractMethodModel>())).Returns(newProc);

                SUT.apply(codeModule.Object, model.Object, selection);

                extraction.Verify(extr => extr.constructLinesOfProc(codeModule.Object, model.Object));


            }

            [TestMethod]
            [TestCategory("ExtractedMethodRefactoringTests")]
            public void shouldRemoveSelection()
            {

                var newProc = @"
Public Sub NewMethod()
    DebugPrint ""a""
End Sub";
                var extraction = new Mock<ExtractMethodExtraction>() { CallBase = true };
                IExtractMethodExtraction SUT = extraction.Object;
                var codeModule = new Mock<ICodeModuleWrapper>();
                var selection = new Selection(1, 1, 1, 1);
                var selections = new List<Selection>() { new Selection(5, 1, 5, 20), new Selection(10, 1, 12, 20) };
                var methodMock = new Mock<ExtractedMethod>() { CallBase = true };
                var method = methodMock.Object;
                method.Accessibility = Accessibility.Private;
                method.Parameters = new List<ExtractedParameter>();
                method.MethodName = "NewMethod";
                methodMock.Setup(m => m.NewMethodCall()).Returns("theMethodCall");
                var model = new Mock<IExtractMethodModel>();
                model.Setup(m => m.PositionForNewMethod).Returns(selection);
                model.Setup(m => m.Method).Returns(method);
                model.Setup(m => m.SelectionToRemove).Returns(selections);

                extraction.Setup(em => em.constructLinesOfProc(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IExtractMethodModel>())).Returns("theMethodCall");

                SUT.apply(codeModule.Object, model.Object, selection);

                extraction.Verify(ext => ext.removeSelection(codeModule.Object, selections));
            }

            [TestMethod]
            [TestCategory("ExtractedMethodRefactoringTests")]
            public void shouldInsertMethodCall()
            {
                
                var extraction = new Mock<ExtractMethodExtraction>() { CallBase = true };
                IExtractMethodExtraction SUT = extraction.Object;
                var codeModule = new Mock<ICodeModuleWrapper>();
                var model = new Mock<IExtractMethodModel>();
                var selection = new Selection(7, 1, 7, 1);
                model.Setup(m => m.PositionForNewMethod).Returns(selection);
                var methodMock = new Mock<ExtractedMethod>() { CallBase = true };
                var method = methodMock.Object;
                method.Accessibility = Accessibility.Private;
                method.Parameters = new List<ExtractedParameter>();
                method.MethodName = "NewMethod";
                methodMock.Setup(m => m.NewMethodCall()).Returns("theMethodCall");
                model.Setup(m => m.Method).Returns(method);

                extraction.Setup(em => em.constructLinesOfProc(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IExtractMethodModel>())).Returns("theMethodCall");
                extraction.Setup(em => em.removeSelection(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IEnumerable<Selection>>()));

                var inserted = new List<Tuple<int,string>>();
                codeModule.Setup( cm => cm.InsertLines(It.IsAny<int>(),It.IsAny<string>()))
                    .Callback<int,string>( (line,data) => inserted.Add(Tuple.Create(line,data)));

                SUT.apply(codeModule.Object, model.Object, selection);

                // selection.StartLine = 7
                var expected = Tuple.Create(7,"theMethodCall");
                var actual = inserted[1];
                //Make sure the second insert inserted the methodCall higher up.
                Assert.AreEqual(expected ,actual);
            }

            [TestMethod]
            [TestCategory("ExtractedMethodRefactoringTests")]
            public void shouldInsertNewMethodAtGivenLineNoBeforeInsertingMethodCall()
            {
                var newProc = @"
Public Sub NewMethod()
    DebugPrint ""a""
End Sub";
                var extraction = new Mock<ExtractMethodExtraction>() { CallBase = true };
                IExtractMethodExtraction SUT = extraction.Object;
                var codeModule = new Mock<ICodeModuleWrapper>();
                var model = new Mock<IExtractMethodModel>();
                var selection = new Selection(1, 1, 1, 1);
                model.Setup(m => m.PositionForNewMethod).Returns(selection);
                var method = new ExtractedMethod();
                method.Accessibility = Accessibility.Private;
                method.Parameters = new List<ExtractedParameter>();
                method.MethodName = "NewMethod";
                model.Setup(m => m.Method).Returns(method);
                extraction.Setup(em => em.constructLinesOfProc(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IExtractMethodModel>())).Returns(newProc);
                extraction.Setup(em => em.removeSelection(It.IsAny<ICodeModuleWrapper>(), It.IsAny<IEnumerable<Selection>>()));

                var inserted = new List<Tuple<int,string>>();
                codeModule.Setup( cm => cm.InsertLines(It.IsAny<int>(),It.IsAny<string>())).Callback<int,string>( (line,data) => inserted.Add(Tuple.Create(line,data)));
                SUT.apply(codeModule.Object, model.Object, selection);

                var expected = Tuple.Create(selection.StartLine,newProc);
                var actual = inserted[0];
                //Make sure the first insert inserted the rows.
                Assert.AreEqual(expected,actual);

            }
        }
        [TestClass]
        public class WhenConstructLinesOfProcIsCalledWithAListOfSelections
        {
            [TestMethod]
            [TestCategory("ExtractedMethodRefactoringTests")]
            public void shouldConcatenateASeriesOfLines()
            {

                var notifyCalls = new List<Tuple<int, int>>();
                var codeModule = new Mock<ICodeModuleWrapper>();
                codeModule.Setup(cm => cm.get_Lines(It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<int, int>((start, count) => notifyCalls.Add(Tuple.Create(start, count)));
                var selections = new List<Selection>() { new Selection(5, 1, 5, 20), new Selection(10, 1, 12, 20) };
                var model = new Mock<IExtractMethodModel>();
                var method = new ExtractedMethod();
                method.Accessibility = Accessibility.Private;
                method.Parameters = new List<ExtractedParameter>();
                method.MethodName = "NewMethod";
                model.Setup(m => m.SelectionToRemove).Returns(selections);
                model.Setup(m => m.Method).Returns(method);

                var SUT = new ExtractMethodExtraction();
                SUT.constructLinesOfProc(codeModule.Object, model.Object);

                Assert.AreEqual(Tuple.Create(5, 1), notifyCalls[0]);
                Assert.AreEqual(Tuple.Create(10, 3), notifyCalls[1]);


            }

        }
    }
}
