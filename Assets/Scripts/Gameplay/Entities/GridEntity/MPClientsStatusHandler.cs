using System;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// NetworkBehaviour component of a <see cref="MPClientsStatusHandler"/> for operations/state that need to be synchronized
    /// </summary>
    public class MPClientsStatusHandler : NetworkBehaviour {
        /// <summary>
        /// Indicates that this client is ready. Passes along the index. 
        /// </summary>
        public event Action<int> ClientReadyEvent;
        
        [Command(requiresAuthority = false)]
        public void CmdSetClientReady(int clientIndex) {
            ClientReadyEvent?.Invoke(clientIndex);
        }
    }
}