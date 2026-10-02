namespace OmniTrade.Domain;

public interface IOrderBook
{
    /// <summary>
    /// Pure function: accepts a command and returns the list of events that represent what happened.
    /// Must perform no I/O.
    /// </summary>
    IEnumerable<IOrderEvent> Handle(IOrderCommand command);
}
