﻿// Visual Studio Shared Project
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
#if NTVS_FEATURE_INTERACTIVEWINDOW
using Microsoft.NodejsTools.Repl;
#elif DEV14_OR_LATER
using Microsoft.PythonTools.InteractiveWindow;
#else
using Microsoft.VisualStudio.Repl;
#endif
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace TestUtilities.Mocks {
#if !NTVS_FEATURE_INTERACTIVEWINDOW && DEV14_OR_LATER
    using IReplEvaluator = IInteractiveEvaluator;
    using IReplWindow = IInteractiveWindow;
#endif

    public class MockReplWindow : IReplWindow {
        private readonly StringBuilder _output = new StringBuilder();
        private readonly StringBuilder _error = new StringBuilder();
        private readonly IReplEvaluator _eval;
        private readonly MockTextView _view;
        private readonly string _contentType;

#if DEV14_OR_LATER
        private PropertyCollection _properties;

        public event EventHandler<SubmissionBufferAddedEventArgs> SubmissionBufferAdded {
            add {
            }
            remove {
            }
        }
#endif

        public MockReplWindow(IReplEvaluator eval, string contentType = "Python") {
            _eval = eval;
            _contentType = contentType;
            _view = new MockTextView(new MockTextBuffer(String.Empty, contentType, filename: "text"));
            _eval._Initialize(this);
        }

        public bool ShowAnsiCodes { get; set; }

        public Task<ExecutionResult> Execute(string text) {
            _view.BufferGraph.AddBuffer(
                new MockTextBuffer(text, contentType: _contentType)
            );
            return _eval.ExecuteText(text);
        }

        public string Output {
            get {
                return _output.ToString();
            }
        }

        public string Error {
            get {
                return _error.ToString();
            }
        }

        #region IReplWindow Members

        public IWpfTextView TextView {
            get { return _view; }
        }

        public ITextBuffer CurrentLanguageBuffer {
            get { return _view.TextBuffer; }
        }

        public IReplEvaluator Evaluator {
            get { return _eval; }
        }

        public string Title {
            get { return "Mock Repl Window"; }
        }

#if DEV14_OR_LATER
        public ITextBuffer OutputBuffer {
            get {
                return _view.TextBuffer;
            }
        }

        public TextWriter OutputWriter {
            get {
                return new StringWriter(_output);
            }
        }

        public TextWriter ErrorOutputWriter {
            get {
                return new StringWriter(_error);
            }
        }

        public bool IsRunning {
            get {
                throw new NotImplementedException();
            }
        }

        public bool IsResetting {
            get {
                throw new NotImplementedException();
            }
        }

        public bool IsInitializing {
            get {
                throw new NotImplementedException();
            }
        }

        public IInteractiveWindowOperations Operations {
            get {
                throw new NotImplementedException();
            }
        }

        public PropertyCollection Properties {
            get {
                if (_properties == null) {
                    _properties = new PropertyCollection();
                }
                return _properties;
            }
        }
#endif

        public void ClearScreen() {
            _output.Clear();
            _error.Clear();
        }

        public void ClearHistory() {
            throw new NotImplementedException();
        }

        public void Focus() {
            throw new NotImplementedException();
        }

        public void Cancel() {
            throw new NotImplementedException();
        }

        public void InsertCode(string text) {
            throw new NotImplementedException();
        }

#if !DEV14_OR_LATER
        public void Submit(IEnumerable<string> inputs) {
            throw new NotImplementedException();
        }
#endif

        public System.Threading.Tasks.Task<ExecutionResult> Reset() {
            return _eval.Reset();
        }

#if !DEV14_OR_LATER
        public void AbortCommand() {
            _eval.AbortCommand();
        }
#endif

        public Task<ExecutionResult> ExecuteCommand(string command) {
            var tcs = new TaskCompletionSource<ExecutionResult>();
            tcs.SetException(new NotImplementedException());
            return tcs.Task;
        }

        public void WriteLine(string text) {
            if (!ShowAnsiCodes && text.IndexOf('\x1b') != -1) {
                AppendEscapedText(text);
            } else {
                _output.AppendLine(text);
            }
        }

        private void AppendEscapedText(string text) {
            // http://en.wikipedia.org/wiki/ANSI_escape_code
            // process any ansi color sequences...

            int escape = text.IndexOf('\x1b');
            int start = 0;
            do {
                if (escape != start) {
                    // add unescaped text
                    _output.Append(text.Substring(start, escape - start));
                }

                // process the escape sequence                
                if (escape < text.Length - 1 && text[escape + 1] == '[') {
                    // We have the Control Sequence Introducer (CSI) - ESC [

                    int? value = 0;
                    for (int i = escape + 2; i < text.Length; i++) { // skip esc + [
                        if (text[i] >= '0' && text[i] <= '9') {
                            // continue parsing the integer...
                            if (value == null) {
                                value = 0;
                            }
                            value = 10 * value.Value + (text[i] - '0');
                        } else if (text[i] == ';') {
                            if (value != null) {
                                value = null;
                            } else {
                                // CSI ; - invalid or CSI ### ;;, both invalid
                                break;
                            }
                        } else if (text[i] == 'm') {
                            // parsed a valid code
                            start = i + 1;
                        } else {
                            // unknown char, invalid escape
                            break;
                        }
                    }
                }// else not an escape sequence, process as text

                escape = text.IndexOf('\x1b', escape + 1);
            } while (escape != -1);
            if (start != text.Length - 1) {
                _output.Append(text.Substring(start));
            }
        }

        public void WriteOutput(object value) {
            _output.Append(value.ToString());
        }

        public void WriteError(object value) {
            _error.Append(value);
        }

#if DEV14_OR_LATER
        public TextReader ReadStandardInput() {
            throw new NotImplementedException();
        }
#else
        public string ReadStandardInput() {
            throw new NotImplementedException();
        }
#endif

#if !DEV14_OR_LATER || NTVS_FEATURE_INTERACTIVEWINDOW
        public void SetOptionValue(ReplOptions option, object value) {
        }

        public object GetOptionValue(ReplOptions option) {
            return null;
        }
#endif

#if DEV14_OR_LATER
        public Task<ExecutionResult> InitializeAsync() {
            throw new NotImplementedException();
        }

        public void Close() {
            throw new NotImplementedException();
        }

        Span IReplWindow.WriteLine(string text) {
            var start = _output.Length;
            _output.AppendLine(text);
            return new Span(start, _output.Length - start);
        }

        Span IReplWindow.WriteError(string text) {
            var start = _error.Length;
            _error.Append(text);
            return new Span(start, _error.Length - start);
        }

        Span IReplWindow.WriteErrorLine(string text) {
            var start = _error.Length;
            _error.AppendLine(text);
            return new Span(start, _error.Length - start);
        }

        public Task SubmitAsync(IEnumerable<string> inputs) {
            throw new NotImplementedException();
        }

        public Span Write(string text) {
            var start = _output.Length;
            _output.Append(text);
            return new Span(start, _output.Length - start);
        }

        public void Write(UIElement element) {
            throw new NotImplementedException();
        }

        public void FlushOutput() {
            throw new NotImplementedException();
        }

        public void AddInput(string input) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
#endif

        public event Action ReadyForInput {
            add { }
            remove { }
        }

        #endregion
    }

    public static class ReplEvalExtensions {
#if DEV14_OR_LATER
        public static Task<ExecutionResult> _Initialize(this IReplEvaluator self, IReplWindow window) {
            self.CurrentWindow = window;
            return self.InitializeAsync();
        }

        public static Task<ExecutionResult> ExecuteText(this IReplEvaluator self, string text) {
            return self.ExecuteCodeAsync(text);
        }

        public static Task<ExecutionResult> Reset(this IReplEvaluator self) {
            return self.ResetAsync();
        }
#else
        public static Task<ExecutionResult> _Initialize(this IReplEvaluator self, IReplWindow window) {
            return self.Initialize(window);
        }
#endif
    }
}
