using Mirror;

namespace Util {
    public class ThirdPartyPackageUtil {
        
        /// <summary>
        /// Perform any necessary checks to see if no-op methods we added to third party code have been removed.
        /// This exists in order to catch any cases where we update third party packages and forget to update the
        /// corresponding custom changes to those third parties. 
        /// </summary>
        private void CheckForRemovedPackageModifications() {
            MessagePacking.CauseCompilationErrorIfMethodRemoved();
        }
    }
}