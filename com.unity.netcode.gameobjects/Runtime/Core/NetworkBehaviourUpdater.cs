using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Netcode
{
    public class NetworkBehaviourUpdater
    {
        private HashSet<NetworkBehaviour> m_Touched = new HashSet<NetworkBehaviour>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private ProfilerMarker m_NetworkBehaviourUpdate = new ProfilerMarker($"{nameof(NetworkBehaviour)}.{nameof(NetworkBehaviourUpdate)}");
#endif

        internal void AddForUpdate(NetworkBehaviour networkBehaviour)
        {
            m_Touched.Add(networkBehaviour);
        }

        internal void NetworkBehaviourUpdate(NetworkManager networkManager)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                if (networkManager.IsServer)
                {
                    foreach (var networkBehaviour in m_Touched)
                    {
                        foreach (var client in networkManager.ConnectedClientsList)
                        {
                            if (!networkBehaviour.HasNetworkObject || !networkBehaviour.NetworkObject.IsNetworkVisibleTo(client.ClientId))
                            {
                                continue;
                            }

                            networkBehaviour.VariableUpdate(client.ClientId);
                        }
                    }

                    foreach (var networkBehaviour in m_Touched)
                    {
                        networkBehaviour.PostNetworkVariableWrite();
                    }
                    m_Touched.Clear();
                }
                else
                {
                    // when client updates the server, it tells it about all its objects
                    foreach (var sobj in networkManager.SpawnManager.SpawnedObjectsList)
                    {
                        if (sobj.IsOwner)
                        {
                            for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                            {
                                sobj.ChildNetworkBehaviours[k].VariableUpdate(NetworkManager.ServerClientId);
                            }
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in networkManager.SpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_NetworkBehaviourUpdate.End();
#endif
            }
        }

    }
}
