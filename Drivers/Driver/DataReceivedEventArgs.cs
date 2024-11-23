namespace Qenex.QSuite.Driver;

public class DataReceivedEventArgs<T> : EventArgs
{
    public T Data { get; }

    public DataReceivedEventArgs(T data)
    {
        Data = data;
    }
    
}