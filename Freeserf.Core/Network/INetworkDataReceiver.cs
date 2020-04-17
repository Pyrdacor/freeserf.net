namespace Freeserf.Network
{
    public delegate void ResponseHandler(ResponseType responseType);
    public delegate void ReceivedNetworkDataHandler(IRemote source, INetworkData data, ResponseHandler responseHandler);

    public interface INetworkDataReceiver
    {
        void Receive(IRemote source, INetworkData data, ResponseHandler responseHandler);
        void ProcessReceivedData(ReceivedNetworkDataHandler dataHandler);
    }

    public interface INetworkDataReceiverFactory
    {
        INetworkDataReceiver CreateReceiver();
    }

    public interface INetworkDataHandler
    {
        void UpdateNetworkEvents(INetworkDataReceiver networkDataReceiver);
    }
}
