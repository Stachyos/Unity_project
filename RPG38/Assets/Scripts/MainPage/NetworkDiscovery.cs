using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Mirror;
using Mirror.Discovery;

public class NetworkDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
{
    // 隐藏基类的同名事件，供 CustomNetworkManager 订阅
    public new event Action<DiscoveryResponse, IPEndPoint> OnServerFound;

    // 1. 客户端广播这个消息给所有主机
    protected override DiscoveryRequest GetRequest()
    {
        return new DiscoveryRequest();
    }

    // 2. 服务端收到请求后，回复自己的局域网 IP
    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        // 自动选第一个真实的本机局域网 IPv4
        string localLanIp = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .First(a =>
                a.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(a) &&
                !IsDockerOrHyperV(a)
            )
            .ToString();

        // 从同一个 GameObject 上的 NetworkManager 拿到 Transport port
        var transport = GetComponent<NetworkManager>().transport;
        ushort port = (ushort)transport.GetType()
            .GetProperty("Port")
            .GetValue(transport);

        // 构造 telepathy://<局域网IP>:<端口>
        var uri = new Uri($"telepathy://{localLanIp}:{port}");
        return new DiscoveryResponse { uri = uri };
    }

    // 3. 客户端收到某台主机的回复时，会调用此方法
    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        OnServerFound?.Invoke(response, endpoint);
    }

    // 排除 Docker/HyperV 网卡的辅助方法
    bool IsDockerOrHyperV(IPAddress a)
    {
        var bytes = a.GetAddressBytes();
        return bytes[0] == 198 && (bytes[1] & 0xFC) == 18;
    }
}