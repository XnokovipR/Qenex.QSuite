namespace Qenex.QSuite.Drivers;

public class DataReceivedEventArgs<T> : EventArgs
{
    public DateTime Timestamp { get; }
    public T Data { get; }

    public DataReceivedEventArgs(T data)
    {
        Timestamp = DateTime.Now;
        Data = data;
    }
    
}