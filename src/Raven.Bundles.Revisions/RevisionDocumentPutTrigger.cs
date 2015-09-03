#region CopyrightAndLicence
// --------------------------------------------------------------------------------------------------------------------
// <Copyright company="Damian Hickey" file="RevisionDocumentPutTrigger.cs">
// 	Copyright © 2012 Damian Hickey
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

namespace Raven.Bundles.Revisions
{
    using System.Diagnostics.Contracts;
    using Abstractions.Data;
    using Database.Plugins;
    using Json.Linq;

    public class RevisionDocumentPutTrigger : AbstractPutTrigger
    {
        internal const string RevisionSegment = "/revision/";
        internal const string RavenDocumentRevision = "Raven-Document-Revision";
        internal const string RavenDocumentRevisionStatus = "Raven-Document-Revision-Status";
        internal const string RavenDocumentEnsureUniqueConstraints = "Ensure-Unique-Constraints";

        public override VetoResult AllowPut(string key,
                                            RavenJObject document,
                                            RavenJObject metadata,
                                            TransactionInformation transactionInformation)
        {
            return VetoResult.Allowed;
        }

        public override void OnPut(string key,
                                   RavenJObject document,
                                   RavenJObject metadata,
                                   TransactionInformation transactionInformation)
        {
            Contract.Assume(!string.IsNullOrWhiteSpace(key));

            RavenJToken versionToken;
            if (!document.TryGetValue("Revision", out versionToken) || key.Contains(RevisionSegment))
                return;

            var newRevision = versionToken.Value<int>();
            var currentRevision = metadata.ContainsKey(RavenDocumentRevision) ? metadata[RavenDocumentRevision].Value<int>() : 0;

            metadata[RavenDocumentRevisionStatus] = RavenJToken.FromObject("Current");

            //if we have a higher revision number than the existing then put a new revision
            if (newRevision > currentRevision)
            {
                metadata[RavenDocumentRevision] = RavenJToken.FromObject(versionToken.Value<int>());
                metadata.__ExternalState[RavenDocumentRevision] = metadata[RavenDocumentRevision];
            }
        }

        public override void AfterPut(string key, RavenJObject document, RavenJObject metadata, Etag etag, TransactionInformation transactionInformation)
        {
            if (!metadata.__ExternalState.ContainsKey(RavenDocumentRevision))
                return;

            using (Database.DisableAllTriggersForCurrentThread())
            {
                var revisionMetadata = new RavenJObject(metadata);
                revisionMetadata[RavenDocumentRevisionStatus] = RavenJToken.FromObject("Historical");
                revisionMetadata.Remove(RavenDocumentRevision);

                if (revisionMetadata.ContainsKey(RavenDocumentEnsureUniqueConstraints))
                    revisionMetadata.Remove(RavenDocumentEnsureUniqueConstraints);

                var revisionCopy = new RavenJObject(document);
                var revisionkey = key + RevisionSegment + metadata.__ExternalState[RavenDocumentRevision];
                Database.Documents.Put(revisionkey, null, revisionCopy, revisionMetadata, transactionInformation);
            }
        }
    }
}