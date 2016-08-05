namespace Hl7.FluentPath
{
    public interface INavigator<out T>
        where T : INavigator<T>
    {
        bool MoveToNext();
        bool MoveToFirstChild();
        T Clone();
    }
}