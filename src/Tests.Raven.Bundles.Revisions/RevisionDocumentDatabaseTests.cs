#region CopyrightAndLicence
// --------------------------------------------------------------------------------------------------------------------
// <Copyright company="Damian Hickey" file="RevisionDocumentDatabaseTests.cs">
// 	Copyright � 2012 Damian Hickey
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

using Raven.Abstractions.Exceptions;
using Raven.Bundles.Revisions;
using Raven.Client.Embedded;
using Raven.Client.Revisions;

namespace Tests.Raven.Bundles.Revisions
{
	using System;
	using System.Linq;
	using Xunit;

	public class RevisionDocumentDatabaseTests : IDisposable
	{
		private readonly EmbeddableDocumentStore _documentStore;

		public RevisionDocumentDatabaseTests()
		{
			_documentStore = new EmbeddableDocumentStore
			{
				RunInMemory = true,
			};
			_documentStore.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
			_documentStore.Initialize();
			_documentStore.DocumentDatabase.PutTriggers.Add(new RevisionDocumentPutTrigger { Database = _documentStore.DocumentDatabase });
			_documentStore.DocumentDatabase.ReadTriggers.Add(new HideRevisionDocumentsFromIndexingReadTrigger { Database = _documentStore.DocumentDatabase });
		}

		[Fact]
		public void When_save_revisioned_document_Then_should_be_able_to_load_revision()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new RevisionedDocument { Id = "key", Revision = 1 };
				session.Store(doc);
				session.SaveChanges();
			}

			using (var session = _documentStore.OpenSession())
			{
				var doc = session.LoadRevision<RevisionedDocument>("key", 1);
				Assert.NotNull(doc);
			}
		}

		[Fact]
		public void When_save_revisioned_document_second_time_without_changing_version_Then_should_update_versioned_copy()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new RevisionedDocument { Id = "key", Revision = 1, Data = "Alpha" };
				session.Store(doc);
				session.SaveChanges();
			}

			using (var session = _documentStore.OpenSession())
			{
				var doc = session.Load<RevisionedDocument>("key");
				doc.Data = "Beta";
				session.SaveChanges();
			}

			using (var session = _documentStore.OpenSession())
			{
				var doc = session.Load<RevisionedDocument>("key");
				Assert.Equal("Beta", doc.Data);
			}

			using (var session = _documentStore.OpenSession())
			{
				var doc = session.LoadRevision<RevisionedDocument>("key", 1);
				Assert.Equal("Beta", doc.Data);
			}
		}

		[Fact]
		public void When_query_Then_revisioned_document_is_not_returned()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new RevisionedDocument { Id = "key", Revision = 1, Data = "Alpha" };
				session.Store(doc);
				session.SaveChanges();
			}

			using (var session = _documentStore.OpenSession())
			{
				RevisionedDocument[] myVersionedDocuments = session.Query<RevisionedDocument>().Customize(q => q.WaitForNonStaleResults()).ToArray();
				Assert.Equal(1, myVersionedDocuments.Length);
				Assert.Equal("key", myVersionedDocuments[0].Id);
			}
		}

		[Fact]
		public void When_save_non_revisioned_document_and_load_revision_Then_should_get_null()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new NonRevisionedDocument { Id = "key", Data = "Alpha" };
				session.Store(doc);
				session.SaveChanges();
			}


			using (var session = _documentStore.OpenSession())
			{
				var doc = session.LoadRevision<NonRevisionedDocument>("key", 1);
				Assert.Null(doc);
			}
		}

		[Fact]
		public void When_saving_a_modified_revision_document_Then_should_throw()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new RevisionedDocument { Id = "key", Revision = 1, Data = "Alpha" };
				session.Store(doc);
				session.SaveChanges();
			}

			using (var session = _documentStore.OpenSession())
			{
				var doc = session.LoadRevision<RevisionedDocument>("key", 1);
				doc.Data = "Beta";
				Assert.Throws<OperationVetoedException>(() => session.SaveChanges());
			}
		}

		[Fact]
		public void When_delete_revision_and_load_Then_should_get_null()
		{
			using (var session = _documentStore.OpenSession())
			{
				var doc = new RevisionedDocument { Id = "key", Revision = 1, Data = "Alpha" };
				session.Store(doc);
				session.SaveChanges();
			}

			_documentStore.DatabaseCommands.DeleteRevision("key", 1, null);

			using (var session = _documentStore.OpenSession())
			{
				Assert.Null(session.LoadRevision<RevisionedDocument>("key", 1));
			}
		}

		public void Dispose()
		{
			_documentStore.Dispose();
		}
	}
}