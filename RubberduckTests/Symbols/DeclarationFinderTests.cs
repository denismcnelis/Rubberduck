
using System.CodeDom;
using NUnit.Framework;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor.SafeComWrappers;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using RubberduckTests.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rubberduck.VBEditor;
using Antlr4.Runtime;
using Rubberduck.Common;

namespace RubberduckTests.Symbols
{

    [TestFixture]
    public class DeclarationFinderTests
    {

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_InProcedure_MethodDeclaration()
        {
            var expectedResults = new string[]
            {
                "member1",
                "adder",
                "Foo"
            };

            var moduleContent1 = 
            @"

Private member1 As Long

Public Function Foo() As Long   'Selecting 'Foo' to rename
    Dim adder as Long
    adder = 10
    member1 = member1 + adder
    Foo = member1
End Function
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "CFirstClass", "Foo", "Function Foo() As Long");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.ClassModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_InProcedure_LocalVariableReference()
        {
            var expectedResults = new string[]
            {
                "member1",
                "Foo"
            };

            var moduleContent1 =
            @"

Private member1 As Long

Public Function Foo() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder   'Selecting 'adder' to rename
    Foo = member1
End Function
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modTest", "adder", "member1 + adder");
            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_InProcedure_MemberDeclaration()
        {
            var expectedResults = new string[]
            {
                "adder",
                "member1",
                "Foo"
            };

            var moduleContent1 =
@"
Private member1 As Long

Public Function Foo() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder       'Selecting member1 to rename
    Foo = member1
End Function
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "CFirstClass", "member1", "member1 + adder");
            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.ClassModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_ModuleScope()
        {
            var expectedResults = new string[]
            {
                "adder",
                "Foo"
            };

            var moduleContent1 =
            @"

Private member1 As Long

Public Function Foo() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder       'Selecting 'member1' to rename
    Foo = member1
End Function
";
            var moduleContent2 =
            @"

Private member11 As Long
Public member2 As Long

Public Function Foo2() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder
    Foo2 = member1
End Function

Private Sub Bar()
    member2 = member2 * 4
End Sub
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "CFirstClass", "member1", "member1 + adder");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.ClassModule);
            AddTestComponent(tdo, "modOne", moduleContent2, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_PublicClassAndPublicModuleSub_RenameClassSub()
        {
            var expectedResults = new string[]
            {
                "Foo"
            };

            var moduleContent1 = 
            @"
Public Function Foo() As Long   'Selecting 'Foo' to rename
    Foo = 5
End Function
";
            var moduleContent2 =
            @"
Public Function Foo2() As Long
    Foo2 = 2
End Function
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "CFirstClass", "Foo", "Function Foo() As Long");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.ClassModule);
            AddTestComponent(tdo, "modOne", moduleContent2, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_Module_To_ClassScope()
        {
            var expectedResults = new string[]
            {
                "Foo2",
                "Bar",
                "member11"
            };

            var moduleContent1 =
            @"

Private member11 As Long
Public member2 As Long

Public Function Foo2() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder
    Foo2 = member1
End Function

Private Sub Bar()
    member2 = member2 * 4   'Selecting member2 to rename
End Sub
";
            var moduleContent2 =
            @"

Private member1 As Long

Public Function Foo() As Long
    Dim adder as Long
    adder = 10
    member1 = member1 + adder
    Foo = member1
End Function
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modOne", "member2", "member2 * 4");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);
            AddTestComponent(tdo, "CFirstClass", moduleContent2, ComponentType.ClassModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_PrivateSub_RespectPublicSubInOtherModule()
        {
            var expectedResults = new string[]
            {
                "DoThis",
                "filename",
                "member1"
            };

            var moduleContent1 =
@"
Private Sub DoThis(filename As String)
    SetFilename filename            'Selecting 'SetFilename' to rename
End Sub
";
            var moduleContent2 =
@"
Private member1 As String

Public Sub SetFilename(filename As String)
    member1 = filename
End Sub
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modOne", "SetFilename", "SetFilename filename");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);
            AddTestComponent(tdo, "modTwo", moduleContent2, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_PrivateSub_MultipleReferences()
        {
            var expectedResults = new string[]
            {
                "DoThis",
                "filename",
                "member1",
                "StoreFilename",
                "ExtractFilename",
                "mFolderpath",
                "filepath"
            };

            var moduleContent1 =
@"
Private Sub DoThis(filename As String)
    SetFilename filename       'Selecting 'SetFilename' to rename
End Sub
";
            var moduleContent2 =
@"
Private member1 As String

Public Sub SetFilename(filename As String)
    member1 = filename
End Sub
";
            var moduleContent3 =
@"
Private mFolderpath As String

Private Sub StoreFilename(filepath As String)
    Dim filename As String
    filename = ExtractFilename(filepath)
    SetFilename filename
End Sub

Private Function ExtractFilename(filepath As String) As String
    ExtractFilename = filepath
End Function"
;

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modOne", "SetFilename", "SetFilename filename");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);
            AddTestComponent(tdo, "modTwo", moduleContent2, ComponentType.StandardModule);
            AddTestComponent(tdo, "modThree", moduleContent3, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_PrivateSub_WithBlock()
        {
            var expectedResults = new string[]
            {
                "mFolderpath",
                "ExtractFilename",
                "SetFilename",
                "filename",
                "input",
                "Bar"
            };

            var moduleContent1 =
@"
Private myData As String
Private mDupData As String

Public Sub Foo(filenm As String)
    Dim filepath As String
    filepath = ""C:\MyStuff\"" & filenm
    Dim helper As CFileHelper
    Set helper = new CFileHelper
    With helper
        .StoreFilename filepath     'Selecting 'StoreFilename' to rename
        mDupData = filepath
    End With
End Sub

Public Sub StoreFilename(filename As String)
    myData = filename
End Sub
";
            var moduleContent2 =
@"
Private mFolderpath As String

Public Sub StoreFilename(input As String)
    Dim filename As String
    filename = ExtractFilename(input)
    SetFilename filename
End Sub

Private Function ExtractFilename(filepath As String) As String
    ExtractFilename = filepath
End Function

Public Sub Bar()
End Sub
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modOne", "StoreFilename", ".StoreFilename filepath");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);
            AddTestComponent(tdo, "CFileHelper", moduleContent2, ComponentType.ClassModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        [Test]
        [Category("Resolver")]
        public void DeclarationFinder_Module_To_ModuleScopeResolution()
        {
            var expectedResults = new string[]
            {
                "Foo1",
                "Foo2",
                "Foo3",
                "Foo4",
                "gConstant",
                "member2"
            };

            var moduleContent1 =
@"
Private member11 As Long
Public member2 As Long

Private Function Bar1() As Long
    Bar2
    Bar1 = member2 + modTwo.Foo1 + modTwo.Foo2 + modTwo.Foo3   'Selecting Foo2 to rename
End Function

Private Sub Bar2()
    member2 = member2 * 4 
End Sub
";
            var moduleContent2 =
@"
Public Const gConstant As Long = 10

Public Function Foo1() As Long
    Foo1 = 1
End Function

Public Function Foo2() As Long
    Foo2 = 2
End Function

Public Function Foo3() As Long
    Foo3 = 3
End Function

Private Sub Foo4()

End Sub
";

            var tdo = new AccessibilityTestsDataObject();
            AddTestSelectionCriteria(tdo, "modOne", "Foo2", "Foo2 + modTwo.Foo3");

            AddTestComponent(tdo, tdo.SelectionModuleName, moduleContent1, ComponentType.StandardModule);
            AddTestComponent(tdo, "modTwo", moduleContent2, ComponentType.StandardModule);

            TestAccessibleDeclarations(tdo, expectedResults);
        }

        private void TestAccessibleDeclarations(AccessibilityTestsDataObject tdo, string[] testSpecificExpectedResults)
        {

            PrepareScenarioTestData(tdo, testSpecificExpectedResults);

            var target = tdo.Parser.AllUserDeclarations.FindTarget(tdo.QualifiedSelection);
            if(null == target)  { Assert.Inconclusive("Unable to find target from QualifiedSelection"); }

            var accessibleNames = tdo.Parser.DeclarationFinder.GetDeclarationsWithIdentifiersToAvoid(target)
                    .Select(d => d.IdentifierName);

            Assert.IsFalse(accessibleNames.Except(tdo.ExpectedResults).Any()
                    || tdo.ExpectedResults.Except(accessibleNames).Any()
                        , BuildIdentifierListToDisplay(accessibleNames.Except(tdo.ExpectedResults), tdo.ExpectedResults.Except(accessibleNames)));
        }

        private void PrepareScenarioTestData(AccessibilityTestsDataObject tdo, string[] testSpecificExpectedResults)
        {
            SetExpectedResults(tdo, testSpecificExpectedResults);

            tdo.VBE = BuildProject(tdo.ProjectName, tdo.Components);
            tdo.Parser = MockParser.CreateAndParse(tdo.VBE);

            CreateQualifiedSelectionForTestCase(tdo);
        }

        private void SetExpectedResults(AccessibilityTestsDataObject tdo, string[] testSpecificExpectedResults)
        {
            tdo.ExpectedResults = new List<string>();
            tdo.ExpectedResults.AddRange(testSpecificExpectedResults);

            //Add module name(s) and project name
            tdo.ExpectedResults.Add(tdo.SelectionTarget);
            tdo.Components.ForEach(c => tdo.ExpectedResults.Add(c.Name));
            tdo.ExpectedResults.Add(tdo.ProjectName);
        }

        private string BuildIdentifierListToDisplay(IEnumerable<string> extraIdentifiers, IEnumerable<string> missedIdentifiers)
        {
            var extraNamesPreface = "Returned unexpected identifier(s): ";
            var missedNamesPreface = "Did not return expected identifier(s): ";
            string extraResults = string.Empty;
            string missingResults = string.Empty;
            if (extraIdentifiers.Any())
            {
                extraResults = extraNamesPreface + GetListOfNames(extraIdentifiers);
            }
            if (missedIdentifiers.Any())
            {
                missingResults = missedNamesPreface + GetListOfNames(missedIdentifiers);
            }

            return "\r\n" + extraResults + "\r\n" + missingResults;
        }

        private string GetListOfNames(IEnumerable<string> identifiers)
        {
            if (!identifiers.Any()) { return ""; }

            string result = string.Empty;
            string postPend = "', ";
            foreach (var identifier in identifiers)
            {
                result = result + "'" + identifier + postPend;
            }
            return result.Remove(result.Length - postPend.Length + 1);
        }

        private void CreateQualifiedSelectionForTestCase(AccessibilityTestsDataObject tdo)
        {
            var component = RetrieveComponent(tdo, tdo.SelectionModuleName);
            var moduleContent = component.CodeModule.GetLines(1, component.CodeModule.CountOfLines);

            var splitToken = new string[] { "\r\n" };

            var lines = moduleContent.Split(splitToken, System.StringSplitOptions.None);
            int lineOfInterestNumber = 0;
            string lineOfInterestContent = string.Empty;
            for (int idx = 0; idx < lines.Count() && lineOfInterestNumber < 1; idx++)
            {
                if (lines[idx].Contains(tdo.SelectionLineIdentifier))
                {
                    lineOfInterestNumber = idx + 1;
                    lineOfInterestContent = lines[idx];
                }
            }
            Assert.IsTrue(lineOfInterestNumber > 0, "Unable to find target '" + tdo.SelectionTarget + "' in " + tdo.SelectionModuleName + " content.");
            var column = lineOfInterestContent.IndexOf(tdo.SelectionLineIdentifier);
            column = column + tdo.SelectionLineIdentifier.IndexOf(tdo.SelectionTarget) + 1;

            var moduleParent = component.CodeModule.Parent;
            tdo.QualifiedSelection = new QualifiedSelection(new QualifiedModuleName(moduleParent), new Selection(lineOfInterestNumber, column, lineOfInterestNumber, column));
        }

        private void AddTestComponent(AccessibilityTestsDataObject tdo, string moduleIdentifier, string moduleContent, ComponentType componentType)
        {
            if (null == tdo.Components)
            {
                tdo.Components = new List<TestComponentSpecification>();
            }
            tdo.Components.Add(new TestComponentSpecification(moduleIdentifier, moduleContent, componentType));
        }

        private IVBE BuildProject(string projectName, List<TestComponentSpecification> testComponents)
        {
            var builder = new MockVbeBuilder();
            var enclosingProjectBuilder = builder.ProjectBuilder(projectName, ProjectProtection.Unprotected);

            testComponents.ForEach(c => enclosingProjectBuilder.AddComponent(c.Name, c.ModuleType, c.Content));
            var enclosingProject = enclosingProjectBuilder.Build();
            builder.AddProject(enclosingProject);
            return builder.Build().Object;
        }

        private IVBComponent RetrieveComponent(AccessibilityTestsDataObject tdo, string componentName)
        {
            var vbProject = tdo.VBE.VBProjects.Where(item => item.Name == tdo.ProjectName).SingleOrDefault();
            return vbProject.VBComponents.Where(item => item.Name == componentName).SingleOrDefault();
        }

        private void AddTestSelectionCriteria(AccessibilityTestsDataObject tdo, string moduleName, string selectionTarget, string selectionLineIdentifier)
        {
            tdo.SelectionModuleName = moduleName;
            tdo.SelectionTarget = selectionTarget;
            tdo.SelectionLineIdentifier = selectionLineIdentifier;
        }

        internal class TestComponentSpecification
        {
            private string _name;
            private string _content;
            private ComponentType _componentType;
            public TestComponentSpecification(string componentName, string componentContent, ComponentType componentType)
            {
                _name = componentName;
                _content = componentContent;
                _componentType = componentType;
            }

            public string Name { get { return _name; } }
            public string Content { get { return _content; } }
            public ComponentType ModuleType { get { return _componentType; } }
        }


        internal class AccessibilityTestsDataObject
        {
            public AccessibilityTestsDataObject()
            {
                ProjectName = "TestProject";
            }
            public IVBE VBE { get; set; }
            public RubberduckParserState Parser { get; set; }
            public List<TestComponentSpecification> Components { get; set; }
            public string ProjectName { get; set; }
            public string SelectionModuleName { get; set; }
            public string SelectionTarget { get; set; }
            public string SelectionLineIdentifier { get; set; }
            public List<string> ExpectedResults { get; set; }
            public QualifiedSelection QualifiedSelection { get; set; }
        }
        
        [Test]
        [Category("Resolver")]
        public void FiendishlyAmbiguousNameSelectsSmallestScopedDeclaration()
        {
            var code =
            @"
Option Explicit

Public Sub foo()
    Dim foo As Long
    foo = 42
    Debug.Print foo
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("foo", ProjectProtection.Unprotected)
                .AddComponent("foo", ComponentType.StandardModule, code, new Selection(6, 6))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Variable).Single();
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void AmbiguousNameSelectsSmallestScopedDeclaration()
        {
            var code =
@"
Option Explicit

Public Sub foo()
    Dim foo As Long
    foo = 42
    Debug.Print foo
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(6, 6))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Variable).Single();
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void AmbiguousNameSelectsParameterOverProperty()
        {
            var code =
                @"
Option Explicit

Public Property Get Item()
    Item = 12
End Property

Public Property Let Item(ByVal Item As Variant)
    DoSomething Item
End Property

Private Sub DoSomething(ByVal value As Variant)
    Debug.Print value
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(9, 18))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Parameter).Single(d => d.IdentifierName == "Item");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void AmbiguousNameSelectsParameterOverSub()
        {
            var code =
                @"
Option Explicit

Public Sub foo(ByVal foo As Bookmarks)
    Dim bar As Bookmark
    For Each bar In foo
        Debug.Print bar.Name
    Next
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(6, 22))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Parameter).Single(d => d.IdentifierName == "foo");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void SameNameForProjectAndClass_ScopedDeclaration_ProjectSelection()
        {
            var refEditClass = @"
Option Explicit

Private ValueField As Variant

Public Property Get Value()
  Value = ValueField
End Property

Public Property Let Value(Value As Variant)
  ValueField = Value
End Property";

            var code =
                @"
Option Explicit

Public Sub foo()
    Dim myEdit As RefEdit.RefEdit
    Set myEdit = New RefEdit.RefEdit

    myEdit.Value = ""abc""
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("RefEdit", ProjectProtection.Unprotected)
                .AddComponent("RefEdit", ComponentType.ClassModule, refEditClass)
                .AddComponent("Test", ComponentType.StandardModule, code, new Selection(6, 23))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Project).Single();
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void SameNameForProjectAndClass_ScopedDeclaration_ClassSelection()
        {
            var refEditClass = @"
Option Explicit

Private ValueField As Variant

Public Property Get Value()
  Value = ValueField
End Property

Public Property Let Value(Value As Variant)
  ValueField = Value
End Property";

            var code =
                @"
Option Explicit

Public Sub foo()
    Dim myEdit As RefEdit.RefEdit
    Set myEdit = New RefEdit.RefEdit

    myEdit.Value = ""abc""
End Sub
";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("RefEdit", ProjectProtection.Unprotected)
                .AddComponent("RefEdit", ComponentType.ClassModule, refEditClass)
                .AddComponent("Test", ComponentType.StandardModule, code, new Selection(6, 31))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.ClassModule).Single();
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Test]
        [Category("Resolver")]
        public void SameNameForProjectAndClassImplicit_ScopedDeclaration()
        {
            var refEditClass = @"
Option Explicit

Private ValueField As Variant

Public Property Get Value()
  Value = ValueField
End Property

Public Property Let Value(Value As Variant)
  ValueField = Value
End Property";

            var code =
                @"
Option Explicit

Public Sub foo()
    Dim myEdit As RefEdit
    Set myEdit = New RefEdit

    myEdit.Value = ""abc""
End Sub
";

            var vbe = new MockVbeBuilder()
                .ProjectBuilder("RefEdit", ProjectProtection.Unprotected)
                .AddComponent("RefEdit", ComponentType.ClassModule, refEditClass)
                .AddComponent("Test", ComponentType.StandardModule, code, new Selection(7, 6))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = ParserState.ResolverError;
            var actual = parser.State.Status;

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected, actual);
        }

        [Test]
        [Category("Resolver")]
        public void Resolve_RecursiveCall_AssignmentLHS()
        {
            var sillyClass = @"
Option Explicit

Public Function Class1() As Class1
    Set Class1 = Me
End Function";

            var code =
                @"
Option Explicit

Public Function Class1(this As Class1) As Class1
    Set this = New Class1
    
    Set Class1 = Class1(this)
End Function";

            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("Class1", ComponentType.ClassModule, sillyClass)
                .AddComponent("Test", ComponentType.StandardModule, code, new Selection(7, 10))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());
            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Function).Single(d => d.QualifiedModuleName.ComponentName == "Test");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected, actual);
        }

        [Test]
        [Category("Resolver")]
        public void Resolve_RecursiveCall_AssignmentRHS()
        {
            var sillyClass = @"
Option Explicit

Public Function Class1() As Class1
    Set Class1 = Me
End Function";

            var code =
                @"
Option Explicit

Public Function Class1(this As Class1) As Class1
    Set this = New Class1
    
    Set Class1 = Class1(this)
End Function";

            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("Class1", ComponentType.ClassModule, sillyClass)
                .AddComponent("Test", ComponentType.StandardModule, code, new Selection(7, 19))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Function).Single(d => d.QualifiedModuleName.ComponentName == "Test");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected, actual);
        }

