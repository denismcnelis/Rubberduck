﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Rubberduck.Common;
using Rubberduck.Parsing.PreProcessing;
using Rubberduck.Parsing.Symbols.DeclarationLoaders;
using Rubberduck.Parsing.VBA;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.UIContext;
using Rubberduck.Root;
using Rubberduck.VBEditor.ComManagement;
using Rubberduck.VBEditor.Events;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using Rubberduck.VBEditor.Utility;

namespace Rubberduck.API.VBA
{
    [
        ComVisible(true),
        Guid(RubberduckGuid.IParserStateGuid)
    ]
    public interface IParserState
    {
        // vbe is the com coclass interface from the interop assembly.
        // There is no shared interface between VBA and VB6 types, hence object.
        [DispId(1)]
        void Initialize(object vbe); 
        [DispId(2)]
        void Parse();
        [DispId(3)]
        void BeginParse();
        [DispId(4)]
        Declaration[] AllDeclarations { get; }
        [DispId(5)]
        Declaration[] UserDeclarations { get; }
    }

    [
        ComVisible(true),
        Guid(RubberduckGuid.IParserStateEventsGuid),
        InterfaceType(ComInterfaceType.InterfaceIsIDispatch)
    ]
    public interface IParserStateEvents
    {
        [DispId(1)]
        void OnParsed();
        [DispId(2)]
        void OnReady();
        [DispId(3)]
        void OnError();
    }

    [
        ComVisible(true),
        Guid(RubberduckGuid.ParserStateClassGuid),
        ProgId(RubberduckProgId.ParserStateProgId),
        ClassInterface(ClassInterfaceType.None),
        ComDefaultInterface(typeof(IParserState)),
        ComSourceInterfaces(typeof(IParserStateEvents)),
        EditorBrowsable(EditorBrowsableState.Always)
    ]
    public sealed class ParserState : IParserState, IDisposable
    {
        private RubberduckParserState _state;
        private AttributeParser _attributeParser;
        private ParseCoordinator _parser;
        private IVBE _vbe;
        private IVBEEvents _vbeEvents;
        private readonly IUiDispatcher _dispatcher;

        public ParserState()
        {
            UiContextProvider.Initialize();
            _dispatcher = new UiDispatcher(UiContextProvider.Instance());
        }

        // vbe is the com coclass interface from the interop assembly.
        // There is no shared interface between VBA and VB6 types, hence object.
        public void Initialize(object vbe)
        {
            if (_parser != null)
            {
                throw new InvalidOperationException("ParserState is already initialized.");
            }

            _vbe = RootComWrapperFactory.GetVbeWrapper(vbe);
            _vbeEvents = VBEEvents.Initialize(_vbe);
            var declarationFinderFactory = new ConcurrentlyConstructedDeclarationFinderFactory();
            var projectRepository = new ProjectsRepository(_vbe);
            _state = new RubberduckParserState(null, projectRepository, declarationFinderFactory, _vbeEvents);
            _state.StateChanged += _state_StateChanged;

            var exporter = new ModuleExporter();

            Func<IVBAPreprocessor> preprocessorFactory = () => new VBAPreprocessor(double.Parse(_vbe.Version, CultureInfo.InvariantCulture));
            _attributeParser = new AttributeParser(exporter, preprocessorFactory, _state.ProjectsProvider);
            var projectManager = new RepositoryProjectManager(projectRepository);
            var moduleToModuleReferenceManager = new ModuleToModuleReferenceManager();
            var parserStateManager = new ParserStateManager(_state);
            var referenceRemover = new ReferenceRemover(_state, moduleToModuleReferenceManager);
            var supertypeClearer = new SupertypeClearer(_state);
            var comSynchronizer = new COMReferenceSynchronizer(_state, parserStateManager);
            var builtInDeclarationLoader = new BuiltInDeclarationLoader(
                _state,
                new List<ICustomDeclarationLoader>
                    {
                        new DebugDeclarations(_state),
                        new SpecialFormDeclarations(_state),
                        new FormEventDeclarations(_state),
                        new AliasDeclarations(_state),
                        //new RubberduckApiDeclarations(_state)
                    }
                );
            var parseRunner = new ParseRunner(
                _state,
                parserStateManager,
                preprocessorFactory,
                _attributeParser, 
                exporter);
            var declarationResolveRunner = new DeclarationResolveRunner(
                _state, 
                parserStateManager, 
                comSynchronizer);
            var referenceResolveRunner = new ReferenceResolveRunner(
                _state,
                parserStateManager,
                moduleToModuleReferenceManager,
                referenceRemover);
            var parsingStageService = new ParsingStageService(
                comSynchronizer,
                builtInDeclarationLoader,
                parseRunner,
                declarationResolveRunner,
                referenceResolveRunner  
                );
            var parsingCacheService = new ParsingCacheService(
                _state,
                moduleToModuleReferenceManager,
                referenceRemover,
                supertypeClearer
                );

            _parser = new ParseCoordinator(
                _state,
                parsingStageService,
                parsingCacheService,
                projectManager,
                parserStateManager
                );
        }

        /// <summary>
        /// Blocking call, for easier unit-test code
        /// </summary>
        public void Parse()
        {
            // blocking call
            _parser.Parse(new System.Threading.CancellationTokenSource());
        }

        /// <summary>
        /// Begins asynchronous parsing
        /// </summary>
        public void BeginParse()
        {
            // non-blocking call
            _dispatcher.Invoke(() => _state.OnParseRequested(this));
        }

        public event Action OnParsed;
        public event Action OnReady;
        public event Action OnError;

        private void _state_StateChanged(object sender, EventArgs e)
        {
            AllDeclarations = _state.AllDeclarations
                                     .Select(item => new Declaration(item))
                                     .ToArray();
            
            UserDeclarations = _state.AllUserDeclarations
                                     .Select(item => new Declaration(item))
                                     .ToArray();

            var errorHandler = OnError;
            if (_state.Status == Parsing.VBA.ParserState.Error && errorHandler != null)
            {
                _dispatcher.Invoke(errorHandler.Invoke);
            }

            var parsedHandler = OnParsed;
            if (_state.Status == Parsing.VBA.ParserState.Parsed && parsedHandler != null)
            {
                _dispatcher.Invoke(parsedHandler.Invoke);
            }

            var readyHandler = OnReady;
            if (_state.Status == Parsing.VBA.ParserState.Ready && readyHandler != null)
            {
                _dispatcher.Invoke(readyHandler.Invoke);
            }
        }

        public Declaration[] AllDeclarations { get; private set; }

        public Declaration[] UserDeclarations { get; private set; }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_state != null)
            {
                _state.StateChanged -= _state_StateChanged;
            }


            //_vbe.Release();            
            _disposed = true;
        }
    }
}
