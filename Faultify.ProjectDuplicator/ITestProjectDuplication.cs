using System.Collections.Generic;
using System;

namespace Faultify.ProjectDuplicator
{
    public interface ITestProjectDuplication
    {
        IEnumerable<FileDuplication> TestProjectReferences { get; set; }
        FileDuplication TestProjectFile { get; set; }
        int DuplicationNumber { get; }
        bool IsInUse { get; set; }
        event EventHandler<TestProjectDuplication>? TestProjectFreed;

        void MarkAsFree();
        void DeleteTestProject();

        IList<MutationVariant> GetMutationVariants(
            IList<MutationVariantIdentifier>? mutationIdentifiers,
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular
        );

        void FlushMutations(IList<MutationVariant> mutationVariants);
    }
}