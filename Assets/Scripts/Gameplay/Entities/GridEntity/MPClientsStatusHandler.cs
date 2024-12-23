using System;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// NetworkBehaviour component of a <see cref="MPClientsStatusHandler"/> for operations/state that need to be synchronized
    /// </summary>
    public class MPClientsStatusHandler : NetworkBehaviour {
        public event Action ClientReadyEvent;
        
        [Command(requiresAuthority = false)]
        public void CmdSetClientReady() {
            ClientReadyEvent?.Invoke();
        }
    }
}