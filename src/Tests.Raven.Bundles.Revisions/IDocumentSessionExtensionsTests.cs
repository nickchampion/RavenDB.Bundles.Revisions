#region CopyrightAndLicence
// --------------------------------------------------------------------------------------------------------------------
// <Copyright company="Damian Hickey" file="IDocumentSessionExtensionsTests.cs">
// Copyright © 2012 Damian Hickey
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of
// the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </Copyright>
//  --------------------------------------------------------------------------------------------------------------------
#endregion

namespace Tests.Raven.Bundles.Revisions
{
	using System;
	using Moq;
	using Xunit;
	using global::Raven.Client;
	using global::Raven.Client.Connection;
	using global::Raven.Client.Revisions;

	public class IDocumentSessionExtensionsTests
	{
		[Fact]
		public void When_session_is_null_Then_should_throw()
		{
			IDocumentSession session = null;
			Assert.Throws<InvalidOperationException>(() => session.LoadRevision<RevisionedDocument>("key", 1));
		}

		[Fact]
		public void When_LoadRevision_Then_should_call_Load_with_revision_key()
		{
			var mockSession = new Mock<IDocumentSession>();
			mockSession.Setup(m => m.Load<RevisionedDocument>("key/revision/1"));
			mockSession.Object.LoadRevision<RevisionedDocument>("key", 1);
			mockSession.VerifyAll();
		}
	}

	public class IDatabaseCommandsExtensionsTests
	{
		[Fact]
		public void When_session_is_null_Then_should_throw()
		{
			IDatabaseCommands databaseCommands = null;
			Assert.Throws<InvalidOperationException>(() => databaseCommands.DeleteRevision("key", 1, null));
		}

		[Fact]
		public void When_Delete_Then_should_call_Delete_with_revision_key()
		{
			var mockDatabaseCommands = new Mock<IDatabaseCommands>();
			mockDatabaseCommands.Setup(m => m.Delete("key/revision/1", null));
			mockDatabaseCommands.Object.DeleteRevision("key", 1, null);
			mockDatabaseCommands.VerifyAll();
		}
	}
}