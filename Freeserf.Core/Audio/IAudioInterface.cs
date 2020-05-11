namespace Freeserf.Audio
{
    public interface IAudioInterface
    {
        IAudioFactory AudioFactory { get; }

        Data.DataSource DataSource { get; }
    }
}
