﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class ReferenceDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ReferenceDirectiveCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetCompletionProviderType()
            => typeof(ReferenceDirectiveCompletionProvider);

        protected override IEqualityComparer<string> GetStringComparer()
            => StringComparer.OrdinalIgnoreCase;

        private protected override Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters, flags);
        }

        [Fact]
        public async Task IsCommitCharacterTest()
        {
            var commitCharacters = PathUtilities.IsUnixLikePlatform ? new[] { '"', '/' } : new[] { '"', '\\', '/', ',' };
            await VerifyCommitCharactersAsync("#r \"$$", textTypedSoFar: "", validChars: commitCharacters, sourceCodeKind: SourceCodeKind.Script);
        }

        [Theory]
        [InlineData("#r \"$$/")]
        [InlineData("#r \"$$\\")]
        [InlineData("#r \"$$,")]
        [InlineData("#r \"$$A")]
        [InlineData("#r \"$$!")]
        [InlineData("#r \"$$(")]
        public void IsTextualTriggerCharacterTest(string markup)
            => VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true, SourceCodeKind.Script);

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(EnterKeyRule.Never)]
        [InlineData(EnterKeyRule.AfterFullyTypedWord)]
        [InlineData(EnterKeyRule.Always)] // note: GAC completion helper uses its own EnterKeyRule
        public async Task SendEnterThroughToEditorTest(EnterKeyRule enterKeyRule)
            => await VerifySendEnterThroughToEnterAsync("#r \"System$$", "System", enterKeyRule, expected: false, SourceCodeKind.Script);

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task GacReference()
            => await VerifyItemExistsAsync("#r \"$$", "System.Windows.Forms", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task GacReferenceFullyQualified()
        {
            await VerifyItemExistsAsync(
                "#r \"System.Windows.Forms,$$",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task FileSystemReference()
        {
            var systemDir = Path.GetFullPath(Environment.SystemDirectory);
            var windowsDir = Directory.GetParent(systemDir);
            var windowsRoot = Directory.GetDirectoryRoot(systemDir);

            // we need to get the exact casing from the file system:
            var normalizedWindowsPath = Directory.GetDirectories(windowsRoot, windowsDir.Name).Single();
            var windowsFolderName = Path.GetFileName(normalizedWindowsPath);

            var code = "#r \"" + windowsRoot + "$$";
            await VerifyItemExistsAsync(code, windowsFolderName, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }
    }
}
