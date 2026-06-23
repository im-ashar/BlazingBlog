namespace BlazingBlog.Services
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Danger
    }

    public sealed class ToastMessage
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ToastType Type { get; set; } = ToastType.Info;
    }

    public sealed class ToastService
    {
        public event Action<ToastMessage>? OnNotify;

        public void Notify(ToastMessage message) => OnNotify?.Invoke(message);

        public void Notify(string title, string message, ToastType type = ToastType.Info)
            => Notify(new ToastMessage { Title = title, Message = message, Type = type });
    }
}
