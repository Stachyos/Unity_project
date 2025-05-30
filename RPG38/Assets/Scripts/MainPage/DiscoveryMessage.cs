using Mirror;
using System;

[Serializable]
public struct DiscoveryRequest : NetworkMessage { }

[Serializable]
public struct DiscoveryResponse : NetworkMessage
{
    public Uri uri;
}