        [Test]
        [Category("Resolver")]
        public void Resolve_RecursiveCall_PropertyAssignment()
        {
            var sillyClass = @"
Option Explicit

Public Function Class1() As Class1
    Set Class1 = Me
End Function";

            var code =
                @"
Option Explicit

Public Function Class1(this As Class1) As Class1
    Set this = New Class1
    
    Set Class1 = Class1(this)
End Function";

            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("Test", ComponentType.StandardModule, code)
                .AddComponent("Class1", ComponentType.ClassModule, sillyClass, new Selection(4, 10))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Function).Single(d => d.QualifiedModuleName.ComponentName == "Class1");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected, actual);
        }

        [Category("Resolver")]
        [Test]
        public void Identify_NamedParameter_Parameter()
        {
            const string code = @"
Public Function Foo(Item As String) As Boolean
    MsgBox (Item)
End Function

Public Sub DoIt()
    Dim Result As Boolean
    Dim Item As String
    
    Item = ""abc""
    Result = Foo(Item:=Item)
End Sub";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(11, 19))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Parameter).Single();
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Category("Resolver")]
        [Test]
        public void Identify_NamedParameter_LocalVariable()
        {
            const string code = @"
Public Function Foo(Item As String) As Boolean
    MsgBox (Item)
End Function

Public Sub DoIt()
    Dim Result As Boolean
    Dim Item As String
    
    Item = ""abc""
    Result = Foo(Item:=Item)
End Sub";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(11, 25))
                .AddProjectToVbeBuilder()
                .Build();

            var parser = MockParser.Create(vbe.Object);
            parser.Parse(new CancellationTokenSource());

            var expected = parser.State.DeclarationFinder.DeclarationsWithType(DeclarationType.Variable)
                .Single(p => p.IdentifierName == "Item");
            var actual = parser.State.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Category("Resolver")]
        [Test]
        public void Identify_NamedParameter_Parameter_FromExcel()
        {
            const string code = @"
Public Sub DoIt()
    Dim sht As WorkSheet

    sht.Paste Link:=True
End Sub";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(5, 16))
                .AddReference("Excel", MockVbeBuilder.LibraryPathMsExcel)
                .AddProjectToVbeBuilder()
                .Build();

            var state = MockParser.CreateAndParse(vbe.Object, serializedDeclarationsPath: null, testLibraries:new[] { "Excel.1.8.xml" });
            
            var expected = state.DeclarationFinder.DeclarationsWithType(DeclarationType.Parameter).Single(p => !p.IsUserDefined && p.IdentifierName=="Link" && p.ParentScope == "EXCEL.EXE;Excel.Worksheet.Paste");
            var actual = state.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }
        [Category("Resolver")]
        [Test]
        [Ignore("Need to fix the default member access for function calls; see case #3937")]
        public void Identify_NamedParameter_Parameter_FromExcel_DefaultAccess()
        {
            // Note that ColumnIndex is actually a parameter of the _Default default member
            // of the Excel.Range object.
            const string code = @"
Public Sub DoIt()
    Dim foo As Variant
    Dim sht As WorkSheet

    foo = sht.Cells(ColumnIndex:=12).Value
End Sub";
            var vbe = new MockVbeBuilder()
                .ProjectBuilder("TestProject", ProjectProtection.Unprotected)
                .AddComponent("TestModule", ComponentType.StandardModule, code, new Selection(6, 22))
                .AddReference("Excel", MockVbeBuilder.LibraryPathMsExcel)
                .AddProjectToVbeBuilder()
                .Build();

            var state = MockParser.CreateAndParse(vbe.Object, serializedDeclarationsPath: null, testLibraries: new[] { "Excel.1.8.xml" });

            var expected = state.DeclarationFinder.DeclarationsWithType(DeclarationType.Parameter).Single(p => !p.IsUserDefined && p.IdentifierName == "ColumnIndex" && p.ParentScope == "EXCEL.EXE;Excel.Range._Default");
            var actual = state.DeclarationFinder.FindSelectedDeclaration(vbe.Object.ActiveCodePane);

            Assert.AreEqual(expected, actual, "Expected {0}, resolved to {1}", expected.DeclarationType, actual.DeclarationType);
        }

        [Category("Resolver")]
        [Test]
        public void DeclarationFinderCanCopeWithMultipleModulesImplementingTheSameInterface()
        {
            var project = GetTestProject("testProject");
            var interf = GetTestClassModule(project, "interface");
            var member = GetTestFunction(interf, "testMember", Accessibility.Public);
            var implementingClass1 = GetTestClassModule(project, "implementingClass1");
            var implementingClass2 = GetTestClassModule(project, "implementingClass2");
            var implementsContext1 = new VBAParser.ImplementsStmtContext(null, 0);
            var implementsContext2 = new VBAParser.ImplementsStmtContext(null, 0);
            AddReference(interf, implementingClass1, implementsContext1);
            AddReference(interf, implementingClass1, implementsContext2);
            var declarations = new List<Declaration> { interf, member, implementingClass1, implementingClass2 };

            DeclarationFinder finder = new DeclarationFinder(declarations, new List<Rubberduck.Parsing.Annotations.IAnnotation>(), new List<UnboundMemberDeclaration>());
            var interfaceDeclarations = finder.FindAllInterfaceMembers().ToList();

            Assert.AreEqual(1, interfaceDeclarations.Count());
        }

        private static ClassModuleDeclaration GetTestClassModule(Declaration projectDeclatation, string name, bool isExposed = false)
        {
            var qualifiedClassModuleMemberName = new QualifiedMemberName(StubQualifiedModuleName(name), name);
            var classModuleAttributes = new Rubberduck.Parsing.VBA.Attributes();
            if (isExposed)
            {
                classModuleAttributes.AddExposedClassAttribute();
            }
            return new ClassModuleDeclaration(qualifiedClassModuleMemberName, projectDeclatation, name, true, null, classModuleAttributes);
        }

        private static ProjectDeclaration GetTestProject(string name)
        {
            var qualifiedProjectName = new QualifiedMemberName(StubQualifiedModuleName("proj"), name);
            return new ProjectDeclaration(qualifiedProjectName, name, true, null);
        }

        private static QualifiedModuleName StubQualifiedModuleName(string name)
        {
            return new QualifiedModuleName("dummy", "dummy", name);
        }

        private static FunctionDeclaration GetTestFunction(Declaration moduleDeclatation, string name, Accessibility functionAccessibility)
        {
            var qualifiedFunctionMemberName = new QualifiedMemberName(moduleDeclatation.QualifiedName.QualifiedModuleName, name);
            return new FunctionDeclaration(qualifiedFunctionMemberName, moduleDeclatation, moduleDeclatation, "test", null, "test", functionAccessibility, null, Selection.Home, false, true, null, null);
        }

        private static void AddReference(Declaration toDeclaration, Declaration fromModuleDeclaration, ParserRuleContext context = null)
        {
            toDeclaration.AddReference(toDeclaration.QualifiedName.QualifiedModuleName, fromModuleDeclaration, fromModuleDeclaration, context, toDeclaration.IdentifierName, toDeclaration, Selection.Home, new List<Rubberduck.Parsing.Annotations.IAnnotation>());
        }
    }
}